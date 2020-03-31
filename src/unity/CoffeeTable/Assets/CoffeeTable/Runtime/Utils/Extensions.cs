using CoffeeTable.Common.Messaging.Core;
using CoffeeTable.Common.Messaging.Handling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Utils
{
	internal static class Extensions
	{
		public static void Wait (this IEnumerator func)
		{
			while (func.MoveNext())
				if (func.Current is IEnumerator next)
					next.Wait();
		}
	}
}
