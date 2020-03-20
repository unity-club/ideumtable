using CoffeeTable.Manifests;
using CoffeeTable.Module.Launchers;
using CoffeeTable.Module.Messaging;
using CoffeeTable.Module.Window;
using Ideum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Applications
{
	public class ApplicationManager
	{
		private const string RootDirectoryName = "CoffeeTable";
		private const string AppDirectoryName = "apps";
		private const string IconFileName = "icon";
		private const string AppManifestFileName = "manifest.json";

		private IMessageRouter mMessageRouter;
		private IWindowManager mWindowManager;
		private ILog mLog = LogManager.GetLogger(typeof(ApplicationManager));
		private List<Application> mApplications = new List<Application>();
		private List<ApplicationInstance> mAppInstances = new List<ApplicationInstance>();

		private ApplicationInstance mLeftSidebar, mRightSidebar, mHomescreen;

		private ILauncher mDefaultLauncher;
		private Dictionary<string, ILauncher> mLauncherMap = new Dictionary<string, ILauncher>();

		public ApplicationManager(IMessageRouter router, IWindowManager windowManager)
		{
			mMessageRouter = router;
			mWindowManager = windowManager;

			RegisterLaunchers();
			RegisterApplications();
		}

		private void RegisterApplications()
		{
			mApplications.Clear();
			string rootPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), RootDirectoryName, AppDirectoryName);
			if (!Directory.Exists(rootPath))
			{
				Directory.CreateDirectory(rootPath);
				return;
			}

			DirectoryInfo rootDirectory = new DirectoryInfo(rootPath);
			foreach (var info in rootDirectory.EnumerateDirectories())
			{
				Application app = RegisterApplication(info);
				if (app != null)
				{
					mApplications.Add(app);
					mLog.Info($"Successfully registered '{app.Name}' with app ID #{app.Id}");
				} else mLog.Warn($"Failed to register application '{info.Name}'");
			}
		}

		private Application RegisterApplication(DirectoryInfo appFolderDirectory)
		{
			// First get manifest file and deserialize to proper application class
			var appFiles = appFolderDirectory.EnumerateFiles();
			if (!appFiles.Any(f => f.Exists && f.Name.Equals(AppManifestFileName, StringComparison.OrdinalIgnoreCase)))
			{
				mLog.Warn($"Application '{appFolderDirectory.Name}' could not be parsed because it did not contain a {AppManifestFileName} file.");
				return null;
			}

			ApplicationManifest appManifest;
			try
			{
				string appManifestJson = File.ReadAllText(Path.Combine(appFolderDirectory.FullName, AppManifestFileName));
				appManifest = JsonConvert.DeserializeObject<ApplicationManifest>(appManifestJson);
			} catch (JsonException)
			{
				mLog.Warn($"Application '{appFolderDirectory.Name}' could not be parsed because it's manifest file did not contain valid JSON.");
				return null;
			}

			// Get path to icon
			string iconPath = (from file in appFiles
							   where file.Exists && IconFileName.Equals(Path.GetFileNameWithoutExtension(file.FullName), StringComparison.OrdinalIgnoreCase)
							   select file.FullName).FirstOrDefault();

			// Convert to a proper application class
			Application app = Application.CreateFromManifest(appManifest,
				Path.Combine(appFolderDirectory.FullName, appManifest.ExecutablePath),
				iconPath);

			// Validate the application
			if (string.IsNullOrWhiteSpace(app.Name) || app.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				mLog.Warn($"Application '{appFolderDirectory.Name}' could not be parsed because it has an invalid application name specified in its manifest file.");
				return null;
			}

			if (string.IsNullOrWhiteSpace(app.IconPath))
			{
				mLog.Warn($"Application '{app.Name}' could not be parsed because an icon file could not be found in the root directory of the application.");
				return null;
			}

			if (string.IsNullOrWhiteSpace(app.ExecutablePath) || !File.Exists(app.ExecutablePath))
			{
				mLog.Warn($"Application '{app.Name}' could not be parsed because the application manifest did not specify an executable file relative to the root application folder," +
					$" or such an executable does not exist.");
				return null;
			}

			return app;
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
				mLog.Info($"Registered launcher mapping: {{{launcherPair.Attr.LauncherName} -> {launcherPair.LauncherType.Name}}}");

				if (launcher is DefaultLauncher)
					mDefaultLauncher = launcher;
			}
		}

		public Application GetApplication (string appName)
		{
			return (from app in mApplications
					where app.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)
					select app).FirstOrDefault();
		}

		public Application GetApplication (uint id)
		{
			return (from app in mApplications
					where app.Id == id
					select app).FirstOrDefault();
		}

		public ApplicationInstance LaunchApplication (Application app)
		{
			if (app == null) return null;

			if (!IsLaunchableWithLayout(app, out ApplicationLayout appLayout)) return null;

			ILauncher appLauncher;
			if (!mLauncherMap.TryGetValue(app.LauncherName ?? string.Empty, out appLauncher)) appLauncher = mDefaultLauncher;
			Process appProcess = appLauncher.LaunchApplication(app);
			if (appProcess == null) return null;

			ApplicationInstance instance = new ApplicationInstance(app, appProcess);

			// Get callback on process exitted
			appProcess.EnableRaisingEvents = true;
			appProcess.Exited += (o, e) => DestroyApplicationInstance(instance);
			instance.State = ApplicationState.Starting;
			instance.Layout = appLayout;

			// Cache sidebars and homescreen instances when they are created
			if (instance.App.Type == ApplicationType.Sidebar)
			{
				if (instance.Layout == ApplicationLayout.LeftPanel) mLeftSidebar = instance;
				else if (instance.Layout == ApplicationLayout.RightPanel) mRightSidebar = instance;
			}
			else if (instance.App.Type == ApplicationType.Homescreen) mHomescreen = instance;

			mWindowManager.OnApplicationInstanceCreated(instance);
			mMessageRouter.OnApplicationInstanceCreated(instance);

			// Configure window and animate the window in
			OpenWindowAnimation(instance);

			return instance;
		}

		private async void OpenWindowAnimation (ApplicationInstance instance)
		{
			mWindowManager.ConfigureApplicationWindow(instance);

			//
			// TODO: VERY TEMPORARY
			//
			while (string.IsNullOrEmpty(instance.Process.MainWindowTitle))
			{
				System.Threading.Thread.Sleep(50);
				instance.Process.Refresh();
			}

			await mWindowManager.AnimateWindow(instance, AnimateWindowMode.OpenWindow);
			instance.State = ApplicationState.Running;
		}

		// Returns true if the given application can be launched, and false if it cannot.
		// If the application can be launched, produces the layout it will be launched with.
		private bool IsLaunchableWithLayout (Application app, out ApplicationLayout layout)
		{
			layout = ApplicationLayout.Fullscreen;
			switch (app.Type)
			{
				case ApplicationType.Sidebar:
					int sidebarCount = mAppInstances.Where(i => i.App.Type == ApplicationType.Sidebar).Count();
					if (sidebarCount == 0) layout = ApplicationLayout.LeftPanel;
					else if (sidebarCount == 1) layout = ApplicationLayout.RightPanel;
					else if (sidebarCount >= 2) return false;
					return true;

				case ApplicationType.Homescreen:
					return mAppInstances.Any(i => i.App.Type == ApplicationType.Homescreen);

				case ApplicationType.Application:
					if (mAppInstances.Any(i => i.Layout == ApplicationLayout.Fullscreen))
						return false;
					if (app.LaunchInFullscreen) return !mAppInstances.Any(i => i.App.Type == ApplicationType.Application);
					else
					{
						// This application wants to be launched in half-screen,
						// so we will attempt to find a half of the screen where it can live.
						if (!mAppInstances.Any(i => i.Layout == ApplicationLayout.LeftPanel))
						{
							layout = ApplicationLayout.LeftPanel;
							return true;
						}
						else if (!mAppInstances.Any(i => i.Layout == ApplicationLayout.RightPanel))
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
		private void DestroyApplicationInstance (ApplicationInstance instance)
		{
			instance.State = ApplicationState.Destroyed;
			mAppInstances.Remove(instance);
			mMessageRouter.OnApplicationInstanceDestroyed(instance);
		}
	}
}
