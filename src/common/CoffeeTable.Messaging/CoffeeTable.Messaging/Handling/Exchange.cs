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

		public bool Complete { get; set; }
		public bool Success { get; set; }
		public bool TimedOut { get; set; }
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

		internal void SetTimedOut ()
		{
			Complete = true;
			Success = false;
			TimedOut = true;
			Completed = DateTime.Now;
			Details = $"Timed out";
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
				DateTime startTime = DateTime.Now;
				while (!Complete)
				{
					if ((DateTime.Now - startTime).TotalMilliseconds > Timeout)
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
