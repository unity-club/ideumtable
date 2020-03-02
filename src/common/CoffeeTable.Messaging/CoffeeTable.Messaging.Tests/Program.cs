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
			handler.AddCallbacks(new Program());

			Message message = handler.GetMessage(0, "test", new int[] { 1, 2, 3, 4 });
			handler.ReceiveMessage(message);

			Console.ReadKey(true);
		}

		[CommandHandler("test")]
		void Bar (Message.SenderInfo sender, int[] data)
		{
			Console.WriteLine("received");
			foreach (int i in data) Console.WriteLine(i);
		}

		[CommandHandler("test")]
		static void Foo(Message.SenderInfo sender, int[] data, int y)
		{
			Console.WriteLine("received statically");
			foreach (int i in data) Console.WriteLine(i);
		}
	}
}
