using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeTable.Messaging.Handling
{
	public sealed class Null
	{
		private Null() { }

		public override bool Equals(object obj)
		{
			return obj == null || obj.GetType().Equals(typeof(Null));
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
