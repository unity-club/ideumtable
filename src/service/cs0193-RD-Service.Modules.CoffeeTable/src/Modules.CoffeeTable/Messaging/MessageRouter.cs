﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoffeeTable.Messaging.Core;
using CoffeeTable.Messaging.Handling;
using CoffeeTable.Module.Applications;
using Ideum;
using Ideum.Networking.Transport;
using Newtonsoft.Json;

namespace CoffeeTable.Module.Messaging
{
	public class MessageRouter : IMessageRouter
	{
		private const string SubscriptionKeyword = "subscribe";
		private const uint ModuleId = 1;
		private const string ModuleName = "CoffeeTable";

		public IMessagingHandler Handler { get; private set; }

		private Action<uint, TcpMessage> mServiceSender;
		private HashSet<ApplicationInstance> mAppInstances = new HashSet<ApplicationInstance>();

		public MessageRouter (Action<uint, TcpMessage> serviceSender)
		{
			mServiceSender = serviceSender;
			Handler = new MessagingHandler(RouteMessageFromHandlerToClient);
		}

		public void OnMessageReceived(TcpMessage raw)
		{
			if (raw == null) return;

			Message message;
			try
			{
				string json = Encoding.UTF8.GetString(raw.Payload);
				message = JsonConvert.DeserializeObject<Message>(json);
			}
			catch (JsonException) { return; }

			// Find the connected application instance that represents the sender
			ApplicationInstance sender = mAppInstances
				.Where(i => i.Connection.IsClientConnected && i.Connection.ServiceClientId == raw.ClientId)
				.FirstOrDefault();

			if (sender != null) RouteMessage(sender, message);
			else
			{
				// The client that sent this message is not a registered application
				// So the only valid request they can make is to register themselves
				// with the service.
				if (SubscriptionKeyword.Equals(message.Request, StringComparison.OrdinalIgnoreCase))
				{
					int pid;
					try { pid = JsonConvert.DeserializeObject<int>(message.Data); }
					catch (JsonException) { return; }

					Message response = new Message()
					{
						Data = null,
						CorrelationId = message.Id
					};

					if (SubscribeClient(pid, raw.ClientId)) response.Success = true;
					else response.Success = false;

					SendMessage(response, raw.ClientId);
				} else
				{
					// This client has not subscribed to the service, and is trying to send a request.
					// Send a message saying that it cannot do this.
					Message response = new Message()
					{
						Data = null,
						CorrelationId = message.Id,
						Success = false,
						Details = $"Cannot perform request '{message.Request}' because the client is not subscribed to the service."
					};

					SendMessage(response, raw.ClientId);
				}
			}
		}

		public void OnClientDisconnected (IClient client)
		{
			var connected = mAppInstances
				.Where(i => i.Connection.IsClientConnected && i.Connection.ServiceClientId == client.Id);

			foreach (var instance in connected)
				instance.Connection = new ConnectionStatus
				{
					IsClientConnected = false,
					ServiceClientId = 0
				};
		}

		// Routes a message received externally from an application instance
		private void RouteMessage (ApplicationInstance sender, Message message)
		{
			message.SenderId = sender.Id;
			message.SenderName = sender.App.Name;
			if (message.DestinationId == ModuleId) Handler.Receive(message);
			else
			{
				ApplicationInstance receiving = mAppInstances
					.Where(i => i.Connection.IsClientConnected && i.Id == message.DestinationId)
					.FirstOrDefault();
				if (receiving != null)
					SendMessage(message, receiving.Connection.ServiceClientId);
				else SendMessage(GetNoSuchDestinationResponse(message.Id), sender.Connection.ServiceClientId);
			}
		}

		// Subscribes and connects and application with the given processId and clientId
		private bool SubscribeClient (int processId, uint clientId)
		{
			ApplicationInstance instance = mAppInstances.Where(i => i.ProcessId == processId).FirstOrDefault();
			if (instance == null) return false;
			instance.Connection = new ConnectionStatus
			{
				IsClientConnected = true,
				ServiceClientId = clientId
			};
			return true;
		}

		// Routes a message from our MessagingHandler to a client
		// Same as RouteMessage, but when the sender happens to be the service itself
		private void RouteMessageFromHandlerToClient (Message message)
		{
			ApplicationInstance receiving = mAppInstances
				.Where(i => i.Id == message.DestinationId && i.Connection.IsClientConnected)
				.FirstOrDefault();
			if (receiving != null)
			{
				message.SenderId = ModuleId;
				message.SenderName = ModuleName;
				SendMessage(message, receiving.Connection.ServiceClientId);
			} else if (message.CorrelationId == 0)
			{
				// We are trying to send a request to a client that does not exist
				// Notify our own messaging handler by saying that this client does not exist
				Handler.Receive(GetNoSuchDestinationResponse(message.Id));
			}
		}

		// Serializes and sends an arbitrary message to a client
		private void SendMessage (Message message, uint clientId)
		{
			TcpMessage raw = new TcpMessage()
			{
				ClientId = clientId,
				Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message))
			};

			mServiceSender.Invoke(clientId, raw);
		}

		private Message GetNoSuchDestinationResponse (uint messageId)
		{
			return new Message()
			{
				CorrelationId = messageId,
				Success = false,
				Details = "No such destination"
			};
		}

		public void OnApplicationInstanceCreated(ApplicationInstance instance) => mAppInstances.Add(instance);
		public void OnApplicationInstanceDestroyed(ApplicationInstance instance) => mAppInstances.Remove(instance);
	}
}