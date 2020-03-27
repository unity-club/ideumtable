using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeTable.Common.Messaging.Handling
{
	public sealed class Null
	{
		internal static readonly Type NullType = typeof(Null);

		private Null() { }

		public override bool Equals(object obj)
		{
			return obj == null || obj.GetType().Equals(NullType);
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public override string ToString()
		{
			return string.Empty;
		}
	}
}
