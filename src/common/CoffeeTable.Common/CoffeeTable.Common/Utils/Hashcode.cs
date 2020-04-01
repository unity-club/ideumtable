using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Common.Utils
{
	internal static class HashCode
	{
		/*
		 * See https://stackoverflow.com/a/34006336/10149816
		 * for information concerning best algorithms for generation of
		 * hashcodes that produce the fewest collisions.
		 */
		public static int Combine (params object[] args)
		{
			const int seed = 1009;
			const int factor = 9176;

			int hash = seed;
			unchecked
			{
				foreach (object o in args)
					hash = (hash * factor) + o?.GetHashCode() ?? 0;
			}

			return hash;
		}
	}
}
