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
				await Task.Delay(TimeSpan.FromMilliseconds(5));
				handlerA.Receive(m);
			});

			handlerA.Register(new A());

			handlerB.Send<Data<int>>(0, "tESt", new List<int> { 1, 2, 3, 4 })
				.OnSucceeded += a =>
				{
					Console.WriteLine(a.Data.GenericField);
					Console.WriteLine("I received the data!");
				};

			Console.Read();
		}

		class A
		{
			[RequestHandler("TesT")]
			void handler(Request<List<int>> request, Response<Data<int>> response)
			{
				response.Data = new Data<int>();
				response.Data.GenericField = 10;
				;
			}
		}

		class B
		{
		}

		class Data<T>
		{
			public T GenericField;
			public int Field;
			public string Property { get; set; } = "hello";
			public char[] FieldArray = new char[] { '1', 'a', 'd'};
		}
	}
}
