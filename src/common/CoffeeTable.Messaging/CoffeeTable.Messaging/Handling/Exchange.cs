using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTable.Messaging.Handling
{
	public abstract class Exchange 
	{
		internal static readonly Type GenericType = typeof(Exchange<>);
		internal PropertyInfo Property_Data => GetType().GetProperty(nameof(Exchange<Null>.Data));

		public bool Complete { get; internal set; }
		public bool Success { get; internal set; }
		public bool TimedOut { get; internal set; }
		public string Details { get; internal set; }
		public int Timeout { get; protected set; }
		public DateTime Requested { get; internal set; }
		public DateTime? Completed { get; internal set; }
		internal uint CorrelationId { get; set; }

		public int Delay
		{
			get
			{
				if (Completed != null) return ((Completed ?? DateTime.Now) - Requested).Milliseconds;
				else return 0;
			}
		}

		internal void SetTimedOut ()
		{
			Complete = true;
			Success = false;
			TimedOut = true;
			Completed = DateTime.Now;
			Details = "Timeout";
		}
	}

	public class Exchange<T> : Exchange 
	{
		public T Data { get; set; }

		public Exchange(int timeout)
		{
			Timeout = timeout;
		}

		public TaskAwaiter<Exchange<T>> GetAwaiter()
		{
			return Task.Run(() =>
			{
				while (!Complete)
				{
					if ((DateTime.Now - Requested).TotalMilliseconds > Timeout)
					{
						SetTimedOut();
						break;
					}
				}
				return this;
			}).GetAwaiter();
		}
	}
}
