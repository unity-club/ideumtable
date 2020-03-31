using System;
using System.Collections.Generic;
using CoffeeTable.Common.Messaging.Core;

namespace CoffeeTable.Providers
{
	internal abstract class TableProviderBase : ITableProvider
	{

		public event Action Connected;
		public event Action Disconnected;
		public event Action FailedToConnect;
		public event Action<Message> MessageReceived;

		public abstract bool StartProvider();
		public abstract void Dispose();

		protected virtual void OnConnected() => Connected?.Invoke();
		protected virtual void OnDisconnected() => Disconnected?.Invoke();
		protected virtual void OnFailedToConnect() => FailedToConnect?.Invoke();
		protected virtual void OnMessageReceived(Message t) => MessageReceived?.Invoke(t);
	}
}