using System;
using System.Collections.Generic;
using System.Text;
using CoffeeTable.Common.Messaging.Core;

namespace CoffeeTable.Common.Messaging.Handling
{
	public interface IMessagingHandler
	{
		Exchange<T> Send<T>(uint destinationId, string requestName, object data = null);
		void Receive(Message message);
		void Register(object o);
		void Register<T>();
	}
}
