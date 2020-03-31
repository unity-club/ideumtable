using CoffeeTable.Common.Manifests;
using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Common.Messaging.Core;
using CoffeeTable.Common.Messaging.Handling;
using CoffeeTable.Common.Messaging.Requests;
using CoffeeTable.Logging;
using CoffeeTable.Providers;
using CoffeeTable.Publishers;
using CoffeeTable.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable IDE0051 // Disable warning about unused private methods that are invoked via reflection

namespace CoffeeTable
{
	public sealed class Table : MonoBehaviour
	{
		private OnApplicationsUpdatedPublisher mOnApplicationsUpdatedPublisher = new OnApplicationsUpdatedPublisher();
		private event Action<ApplicationsManifest> mOnApplicationsUpdated;
		public static event Action<ApplicationsManifest> OnApplicationsUpdated
		{
			add { Instance.mOnApplicationsUpdated += value; }
			remove { Instance.mOnApplicationsUpdated -= value; }
		}

		private bool mServiceConnected => mProviderService?.IsConnected ?? false;
		private bool mOnline;
		public static bool IsOnline => Instance.mOnline;

		private ApplicationsManifest mAppManifest;
		public static ApplicationsManifest Apps => Instance.mAppManifest;
		public static ApplicationInstanceInfo CurrentApp
		{
			get
			{
				if (!Instance.mOnline) return null;
				return Apps
					.RunningApplications.Where(i => i.DestinationId == Instance.mApplicationId)
					.FirstOrDefault();
			}
		}

		private List<Message> mMessageBuffer = new List<Message>();
		private TableProviderService mProviderService;

		private const string mModuleName = "coffeetable";
		private int mModuleId = 0;
		private int mHttpPort, mTcpPort;
		private uint mApplicationId = 0;

		public IMessagingHandler Messaging { get; private set; }

		#region Singleton
		private static Table mInstance;
		public static Table Instance
		{
			get
			{
				if (mInstance == null)
					mInstance = FindObjectOfType<Table>();
				if (mInstance == null)
				{
					var go = new GameObject()
					{
						name = Log.ApplicationName
					};
					mInstance = go.AddComponent<Table>();
				}
				return mInstance;
			}
		}

		private void EnsureSingleton()
		{
			if (mInstance == null)
			{
				mInstance = this;
				DontDestroyOnLoad(gameObject);
				gameObject.name = Log.ApplicationName;
			}
			else Destroy(gameObject);
		}

		#endregion

		#region Connection

		public static void Subscribe (object o)
		{
			var i = Instance;
			i.mOnApplicationsUpdatedPublisher.Subscribe(o as IOnApplicationsUpdated);
		}

		public static void Unsubscribe (object o)
		{
			var i = Instance;
			i.mOnApplicationsUpdatedPublisher.Unsubscribe(o as IOnApplicationsUpdated);
		}

		public static bool RetryConnection ()
		{
			Instance.Initialize();
			return IsOnline;
		}

		private void ReadMessageBuffer ()
		{
			lock (mMessageBuffer)
			{
				foreach (var newMessage in mMessageBuffer)
					Messaging?.Receive(newMessage);
				mMessageBuffer.Clear();
			}
		}

		private void Initialize ()
		{
			if (!AppSettings.IsApiEnabled) return;
			EstablishConnection();
			if (mServiceConnected) SubscribeToService();
		}

		private void EstablishConnection ()
		{
			if (mServiceConnected) return;

			GetPortNumbers(ref mHttpPort, ref mTcpPort);

			Ref<int> moduleId = new Ref<int> { Value = 0 };
			GetModuleId(mModuleName, mHttpPort, moduleId).Wait();
			if (moduleId.Value == 0)
			{
				Log.Warn($"Failed to retrieve service module ID for module: '{mModuleName}'. " +
					$"If you are attempting to access this functionality while running in the editor, it is safe to ignore these warnings. " +
					$"Otherwise, make sure that the service is running with the '{mModuleName}' module attached.");
				return;
			}
			mModuleId = moduleId.Value;

			Log.Out($"Initializing service TCP connection on port <b>{mTcpPort}</b>...");
			mProviderService = new TableProviderService()
			{
				PortNumber = mTcpPort,
				ModuleId = mModuleId
			};

			mProviderService.MessageReceived += HandleOnMessageReceived;
			mProviderService.Disconnected += HandleOnServiceDisconnected;

			Messaging = new MessagingHandler(mProviderService.Send);
			Messaging.Register(this);

			if (!mProviderService.StartProvider()) return;
		}

