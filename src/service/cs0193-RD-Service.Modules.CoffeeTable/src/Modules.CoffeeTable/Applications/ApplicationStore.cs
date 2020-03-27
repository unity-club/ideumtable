using CoffeeTable.Manifests;
using Ideum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Applications
{
	public class ApplicationStore
	{
		private const string RootDirectoryName = "CoffeeTable";
		private const string AppDirectoryName = "apps";
		private const string IconFileName = "icon";
		private const string AppManifestFileName = "manifest.json";

		private HashSet<Application> mApplications = new HashSet<Application>();
		private HashSet<ApplicationInstance> mAppInstances = new HashSet<ApplicationInstance>();

		public ApplicationInstance HomeScreen => mAppInstances
			.Where(i => i.App.Type == ApplicationType.Homescreen)
			.FirstOrDefault();

		public ApplicationInstance LeftSidebar => mAppInstances
			.Where(i => i.App.Type == ApplicationType.Sidebar && i.Layout == ApplicationLayout.LeftPanel)
			.FirstOrDefault();

		public ApplicationInstance RightSidebar => mAppInstances
			.Where(i => i.App.Type == ApplicationType.Sidebar && i.Layout == ApplicationLayout.RightPanel)
			.FirstOrDefault();

		public IEnumerable<ApplicationInstance> Sidebars => mAppInstances
			.Where(i => i.App.Type == ApplicationType.Sidebar);

		public IEnumerable<ApplicationInstance> Instances => mAppInstances;

		public event Action<ApplicationInstance> OnApplicationInstanceCreated;
		public event Action<ApplicationInstance> OnApplicationInstanceDestroyed;

		private ILog Log = LogManager.GetLogger(typeof(ApplicationManager));

		public ApplicationStore ()
		{
			RegisterApplications();
		}

		private void RegisterApplications()
		{
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
					Log.Info($"Successfully registered '{app.Name}' with app ID #{app.Id}");
				}
				else Log.Warn($"Failed to register application '{info.Name}'");
			}
		}

		private Application RegisterApplication(DirectoryInfo appFolderDirectory)
		{
			// First get manifest file and deserialize to proper application class
			var appFiles = appFolderDirectory.EnumerateFiles();
			if (!appFiles.Any(f => f.Exists && f.Name.Equals(AppManifestFileName, StringComparison.OrdinalIgnoreCase)))
			{
				Log.Warn($"Application '{appFolderDirectory.Name}' could not be parsed because it did not contain a {AppManifestFileName} file.");
				return null;
			}

			ApplicationManifest appManifest;
			try
			{
				string appManifestJson = File.ReadAllText(Path.Combine(appFolderDirectory.FullName, AppManifestFileName));
				appManifest = JsonConvert.DeserializeObject<ApplicationManifest>(appManifestJson);
			}
			catch (JsonException)
			{
				Log.Warn($"Application '{appFolderDirectory.Name}' could not be parsed because it's manifest file did not contain valid JSON.");
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
				Log.Warn($"Application '{appFolderDirectory.Name}' could not be parsed because it has an invalid application name specified in its manifest file.");
				return null;
			}

			if (string.IsNullOrWhiteSpace(app.IconPath))
			{
				Log.Warn($"Application '{app.Name}' could not be parsed because an icon file could not be found in the root directory of the application.");
				return null;
			}

			if (string.IsNullOrWhiteSpace(app.ExecutablePath) || !File.Exists(app.ExecutablePath))
			{
				Log.Warn($"Application '{app.Name}' could not be parsed because the application manifest did not specify an executable file relative to the root application folder," +
					$" or such an executable does not exist.");
				return null;
			}

			return app;
		}

		public Application GetApplication(string appName)
		{
			return (from app in mApplications
					where app.Name.Equals(appName, StringComparison.OrdinalIgnoreCase)
					select app).FirstOrDefault();
		}

		public Application GetApplication(uint id)
		{
			return (from app in mApplications
					where app.Id == id
					select app).FirstOrDefault();
		}

		public bool AddApplicationInstance (ApplicationInstance instance)
		{
			bool success = mAppInstances.Add(instance);
			if (success) OnApplicationInstanceCreated?.Invoke(instance);
			return success;
		}

		public bool RemoveApplicationInstance (ApplicationInstance instance)
		{
			bool success = mAppInstances.Remove(instance);
			if (success) OnApplicationInstanceDestroyed?.Invoke(instance);
			return success;
		}
	}
}
