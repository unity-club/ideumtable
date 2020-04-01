using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using CoffeeTable.Comm;
using CoffeeTable.Common.Messaging.Core;
using CoffeeTable.Logging;

namespace CoffeeTable.Providers
{
	internal class TableProviderService : TableProviderBase, IReceiver
	{
		public int PortNumber = 4949;
		public int ModuleId = 0;

		private ConnectionManager mConnectionManager;

		public bool IsConnected { get; private set; } = false;

		public override bool StartProvider()
		{
			if (CreateConnection())
				IsConnected = true;
			return IsConnected;
		}

		public override void Dispose()
		{
			CloseConnection();
			IsConnected = false;
		}

		#region Connection Methods
		private void CloseConnection()
		{
			if (mConnectionManager != null && mConnectionManager.Connection != null)
			{
				mConnectionManager.Connection.Close();
				OnDisconnected();
			}
			mConnectionManager = null;
		}

		private bool CreateConnection()
		{
			TCPConnection connection;
			if (TCPConnection.TryOpen(new IPEndPoint(IPAddress.Loopback, PortNumber), out connection))
			{
				mConnectionManager = new ConnectionManager(connection, this, ModuleId, OnGeneralException, ConnectionClosed);
				OnConnected();
				return true;
			}
			else
			{
				OnFailedToConnect();
				return false;
			}
		}

		private void OnGeneralException(ConnectionManager c, Exception e)
		{
			Log.Error($"Unhandled exception in {nameof(TableProviderService)} — {e}");
			if (!c.Connection.IsClosed()) CloseConnection();
			else CreateConnection();
		}

		private void ConnectionClosed(ConnectionManager c)
		{
			Log.Out("Service connection was closed.");
			OnDisconnected();
		}

		public void Send(Message message)
		{
			if (!IsConnected) return;

			if (mConnectionManager != null && !mConnectionManager.Connection.IsClosed())
				mConnectionManager.Send(message);
			else Log.Warn("Failed to send message to service: no connection.");
		}

		public void Receive(Message message) => OnMessageReceived(message);

		#endregion
	}
}