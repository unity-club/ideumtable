using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests.Networking
{
	public class ApplicationsManifest
	{
		public ApplicationInfo[] InstalledApplications { get; set; }
		public ApplicationInstanceInfo[] RunningApplications { get; set; }
	}
}
