using CoffeeTable.Common.Messaging.Core;
using CoffeeTable.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Text;

namespace CoffeeTable.Comm
{
	internal class ConnectionManager : ISender
	{
		public int ModuleId { get; private set; }
		public IComm Connection { get; private set; }
		public string Destination => Connection?.Socket.RemoteEndPoint.ToString();

		private IReceiver _msgReceiver;
		private object _parserLock = new object();
		private ByteMessageParser _parser;
		private Action<ConnectionManager> _connectionClosed;
		private Action<ConnectionManager, Exception> _generalExceptionAction;

		/// <summary>
		/// Creates a Connection Translator.
		/// </summary>
		/// <param name="connection">The Connection instance to use</param>
		/// <param name="onMsgRecieved">Called when a Msg instance is created from incoming byte arrays</param>
		/// <param name="generalExceptionAction">Called when an exception is thrown</param>
		/// <param name="connectionClosed">Called when the Connection closes</param>
		public ConnectionManager(IComm connection, IReceiver onMsgRecieved, int moduleId,
			Action<ConnectionManager, Exception> generalExceptionAction,
			Action<ConnectionManager> connectionClosed = null)
		{
			if (connection == null) throw new ArgumentException();
			Connection = connection;
			ModuleId = moduleId;
			_parser = new ByteMessageParser(PayloadCallback, ExceptionCallback);
			_msgReceiver = onMsgRecieved;
			_generalExceptionAction = generalExceptionAction;
			_connectionClosed = connectionClosed;
			Connection.Bind(ConnectionRecieve, OnConnectionClosed);
		}

		private void ExceptionCallback(Exception obj) => _generalExceptionAction?.Invoke(this, obj);
		private void OnConnectionClosed() => _connectionClosed?.Invoke(this);

		public void Send(Message message)
		{
			if (message == null) return;
			if (ModuleId == 0) return;

			var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

			TcpMessage tcpMessage = new TcpMessage
			{
				ModuleId = (uint) ModuleId,
				MessageId = 1,
				Payload = messageBytes
			};

			Connection.Send(ByteMessageParser.CreateMessage(tcpMessage.CreateMessage()));
		}

		public void Close() => Connection?.Close();

		private void ConnectionRecieve(byte[] raw)
		{
			//NOTE: prevent parser state from getting corrupted by multiple threads accessing the data.
			lock (_parserLock)
				_parser.Consume(raw);
		}

		private void PayloadCallback(byte[] bytes)
		{
			if (bytes == null)
			{
				Log.Out("No bytes are written.");
				_msgReceiver.Receive(null);
				return;
			}

			if (Connection == null || Connection.Socket == null)
			{
				Log.Error("Connection or socket is null for payload callback.");
				return;
			}

			TcpMessage tcpMessage = new TcpMessage();
			if (tcpMessage.DecodeMessage(bytes))
			{
				Message message;
				try { message = JsonConvert.DeserializeObject<Message>(Encoding.UTF8.GetString(tcpMessage.Payload)); }
				catch { return; }

				_msgReceiver.Receive(message);
			}
		}
	}
}