		private void SubscribeToService ()
		{
			if (mOnline) return;

			// Subscribe to the service module
			var subscriptionRequest = new ServiceSubscriptionRequest()
			{
				IsSimulator = Application.isEditor,
				ProcessId = Process.GetCurrentProcess().Id,
				SimulatedApplication = AppSettings.GetManifest()
			};
			var subscriptionExchange = Messaging.Send<ServiceSubscriptionResponse>(1, "subscribe", subscriptionRequest);

			// Wait for a response from the service.
			// This is a bit of a hack, but we need to synchronously await the service's response
			// so that the app manifests will be available on the first call to this class
			//Message response = null;
			Stopwatch st = new Stopwatch();
			st.Start();
			while (!subscriptionExchange.Complete)
			{
				if (st.ElapsedMilliseconds > 2500)
				{
					Log.Warn("Failed to receive a response from the service after attempting to subscribe. " +
						"If this application was not launched by the service and you are running in standalone, " +
						"your application will not be able to access any service-side functionality.");
					return;
				}
				ReadMessageBuffer();
			}
			st.Stop();

			if (!subscriptionExchange.Success)
			{
				Log.Warn($"Failed to subscribe to service: {subscriptionExchange.Details}");
				return;
			}

			mAppManifest = subscriptionExchange.Data.AppsManifest;
			mApplicationId = subscriptionExchange.Data.SubscriberId;

			Log.Out("Successfully subscribed to service.");
			mOnline = true;
		}

		private void Deinitialize ()
		{
			mOnline = false;
			var provider = mProviderService;
			mProviderService = null;
			provider?.Dispose();
			Messaging = null;
		}

		private void GetPortNumbers (ref int httpPort, ref int tcpPort)
		{
			CoffeeTableManifest launchManifest = null;
			string manPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "coffeetable", "coffeetable.json");
			if (File.Exists(manPath))
			{
				try { launchManifest = JsonConvert.DeserializeObject<CoffeeTableManifest>(File.ReadAllText(manPath)); }
				catch (JsonException) { }
			}

			if (launchManifest == null)
			{
				httpPort = AppSettings.FallbackHttpPort;
				tcpPort = AppSettings.FallbackTcpPort;
			} else
			{
				httpPort = launchManifest.ServiceHttpPort;
				tcpPort = launchManifest.ServiceTcpPort;
			}
		}

		private IEnumerator GetModuleId (string moduleName, int httpPort, Ref<int> moduleId)
		{
			if (string.IsNullOrWhiteSpace(moduleName)) yield break;

			UnityWebRequest request = UnityWebRequest.Get($"http://localhost:{httpPort}");
			yield return request.SendWebRequest();

			while (!request.isDone)
				yield return null;

			if (request.isNetworkError || request.isHttpError)
				yield break;

			string json = request.downloadHandler.text;
			if (string.IsNullOrWhiteSpace(json))
			{
				Log.Warn("Failed to receive JSON from HTTP request to service.");
				yield break;
			}

			try
			{
				moduleId.Value = (from JToken j in JObject.Parse(json)["Manifest"]["Modules"].Children()
							where moduleName.Equals((string)j["Name"])
							select (int)j["Id"]).FirstOrDefault();
			}
			catch (Exception e)
			{
				if (e is JsonException || e is InvalidCastException)
					Log.Warn("Failed to parse JSON response from HTTP request to service.");
				else throw;
				yield break;
			}
		}

		private void HandleOnMessageReceived (Message message)
		{
			lock (mMessageBuffer) mMessageBuffer.Add(message);
		}

		private void HandleOnServiceDisconnected ()
		{
			Deinitialize();
		}

		#endregion

		#region MonoBehaviour

		private void Awake()
		{
			EnsureSingleton();
			Initialize();
		}

		private void Update()
		{
			ReadMessageBuffer();
		}

		private void OnDestroy()
		{
			Deinitialize();
		}

		private void OnApplicationQuit()
		{
			Deinitialize();
			Thread.Sleep(500);
		}

		#endregion

		#region CoffeeTable Messaging

		[RequestHandler("update")]
		private void OnApplicationsManifestChanged (Request<ApplicationInstanceInfo[]> request, Response<None> response)
		{
			if (mAppManifest == null) return;
			mAppManifest.RunningApplications = request.Data;
			mOnApplicationsUpdated?.Invoke(mAppManifest);
			mOnApplicationsUpdatedPublisher.Publish(mAppManifest);
		}

		#endregion

		#region Functionality
		
		/// <summary>
		/// Attempts to set the fullscreen mode of this application on the coffee table.
		/// </summary>
		/// <param name="fullscreen">A boolean indicating whether or not the application should be in fullscreen. Pass in <c>true</c> to make this application fullscreen, or <c>false</c> to turn off fullscreen mode.</param>
		/// <returns>A task whose boolean result indicates the success of the operation. If <c>true</c>, the fullscreen mode of this application was successfully set.</returns>
		public static async Task<bool> SetFullscreenAsync (bool fullscreen)
		{
			if (!Instance.mServiceConnected || !IsOnline) return false; 
			var exchange = Instance.Messaging?.Send(1, "setFullscreenMode", fullscreen);
			if (exchange == null) return false;
			await exchange;
			if (!exchange.Success) Log.Warn($"Could not make this application fullscreen: {exchange.Details}");
			return exchange.Success;
		}

		/// <summary>
		/// Tells the service to close this application.
		/// </summary>
		public static async void Quit ()
		{
			if (!Instance.mServiceConnected || !IsOnline)
			{
				ForceQuit();
				return;
			}
			var exchange = Instance.Messaging.Send(1, "closeApplication");
			await exchange;
			if (!exchange.Success)
				Log.Warn($"Failed to quit via service request: {exchange.Details}");
			ForceQuit();
		}

		private static void ForceQuit ()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

#endregion
	}
}
