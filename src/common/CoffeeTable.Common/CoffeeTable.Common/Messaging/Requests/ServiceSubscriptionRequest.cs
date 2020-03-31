using CoffeeTable.Common.Manifests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Messaging.Requests
{
	public class ServiceSubscriptionRequest
	{
		public bool IsSimulator { get; set; }
		public int ProcessId { get; set; }
		public ApplicationManifest SimulatedApplication { get; set; } 
	}
}
