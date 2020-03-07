using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Manifests
{
	/// <summary>
	/// Represents the JSON manifest containing basic information about the Coffee Table's subsystems.
	/// </summary>
	/// <remarks>
	/// This class should represent the JSON manifest file stored at the root level of the Appdata/Roaming/CoffeeTable folder.
	/// </remarks>
	public class CoffeeTableFileManifest
	{
		public int ServiceHttpPort { get; set; }
		public int ServiceTcpPort { get; set; }
		public string ServiceExecutablePath { get; set; }
		public string LauncherPath { get; set; }
	}
}
