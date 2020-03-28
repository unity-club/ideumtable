using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
		[JsonConverter(typeof(StringEnumConverter))]
		public ApplicationLayout Layout { get; set; }
		public ConnectionStatus Connection { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public ApplicationState State { get; set; }
		public ApplicationRect Rect { get; set; }
	}
}
