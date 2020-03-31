using CoffeeTable.Common.Manifests.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable
{
	public static class CoffeeTableExtensions
	{
		public static ApplicationInstanceInfo GetRunningApp (this ApplicationsManifest manifest, uint id)
		{
			if (manifest == null) return null;
			return manifest.RunningApplications.GetAppInstance(id);
		}

		public static IEnumerable<ApplicationInstanceInfo> GetRunningApps (this ApplicationsManifest manifest, string appName)
		{
			if (manifest == null) return null;
			return manifest.RunningApplications.GetAppInstances(appName);
		}

		public static ApplicationInstanceInfo GetRunningApp (this ApplicationsManifest manifest, string appName)
		{
			if (manifest == null) return null;
			return manifest.RunningApplications.GetAppInstance(appName);
		}

		public static ApplicationInfo GetInstalledApp(this ApplicationsManifest manifest, string appName)
		{
			if (manifest == null) return null;
			return manifest.InstalledApplications.GetApp(appName);
		}

		public static ApplicationInstanceInfo GetAppInstance (this ApplicationInstanceInfo[] instances, uint id)
		{
			if (instances == null) return null;
			return instances.Where(i => i.DestinationId == id).FirstOrDefault();
		}

		public static IEnumerable<ApplicationInstanceInfo> GetAppInstances (this ApplicationInstanceInfo[] instances, string appName)
		{
			if (instances == null) return null;
			return instances.Where(i => string.Equals(appName, i.AppInfo.Name, StringComparison.OrdinalIgnoreCase));
		}

		public static ApplicationInstanceInfo GetAppInstance (this ApplicationInstanceInfo[] instances, string appName)
		{
			return GetAppInstances(instances, appName).FirstOrDefault();
		}

		public static ApplicationInfo GetApp (this ApplicationInfo[] apps, string appName)
		{
			if (apps == null) return null;
			return apps.Where(a => string.Equals(appName, a.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
		}
	}
}
