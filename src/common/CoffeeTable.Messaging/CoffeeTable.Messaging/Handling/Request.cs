using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeTable.Messaging.Handling
{
	public abstract class Request
	{
		public string SenderName { get; set; }
		public int SenderId { get; set; }
		public DateTime Sent { get; set; }
		public DateTime Received { get; set; }
	}

	public class Request<T> : Request
	{
		public T Data { get; set; }
	}
}
