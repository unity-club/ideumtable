using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests.Networking
{
	public class ApplicationInstanceInfo
	{
		public ApplicationInfo AppInfo { get; set; }
		public uint DestinationId { get; set; }
		public ApplicationLayout Layout { get; set; }
		public ConnectionStatus Connection { get; set; }
		public ApplicationState State { get; set; }
		public ApplicationRect Rect { get; set; }
	}
}
