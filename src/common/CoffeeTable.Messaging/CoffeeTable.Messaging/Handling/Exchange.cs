using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CoffeeTable.Messaging.Handling
{
	public abstract class Exchange 
	{
		internal static readonly Type GenericType = typeof(Exchange<>);
		internal PropertyInfo Property_Data => GetType().GetProperty(nameof(Exchange<Null>.Data));
		internal FieldInfo Field_OnCompleted => GetType().GetField(nameof(Exchange<Null>.OnCompleted), BindingFlags.Instance | BindingFlags.NonPublic);
		internal FieldInfo Field_OnSucceeded => GetType().GetField(nameof(Exchange<Null>.OnSucceeded), BindingFlags.Instance | BindingFlags.NonPublic);
		internal FieldInfo Field_OnFailed => GetType().GetField(nameof(Exchange<Null>.OnFailed), BindingFlags.Instance | BindingFlags.NonPublic);

		public bool Complete { get; set; }
		public bool Success { get; set; }
		public string Details { get; set; }
		public int Timeout { get; protected set; }
		public DateTime Requested { get; set; }
		public DateTime? Completed { get; set; }
		internal uint CorrelationId { get; set; }

		public int Delay
		{
			get
			{
				if (Completed != null) return ((Completed ?? DateTime.Now) - Requested).Milliseconds;
				else return 0;
			}
		}
	}

	public class Exchange<T> : Exchange
	{
		public T Data { get; set; }
		public event Action<Exchange<T>> OnCompleted;
		public event Action<Exchange<T>> OnSucceeded;
		public event Action<Exchange<T>> OnFailed;

		public Exchange(int timeout)
		{
			Timeout = timeout;
		}
	}
}
