using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Manifests.Networking.Comparers
{
	public class ApplicationInfoComparer : IEqualityComparer<ApplicationInfo>
	{
		/// <summary>
		/// Returns true if and only if the applications <paramref name="a"/> and <paramref name="b"/> have the same <see cref="ApplicationInfo.AppId"/>,
		/// that is, they refer to the same application.
		/// </summary>
		public bool Equals(ApplicationInfo a, ApplicationInfo b)
		{
			if (a == null) return b == null;
			else if (b == null) return false;
			else return a.AppId == b.AppId;
		}

		/// <summary>
		/// Returns the hashcode of the <see cref="ApplicationInfo"/> object's <see cref="ApplicationInfo.AppId"/>
		/// </summary>
		public int GetHashCode(ApplicationInfo obj)
		{
			return obj?.AppId.GetHashCode() ?? 0;
		}
	}
}
