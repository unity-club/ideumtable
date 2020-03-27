using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CoffeeTable.Common.Messaging.Handling
{
	public abstract class Response
	{
		internal static readonly Type GenericType = typeof(Response<>);
		internal PropertyInfo Property_Data => GetType().GetProperty(nameof(Response<Null>.Data));

		public bool Success { get; set; } = true;
		public string Details { get; set; }
	}

	public class Response<T> : Response
	{
		public T Data { get; set; }
	}
}
