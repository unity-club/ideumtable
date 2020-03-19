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
			handlerB = new MessagingHandler(m =>
			{
				handlerA.Receive(m);
			});

			handlerA.Register(new A());
			handlerB.Timeout = 2000;

			SendMessage();

			Console.Read();
		}

		static async void SendMessage ()
		{
			Stopwatch st = new Stopwatch();
			st.Start();
			var data = await handlerB.Send(0, "tESt", 512);
			st.Stop();
			Console.WriteLine($"Send Message Operation took {st.ElapsedMilliseconds} ms and was " + (data.Success ? "successful." : "not successful."));
		}

		class A
		{
			[RequestHandler("TesT")]
			void handler(Request<uint?> request, Response<Null> response)
			{
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
