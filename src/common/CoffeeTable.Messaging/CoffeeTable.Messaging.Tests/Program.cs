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

			Message message = handler.GetMessage(0, "tesT", new int[] { 1, 2, 3, 4 });
			handler.ReceiveMessage(message);

			Console.ReadKey(true);
		}

		[CommandHandler("tESt")]
		void Bar (Message.SenderInfo sender)
		{
			Console.WriteLine("received without data");
		}

		[CommandHandler("TESt")]
		static void Foo(Message.SenderInfo sender)
		{
			Console.WriteLine("received statically without data");
		}
	}
}
