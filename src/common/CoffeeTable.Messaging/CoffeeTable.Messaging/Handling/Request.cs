using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CoffeeTable.Messaging.Handling
{
	public abstract class Request
	{
		internal static readonly Type GenericType = typeof(Request<>);
		internal PropertyInfo Property_Data => GetType().GetProperty(nameof(Request<Null>.Data));

		public string SenderName { get; set; }
		public uint SenderId { get; set; }
		public DateTime Sent { get; set; }
		public DateTime Received { get; set; }
	}

	public class Request<T> : Request
	{
		public T Data { get; set; }
	}
}
