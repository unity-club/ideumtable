using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Editor
{
	public static class ExtensionMethods
	{
		public static bool BoundedByAny (this string str, params string[] input)
		{
			if (str == null) return false;
			foreach (string s in input)
				if (str.StartsWith(s)) return true;
				else if (str.EndsWith(s)) return true;
			return false;
		}
	}
}
