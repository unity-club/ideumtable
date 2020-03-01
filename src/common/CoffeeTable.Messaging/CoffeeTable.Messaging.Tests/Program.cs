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
		static void Main(string[] args)
		{
			MessageCallbackHandler handler = new MessageCallbackHandler();
			handler.AddCallback<int[]>("test", Foo);

			Message message = handler.GetMessage(0, "test", null);
			handler.ReceiveMessage(message);

			Console.ReadKey(true);
		}

		static void Foo (Message.SenderInfo sender, int[] data)
		{
			if (data == null)
			{
				Console.WriteLine("data is null");
				return;
			}
			Console.WriteLine("I have received your message. Mwahahaha");
			foreach (int i in data)
			{
				Console.WriteLine(i);
			}
		}
	}
}
