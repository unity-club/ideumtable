using CoffeeTable.Common.Manifests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Applications
{
	public class Application
	{
		private static uint _appId = 1;

		public uint Id { get; }
		public string Name { get; private set; }
		public ApplicationType Type { get; private set; }
		public string Author { get; private set; }
		public string Description { get; private set; }
		public string RelativeExecutablePath { get; private set; }
		public string ExecutablePath { get; private set; }
		public string IconPath { get; private set; }
		public string LauncherName { get; private set; }
		public bool LaunchInFullscreen { get; private set; }

		public static Application CreateFromManifest(ApplicationManifest manifest, string executablePath, string iconPath)
		{
			if (manifest == null) return null;
			return new Application()
			{
				Name = manifest.Name?.Trim(),
				Type = manifest.Type,
				Author = manifest.Author?.Trim(),
				Description = manifest.Description?.Trim(),
				RelativeExecutablePath = manifest.ExecutablePath,
				ExecutablePath = executablePath,
				IconPath = iconPath,
				LauncherName = manifest.LauncherName,
				LaunchInFullscreen = manifest.LaunchInFullscreen
			};
		}

		private Application() => Id = _appId++;
	}
}
