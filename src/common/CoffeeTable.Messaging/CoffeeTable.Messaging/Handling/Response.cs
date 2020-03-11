using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeTable.Messaging.Handling
{
	public abstract class Response
	{
		public bool Success { get; set; } = true;
		public string Details { get; set; }
	}

	public class Response<T> : Response
	{
		public T Data { get; set; }
	}
}
