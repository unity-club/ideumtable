using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeTable.Messaging.Handling
{
	public abstract class Exchange 
	{
		public bool Complete { get; set; }
		public bool Success { get; set; }
		public string Details { get; set; }
		public int Timeout { get; set; }
		public DateTime Requested { get; set; }
		public DateTime? Completed { get; set; }
		internal uint CorrelationId { get; set; }
	}

	public class Exchange<T> : Exchange
	{
		public T Data { get; set; }
		public Action<Exchange<T>> OnCompleted { get; set; }
		public Action<Exchange<T>> OnSucceeded { get; set; }
		public Action<Exchange<T>> OnFailed { get; set; }
	}
}
