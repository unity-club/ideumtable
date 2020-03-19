using CoffeeTable.Manifests;
using CoffeeTable.Module.Launchers;
using CoffeeTable.Module.Messaging;
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
		private ILog mLog;
		private List<Application> mApplications = new List<Application>();
		private List<ApplicationInstance> mAppInstances = new List<ApplicationInstance>();

		private ILauncher mDefaultLauncher;
		private Dictionary<string, ILauncher> mLauncherMap = new Dictionary<string, ILauncher>();

		public ApplicationManager(ILog logger, IMessageRouter router)
		{
			mLog = logger;
			mMessageRouter = router;

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
				mLog.Info($"Registered launcher mapping: {{{launcherPair.Attr.LauncherName} -> [{launcherPair.LauncherType.Name}]}}");

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

			if (!IsLaunchable(app)) return null;

			ILauncher appLauncher;
			if (!mLauncherMap.TryGetValue(app.LauncherName, out appLauncher)) appLauncher = mDefaultLauncher;
			Process appProcess = appLauncher.LaunchApplication(app);
			if (appProcess == null) return null;

			return null;
		}

		private bool IsLaunchable (Application app)
		{
			if (app.Type == ApplicationType.Sidebar)
				return mAppInstances.Where(a => a.App.Type == ApplicationType.Sidebar).Count() < 2;
			if (app.Type == ApplicationType.Homescreen)
				return !mAppInstances.Any(a => a.App.Type == ApplicationType.Homescreen);

			if (mAppInstances.Any(a => a.IsFullscreen)) return false;
			return mAppInstances.Where(a => a.App.Type == ApplicationType.Application).Count() < 2;
		}
	}
}
