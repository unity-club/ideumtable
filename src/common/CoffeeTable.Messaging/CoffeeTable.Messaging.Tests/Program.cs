using CoffeeTable.Messaging.Core;
using CoffeeTable.Messaging.Handling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTable.Messaging.Tests
{
	class Program
	{
		static void Main(string[] args)
		{
			MessagingHandler handler = new MessagingHandler(null);
			handler.Register<Program>();
			
			Console.Read();
		}

		[RequestHandler("test")]
		static void handlertest (Request<int[]> request, Response<Null> response)
		{
			
		}
	}
}
