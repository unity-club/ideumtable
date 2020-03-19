using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Util
{

	public static class HashCode
	{
		/*
		 * See https://stackoverflow.com/a/34006336/10149816
		 * for information concerning best algorithms for generation of
		 * hashcodes that produce the fewest collisions.
		 */
		public static int Combine (params object[] objects)
		{
			unchecked
			{
				int hash = 1009;
				foreach (object o in objects)
					hash = (hash * 9176) + o.GetHashCode();
				return hash;
			}
		}
	}
}
