using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeTable.Common.Messaging.Handling
{
	public sealed class None
	{
		internal static readonly Type NoneType = typeof(None);

		private None() { }

		public override bool Equals(object obj)
		{
			return obj == null || obj.GetType().Equals(NoneType);
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
