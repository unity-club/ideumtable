﻿using CoffeeTable.Common.Manifests;
using CoffeeTable.Common.Manifests.Networking;
using CoffeeTable.Module.Messaging;
using Ideum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Applications
{
	public class ApplicationStore : IDisposable, IManifestConvertible<ApplicationsManifest>
	{
		private const string RootDirectoryName = "CoffeeTable";
		private const string AppDirectoryName = "apps";
		private const string IconFileName = "icon";
		private const string AppManifestFileName = "manifest.json";

		private bool mDisposed;

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

		public ApplicationInstance LeftPanelApp => mAppInstances
			.Where(i => i.App.Type == ApplicationType.Application && i.Layout == ApplicationLayout.LeftPanel)
			.FirstOrDefault();

		public ApplicationInstance RightPanelApp => mAppInstances
			.Where(i => i.App.Type == ApplicationType.Application && i.Layout == ApplicationLayout.RightPanel)
			.FirstOrDefault();

		public ApplicationInstance FullscreenApp => mAppInstances
			.Where(i => i.App.Type == ApplicationType.Application && i.Layout == ApplicationLayout.Fullscreen)
			.FirstOrDefault();

		public IEnumerable<ApplicationInstance> Instances => mAppInstances;
		public IEnumerable<Application> Applications => mApplications;

		public event Action<ApplicationInstance> OnApplicationInstanceCreated;
		public event Action<ApplicationInstance> OnApplicationInstanceDestroyed;
		public event Action<ApplicationStore> OnNotifyApplicationsChanged;

		private ILog Log = LogManager.GetLogger(typeof(ApplicationManager));

		public ApplicationStore ()
		{
			RegisterApplications();
		}

		public void NotifyApplicationsChanged() => OnNotifyApplicationsChanged?.Invoke(this);

		private void RegisterApplications()
		{
			string rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), RootDirectoryName, AppDirectoryName);
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

			ApplicationManifest manifest;
			try
			{
				string appManifestJson = File.ReadAllText(Path.Combine(appFolderDirectory.FullName, AppManifestFileName));
				manifest = JsonConvert.DeserializeObject<ApplicationManifest>(appManifestJson);
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

			var executablePath = Path.Combine(appFolderDirectory.FullName, manifest.ExecutablePath);

			// Validate the application
			if (string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				Log.Warn($"Application '{appFolderDirectory.Name}' could not be parsed because it has an invalid application name specified in its manifest file.");
				return null;
			}

			if (string.IsNullOrWhiteSpace(iconPath))
			{
				Log.Warn($"Application '{manifest.Name}' could not be parsed because an icon file could not be found in the root directory of the application.");
				return null;
			}

			if (string.IsNullOrWhiteSpace(manifest.ExecutablePath) || !File.Exists(executablePath))
			{
				Log.Warn($"Application '{manifest.Name}' could not be parsed because the application manifest did not specify an executable file relative to the root application folder," +
					$" or such an executable does not exist.");
				return null;
			}

			// Convert to a proper application class
			Application app = Application.CreateFromManifest(manifest,
				executablePath,
				iconPath);

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

		public IEnumerable<ApplicationInstance> GetInstancesOfType(ApplicationType type)
			=> mAppInstances.Where(i => i.App.Type == type);

		public ApplicationInstance GetFromId(uint destinationId)
			=> mAppInstances.Where(i => i.Id == destinationId)
			.FirstOrDefault();

		public bool AddApplicationInstance (ApplicationInstance instance)
		{
			if (instance == null) return false;
			bool success = mAppInstances.Add(instance);
			if (success)
			{
				OnApplicationInstanceCreated?.Invoke(instance);
				NotifyApplicationsChanged();
			}
			return success;
		}

		public bool RemoveApplicationInstance (ApplicationInstance instance)
		{
			if (instance == null) return false;
			bool success = mAppInstances.Remove(instance);
			if (success)
			{
				OnApplicationInstanceDestroyed?.Invoke(instance);
				NotifyApplicationsChanged();
			}
			return success;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (mDisposed) return;

			foreach (var instance in mAppInstances)
			{
				if (instance.Process == null) continue;
				instance.Process.Refresh();
				if (!instance.Process.HasExited)
					try { 
						instance.Process.Kill();
					}
					catch (Win32Exception) { }
			}

			mDisposed = true;
		}

		public ApplicationsManifest ToManifest()
		{
			return new ApplicationsManifest
			{
				InstalledApplications = mApplications.Select(app => app.ToManifest()).ToArray(),
				RunningApplications = mAppInstances.Select(instance => instance.ToManifest()).ToArray()
			};
		}

		~ApplicationStore()
		{
			Dispose(false);
		}
	}
}
