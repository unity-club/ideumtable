using CoffeeTable.Messaging.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Messaging.Tests
{
	class Program
	{
		static MessagingHandler handlerA, handlerB;
		static void Main(string[] args)
		{
			Program prog = new Program();

			handlerA = new MessagingHandler(m => handlerB.Receive(m));
			handlerB = new MessagingHandler(m => handlerA.Receive(m));

			handlerA.Register(new A());

			handlerB.Send<int[]>("request", 0, new int[] { 1, 2, 3, 4 }, a =>
			{
				Console.WriteLine("I received the data!");
				foreach (int i in a)
					Console.WriteLine(i);
			});

			Console.ReadKey(true);
		}

		class A
		{
			[RequestHandler("request")]
			static int[] foo (Message.MessageInfo info, int[] data)
			{
				Console.WriteLine("A receives the request statically");
				for (int i = 0; i < data.Length; i++)
					data[i] += 10;
				return data;
			}
		}

		class B
		{

		}
	}
}
