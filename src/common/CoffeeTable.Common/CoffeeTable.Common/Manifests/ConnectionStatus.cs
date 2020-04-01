using CoffeeTable.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests
{
	public struct ConnectionStatus : IEquatable<ConnectionStatus>
	{
		public bool IsClientConnected { get; set; }
		public uint ServiceClientId { get; set; }

		public override bool Equals(object obj) => obj is ConnectionStatus r && Equals(r);
		public static bool operator ==(ConnectionStatus left, ConnectionStatus right) => left.Equals(right);
		public static bool operator !=(ConnectionStatus left, ConnectionStatus right) => !(left == right);

		public bool Equals(ConnectionStatus other)
		{
			return other.IsClientConnected == IsClientConnected
				&& other.ServiceClientId == ServiceClientId;
		}

		public override int GetHashCode() => HashCode.Combine(IsClientConnected, ServiceClientId);
	}
}
