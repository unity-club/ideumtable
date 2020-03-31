using CoffeeTable.Common.Manifests;
using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Module.Applications;
using CoffeeTable.Module.Messaging;
using CoffeeTable.Module.Util;
using CoffeeTable.Module.Window;
using Ideum;
using Ideum.Networking.Application;
using Ideum.Networking.Transport;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTable.Module
{
	[Module("coffeetable")]
	public class CoffeeTableModule : ModuleBase, ITransportLayerReceiver, ITransportLayerDisconnectReceiver
	{
		private CoffeeTableManifest mCoffeeTableManifest;

		private ApplicationStore mApplicationStore;
		private ApplicationManager mAppManager;
		private WindowManager mWindowManager;
		private MessageRouter mMessageRouter;

		protected override async void Initialize()
		{
			base.Initialize();

			CacheServiceInformation();

			mApplicationStore = new ApplicationStore();
			mWindowManager = new WindowManager(mApplicationStore);
			mMessageRouter = new MessageRouter(mApplicationStore, Service.Send);
			mAppManager = new ApplicationManager(mApplicationStore, mMessageRouter, mWindowManager);

			// Ensures that all running applications are closed when the console is closed
			ConsoleShutdownHandler.OnShutdown += mApplicationStore.Dispose;

			//new Action(async () =>
			//{
			//	await Task.Delay(16000).ConfigureAwait(false);
			//	var inst1 = mAppManager.LaunchApplication(mApplicationStore.GetApplication("RollABall"));
			//}).Invoke();

			

		}

		protected override void Deinitialize()
		{
			base.Deinitialize();
			mApplicationStore.Dispose();
		}

		private void CacheServiceInformation ()
		{
			CoffeeTableManifest manifest;
			string manPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoffeeTable", "coffeetable.json");
			if (!File.Exists(manPath)) manifest = new CoffeeTableManifest();
			else
			{
				try { manifest = JsonConvert.DeserializeObject<CoffeeTableManifest>(File.ReadAllText(manPath)); }
				catch (JsonException) { manifest = new CoffeeTableManifest(); }
			}

			manifest.ServiceHttpPort = Service.Manifest.HttpPort;
			manifest.ServiceTcpPort = Service.Manifest.TcpPort;

			File.WriteAllText(manPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));

			mCoffeeTableManifest = manifest;
		}

		public void Receive(TcpMessage message) => mMessageRouter.OnMessageReceived(message);
		public void OnClientDisconnected(IClient client) => mMessageRouter.OnClientDisconnected(client);

		#region Http

		[Get("/")]
		public void GetCoffeeTableManifest (Http http, ref IResponse response)
		{
			response = new GenericResponse<CoffeeTableManifest>
			{
				Result = mCoffeeTableManifest
			};
		}

		[Get("apps")]
		public void GetApplicationsManifest(Http http, ref IResponse response)
		{
			response = new GenericResponse<ApplicationsManifest>
			{
				Result = mApplicationStore.ToManifest()
			};
		}

		[Get("apps/installed")]
		public void GetInstalledApplicationsManifest (Http http, ref IResponse response)
		{
			response = new GenericResponse<ApplicationInfo[]>
			{
				Result = mApplicationStore.Applications.Select(app => app.ToManifest()).ToArray()
			};
		}

		[Get("apps/running")]
		public void GetRunningApplicationsManifest (Http http, ref IResponse response)
		{
			response = new GenericResponse<ApplicationInstanceInfo[]>
			{
				Result = mApplicationStore.Instances.Select(instance => instance.ToManifest()).ToArray()
			};
		}

		#endregion
	}
}
