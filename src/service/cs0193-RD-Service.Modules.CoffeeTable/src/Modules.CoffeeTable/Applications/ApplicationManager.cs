using CoffeeTable.Common.Manifests;
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

			RegisterLaunchers();
			
			mApplicationStore.OnApplicationInstanceDestroyed += i => Log.Warn($"Application '{i.App.Name}' was destroyed.");
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

		// Call immediately after an ApplicationInstance's process has been terminated
		// and its window closed
		private void OnApplicationInstanceExitted (ApplicationInstance instance)
		{
			if (instance.Process != null && !instance.Process.HasExited)
				instance.Process.Kill();
			instance.State = ApplicationState.Destroyed;
			mApplicationStore.RemoveApplicationInstance(instance);
		}

		/// <summary>
		/// If there are half-screen apps running on the screen, swaps them so that the application running on the 
		/// left hand side of the screen runs on the right hand side of the screen and vice versa.
		/// </summary>
		/// <returns>A boolean indicating the success of the operation. If true, the application windows were successfully swapped.
		/// If false, the application windows could not be swapped because one or more of them was not fully loaded yet. </returns>
		public bool Swap ()
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
				return false; // Do not allow swapping if windows have not finished loading


			if (left != null) left.Layout = ApplicationLayout.RightPanel;
			if (right != null) right.Layout = ApplicationLayout.LeftPanel;

			mWindowManager.SizeWindow(left);
			mWindowManager.SizeWindow(right);

			return true;
		}
	}
}
