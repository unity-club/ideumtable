using CoffeeTable.Messaging.Core;
using CoffeeTable.Messaging.Handling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTable.Messaging.Tests
{
	class Program
	{
		static MessagingHandler handlerA;
		static MessagingHandler handlerB;

		static void Main(string[] args)
		{
			handlerA = new MessagingHandler(m => handlerB.Receive(m));
			handlerB = new MessagingHandler(async m =>
			{
				await Task.Delay(500);
				handlerA.Receive(m);
			});

			handlerA.Register<A>();

			handlerB.Send<int[]>(0, "test", new List<int> { 1, 2, 3, 4 })
				.OnSucceeded += a =>
				{
					Console.WriteLine("I received the data!");
				};

			Console.Read();
		}

		class A
		{
			[RequestHandler("test")]
			static void handler(Request<List<int>> request, Response<int[]> response)
			{
				response.Data = request.Data.ToArray();
				;
			}
		}

		class B
		{
		}
	}
}
