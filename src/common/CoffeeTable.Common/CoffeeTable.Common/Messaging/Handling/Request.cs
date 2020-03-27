using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CoffeeTable.Common.Messaging.Handling
{
	public abstract class Request
	{
		internal static readonly Type GenericType = typeof(Request<>);
		internal PropertyInfo Property_Data => GetType().GetProperty(nameof(Request<Null>.Data));

		public string SenderName { get; internal set; }
		public uint SenderId { get; internal set; }
		public DateTime Sent { get; internal set; }
		public DateTime Received { get; internal set; }
	}

	public class Request<T> : Request
	{
		public T Data { get; set; }
	}
}
