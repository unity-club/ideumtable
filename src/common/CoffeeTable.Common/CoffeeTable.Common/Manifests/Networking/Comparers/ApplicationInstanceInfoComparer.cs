using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests.Networking.Comparers
{
	public class ApplicationInstanceInfoComparer : IEqualityComparer<ApplicationInstanceInfo>
	{
		/// <summary>
		/// Returns true if and only if the application instances <paramref name="a"/> and <paramref name="b"/> have the same <see cref="ApplicationInstanceInfo.DestinationId"/>,
		/// that is, they refer to the same application instance.
		/// </summary>
		public bool Equals(ApplicationInstanceInfo a, ApplicationInstanceInfo b)
		{
			if (a == null) return b == null;
			else if (b == null) return false;
			else return a.DestinationId == b.DestinationId;
		}

		/// <summary>
		/// Returns the hashcode of the <see cref="ApplicationInstanceInfo"/> object's <see cref="ApplicationInstanceInfo.DestinationId"/>
		/// </summary>
		public int GetHashCode(ApplicationInstanceInfo obj)
		{
			return obj?.DestinationId.GetHashCode() ?? 0;
		}
	}
}
