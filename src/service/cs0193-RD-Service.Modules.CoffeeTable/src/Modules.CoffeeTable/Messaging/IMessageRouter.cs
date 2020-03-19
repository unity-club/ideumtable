using CoffeeTable.Messaging.Handling;
using CoffeeTable.Module.Applications;
using Ideum;
using Ideum.Networking.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Messaging
{
	public interface IMessageRouter : IApplicationCallbacksReceiver
	{
		IMessagingHandler Handler { get; }
		void OnMessageReceived(TcpMessage message);
		void OnClientDisconnected(IClient client);
	}
}
