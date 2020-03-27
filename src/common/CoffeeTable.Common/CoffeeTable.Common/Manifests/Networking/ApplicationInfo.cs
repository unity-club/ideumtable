using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests.Networking
{
	public class ApplicationInfo
	{
		public uint AppId { get; set; }
		public string Name { get; set; }
		public ApplicationType Type { get; set; }
		public string Author { get; set; }
		public string Description { get; set; }
		public string IconPath { get; set; }
		public bool LaunchInFullscreen { get; set; }
	}
}
