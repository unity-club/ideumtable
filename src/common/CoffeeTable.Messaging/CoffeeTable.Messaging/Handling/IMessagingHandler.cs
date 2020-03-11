using System;
using System.Collections.Generic;
using System.Text;
using CoffeeTable.Messaging.Core;

namespace CoffeeTable.Messaging.Handling
{
	public interface IMessagingHandler
	{
		Exchange<T> Send<T>(uint destinationId, string requestName, object data = null);
		void Receive(Message message);
		void Register(object o);
		void Register<T>();
	}
}
