using CoffeeTable.Common.Manifests.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Messaging.Requests
{
	public class ServiceSubscriptionResponse
	{
		public ApplicationsManifest AppsManifest { get; set; }
		public uint SubscriberId { get; set; }
	}
}
