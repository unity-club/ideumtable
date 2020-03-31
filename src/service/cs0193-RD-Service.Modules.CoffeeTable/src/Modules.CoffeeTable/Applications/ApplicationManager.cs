using CoffeeTable.Common.Manifests;
using CoffeeTable.Common.Messaging.Handling;
using CoffeeTable.Module.Launchers;
using CoffeeTable.Module.Messaging;
using CoffeeTable.Module.Window;
using Ideum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Applications
{
	public class ApplicationManager
	{
		private ApplicationStore mApplicationStore;
		private MessageRouter mMessageRouter;
		private WindowManager mWindowManager;
		private ILog Log = LogManager.GetLogger(typeof(ApplicationManager));

		private ILauncher mDefaultLauncher;
		private Dictionary<string, ILauncher> mLauncherMap = new Dictionary<string, ILauncher>();

		public ApplicationManager(ApplicationStore appStore, MessageRouter router, WindowManager windowManager)
		{
			mApplicationStore = appStore;
			mMessageRouter = router;
			mWindowManager = windowManager;

			mMessageRouter.CreateSimulatorInstance = CreateSimulatorInstance;

			RegisterLaunchers();

			mApplicationStore.OnApplicationInstanceCreated += i => Log.Warn($"Application '{i.App.Name}' was created.");
			mApplicationStore.OnApplicationInstanceDestroyed += i => Log.Warn($"Application '{i.App.Name}' was destroyed.");
			mApplicationStore.OnNotifyApplicationsChanged += NotifyApplicationInstancesChanged;

			mMessageRouter.Handler.Register(this);
		}

		// Notifies all subscribed clients that the application instance manifests have changed
		private void NotifyApplicationInstancesChanged (ApplicationStore appStore)
		{
			var appInstancesManifest = appStore.ToManifest().RunningApplications;
			var subscribed = appStore.Instances.Where(i => i.Connection.IsClientConnected).ToList();
			foreach (var instance in subscribed)
				mMessageRouter.Handler.Send<None>(instance.Id, "update", appInstancesManifest);
		}

		private void RegisterLaunchers()
		{
			var launcherTypes = from type in Assembly.GetExecutingAssembly().GetTypes()
								let attr = type.GetCustomAttribute<LauncherAttribute>()
								let implementsInterface = type.GetInterfaces().Contains(typeof(ILauncher))
								where attr != null || implementsInterface
								select new { Attr = attr, LauncherType = type, ImplementsInterface = implementsInterface };

			foreach (var launcherPair in launcherTypes) {
				if (launcherPair.Attr == null && launcherPair.ImplementsInterface)
					throw new ArgumentException($"Type {launcherPair.LauncherType.Name} implementing the {nameof(ILauncher)} interface needs to be marked with a {nameof(LauncherAttribute)} indicating the name of the launcher.");
				else if (launcherPair.Attr != null && !launcherPair.ImplementsInterface)
					throw new ArgumentException($"Type {launcherPair.LauncherType.Name} marked with the {nameof(LauncherAttribute)} needs to implement the {nameof(ILauncher)} interface for it to be used as a launcher.");
				else if (string.IsNullOrWhiteSpace(launcherPair.Attr.LauncherName))
					throw new ArgumentException($"Type {launcherPair.LauncherType.Name} marked with the {nameof(LauncherAttribute)} cannot have an empty launcher name.");

				ILauncher launcher = Activator.CreateInstance(launcherPair.LauncherType) as ILauncher;
				mLauncherMap.Add(launcherPair.Attr.LauncherName, launcher);
				Log.Info($"Registered launcher mapping: {{{launcherPair.Attr.LauncherName} -> {launcherPair.LauncherType.Name}}}");

				if (launcher is DefaultLauncher)
					mDefaultLauncher = launcher;
			}
		}

		public ApplicationInstance LaunchApplication (Application app)
		{
			if (app == null) return null;

			if (!IsLaunchableWithLayout(app, out ApplicationLayout appLayout)) return null;

			ApplicationInstance instance = new ApplicationInstance(app, appLayout);
			mApplicationStore.AddApplicationInstance(instance);

			LaunchApplicationAsync(instance);

			return instance;
		}

		private async void LaunchApplicationAsync (ApplicationInstance instance)
		{
			//
			// TODO: Begin loading animation(s) here.
			//

			instance.State = ApplicationState.Starting;
			await Task.Run(async () =>
			{
				ILauncher appLauncher;
				if (!mLauncherMap.TryGetValue(instance.App.LauncherName ?? string.Empty, out appLauncher)) appLauncher = mDefaultLauncher;
				instance.Process = appLauncher.LaunchApplication(instance.App);
				if (instance.Process == null)
				{
					// Failed to launch app process, so early exit.
					OnApplicationInstanceExitted(instance);
					return;
				}

				// Get callback on process exitted
				instance.Process.EnableRaisingEvents = true;
				instance.Process.Exited += (o, e) => OnApplicationInstanceExitted(instance);

				if (!await mWindowManager.OpenWindow(instance).ConfigureAwait(false))
				{
					// App instance's window did not open properly, so let's ensure that
					// the process is killed.
					if (!instance.Process.HasExited)
						try { instance.Process.Kill(); }
						catch (Win32Exception) { }

					//
					// TODO: Also end loading animation(s) here 
					//
				}

				//
				// TODO: End loading animation(s) here 
				//

				instance.State = ApplicationState.Running;
				mApplicationStore.NotifyApplicationsChanged();
			}).ConfigureAwait(false);
		}

		// Returns true if the given application can be launched, and false if it cannot.
		// If the application can be launched, produces the layout it will be launched with.
		private bool IsLaunchableWithLayout (Application app, out ApplicationLayout layout)
		{
			layout = ApplicationLayout.Fullscreen;
			switch (app.Type)
			{
				case ApplicationType.Sidebar:
					int sidebarCount = mApplicationStore.Sidebars.Count();
					if (sidebarCount == 0) layout = ApplicationLayout.LeftPanel;
					else if (sidebarCount == 1) layout = ApplicationLayout.RightPanel;
					else if (sidebarCount >= 2) return false;
					return true;

				case ApplicationType.Homescreen:
					return mApplicationStore.HomeScreen != null;

				case ApplicationType.Application:
					if (mApplicationStore.Instances.Any(i => i.Layout == ApplicationLayout.Fullscreen))
						return false;
					if (app.LaunchInFullscreen) return !mApplicationStore.Instances.Any(i => i.App.Type == ApplicationType.Application);
					else
					{
						// This application wants to be launched in half-screen,
						// so we will attempt to find a half of the screen where it can live.
						if (!mApplicationStore.Instances.Any(i => i.Layout == ApplicationLayout.LeftPanel))
						{
							layout = ApplicationLayout.LeftPanel;
							return true;
						}
						else if (!mApplicationStore.Instances.Any(i => i.Layout == ApplicationLayout.RightPanel))
						{
							layout = ApplicationLayout.RightPanel;
							return true;
						}
						return false;
					}

				default: return false;
			}
		}

		private ApplicationInstance CreateSimulatorInstance (Application app)
		{
			if (app == null) return null;
			if (!IsLaunchableWithLayout(app, out ApplicationLayout layout)) return null;
			ApplicationInstance simulatorInstance = new ApplicationInstance(app, layout);
			simulatorInstance.IsSimulator = true;
			simulatorInstance.State = ApplicationState.Running;
			return simulatorInstance;
		}

		// Call immediately after an ApplicationInstance's process has been terminated
		// and its window closed
		private void OnApplicationInstanceExitted (ApplicationInstance instance)
		{
			if (instance.Process != null && !instance.Process.HasExited)
				instance.Process.Kill();
			instance.State = ApplicationState.Destroyed;
			mApplicationStore.RemoveApplicationInstance(instance);
		}

		#region RequestHandlers

		/// <summary>
		/// If there are half-screen apps running on the screen, swaps them so that the application running on the 
		/// left hand side of the screen runs on the right hand side of the screen and vice versa.
		/// </summary>
		[RequestHandler("swap")]
		private void Swap (Request<None> request, Response<None> response)
		{
			ApplicationInstance left, right;
			left = mApplicationStore.Instances
				.Where(i => i.Layout == ApplicationLayout.LeftPanel && i.App.Type == ApplicationType.Application)
				.FirstOrDefault();
			right = mApplicationStore.Instances
				.Where(i => i.Layout == ApplicationLayout.RightPanel && i.App.Type == ApplicationType.Application)
				.FirstOrDefault();

			if ((left != null && left.State != ApplicationState.Running) ||
				(right != null && right.State != ApplicationState.Running))
			{
				response.Success = false;
				response.Details = "Could not swap application windows because one or more of the windows that would have been swapped were in the process of being launched.";
				return; // Do not allow swapping if windows have not finished loading
			}

			if (left != null) left.Layout = ApplicationLayout.RightPanel;
			if (right != null) right.Layout = ApplicationLayout.LeftPanel;

			mWindowManager.SizeWindow(left);
			mWindowManager.SizeWindow(right);

			mApplicationStore.NotifyApplicationsChanged();
		}

		/// <summary>
		/// Attempts to set the fullscreen mode of the sender application.
		/// The sender application must pass in a boolean with their request indicating whether or not it should be made fullscreen.
		/// If this boolean is true, the service will attempt to make the sender application fullscreen.
		/// If this boolean is true, the service will attempt to make the sender application half-screen.
		/// The response will be successful if the sender application was already fullscreen or already half-screen, respectively.
		/// </summary>
		[RequestHandler("setFullscreenMode")]
		private void SetFullscreenMode (Request<bool> request, Response<None> response)
		{
			response.Success = false;

			ApplicationInstance instance = mApplicationStore.GetFromId(request.SenderId);
			if (instance == null) return;

			if (instance.App.Type == ApplicationType.Sidebar
				|| instance.App.Type == ApplicationType.Homescreen)
			{
				response.Details = "Fullscreen modes cannot be toggled on the sidebar and homescreen applications";
				return;
			}

			if (instance.State == ApplicationState.Destroyed
				|| instance.State == ApplicationState.Starting)
			{
				response.Details = "Fullscreen mode could not be toggled because this application was either destroyed or still loading.";
				return;
			}

			var runningApplications = mApplicationStore.GetInstancesOfType(ApplicationType.Application);
			if (runningApplications.Count() > 1 || !runningApplications.Contains(instance))
			{
				response.Details = "Fullscreen mode could not be set because screen space was not available for this app to be made fullscreen.";
				return;
			}

			if (request.Data)
			{
				// Half-screen -> Fullscreen
				// This app must be the only running application, and it must be half screen
				if (instance.Layout == ApplicationLayout.Fullscreen)
				{
					response.Success = true;
					response.Details = "This app is already running in fullscreen mode.";
					return;
				}
				else instance.Layout = ApplicationLayout.Fullscreen;
			} else
			{
				// Fullscreen -> Half-screen
				// This app must be the only running application, and it must be full-screen
				if (instance.Layout != ApplicationLayout.Fullscreen)
				{
					response.Success = true;
					response.Details = "This application is already not in fullscreen mode.";
					return;
				}
				else instance.Layout = ApplicationLayout.LeftPanel;
			}

			mWindowManager.SizeWindow(instance);

			response.Success = true;
		}

		/// <summary>
		/// Attempts to launch an application. Currently only the homescreen application is allowed to launch
		/// applications, so any other app that attempts to launch an application will receive an unsuccessful response.
		/// </summary>
		/// <param name="request">A request containing an unsigned integer representing the ID of the application that should be launched.</param>
		/// <param name="response">A response indicating whether the application could be launched.</param>
		[RequestHandler("launchApplication")]
		private void LaunchApplication(Request<uint> request, Response<None> response)
		{
			response.Success = false;
			var instance = mApplicationStore.GetFromId(request.SenderId);
			if (instance == null) return;

			if (request.SenderId != ApplicationInstance.HomescreenId)
			{
				response.Details = "Only the homescreen application is allowed to launch applications!";
				return;
			}

			Application app = mApplicationStore.GetApplication(request.Data);
			if (app == null)
			{
				response.Details = $"Could not find an application with the specified id #{request.Data}";
				return;
			}

			var launchInstance = LaunchApplication(app);
			if (launchInstance == null)
			{
				response.Details = $"The application with name {app.Name} could not be launched.";
				return;
			}

			response.Success = true;
		}

		/// <summary>
		/// Attempts to close an application depending upon the identity of the application that sent this message. See remarks for more info.
		/// </summary>
		/// <remarks>
		/// If the application that sent this request is a sidebar application, then this method will attempt to close the application associated with the sidebar.
		/// For instance, if the left sidebar sends this request, this method will attempt to close the application whose layout is ApplicationLayout.LeftPanel.
		/// If a fullscreen application is running and a sidebar application sends this request, this method will attempt to close the fullscreen application regardless of which sidebar sent the request.
		/// If a regular application sends this request, this method will attempt to close the application that sent this request.
		/// </remarks>
		[RequestHandler("closeApplication")]
		private void CloseApplication (Request<None> request, Response<None> response)
		{
			response.Success = false;
			var sender = mApplicationStore.GetFromId(request.SenderId);
			if (sender == null) return;

			ApplicationInstance closingInstance = null;

			switch (sender.App.Type)
			{
				case ApplicationType.Homescreen:
					response.Details = "Homescreen applications can only be closed internally.";
					return;

				case ApplicationType.Sidebar:
					if (sender.Layout == ApplicationLayout.LeftPanel)
						closingInstance = mApplicationStore.LeftPanelApp ?? mApplicationStore.FullscreenApp;
					else if (sender.Layout == ApplicationLayout.RightPanel)
						closingInstance = mApplicationStore.RightPanelApp ?? mApplicationStore.FullscreenApp;
					else return;
					break;

				case ApplicationType.Application:
					closingInstance = sender;
					break;
			}

			if (closingInstance == null)
			{
				response.Details = "Could not find an application instance to close.";
				return;
			}

			if (closingInstance.IsSimulator)
			{
				response.Details = "Cannot close a simulator application instance.";
				return;
			}

			response.Success = true;
			closingInstance.TerminateProcessAsync(TimeSpan.FromSeconds(3));
		}

		#endregion
	}
}
