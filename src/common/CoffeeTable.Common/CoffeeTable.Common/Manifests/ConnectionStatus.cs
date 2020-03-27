using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests
{
	public struct ConnectionStatus
	{
		public bool IsClientConnected { get; set; }
		public uint ServiceClientId { get; set; }
	}
}
