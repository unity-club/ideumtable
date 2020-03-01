using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace CoffeeTable.Messaging.Core
{
	/// <summary>
	/// Represents a discrete message that can be sent between applications running on the Ideum Coffee Table.
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public sealed class Message
	{
		/// <summary>
		/// A meta-data class containing information about the application that originated a <see cref="Message"/> object.
		/// </summary>
		[JsonObject(MemberSerialization.Fields)]
		public sealed class SenderInfo
		{
			/// <summary>
			/// The name of the application that this message, as defined in its application manifest file.
			/// </summary>
			public readonly string Name;
			/// <summary>
			/// The unique ID used to identify the application that sent this message.
			/// </summary>
			public readonly uint Id;

			/// <summary>
			/// Constructs a <see cref="SenderInfo"/> object with the appropriate information.
			/// </summary>
			/// <param name="senderName">The name of the application sending the message</param>
			/// <param name="senderId">The unique ID of the application sending the message</param>
			public SenderInfo (string senderName, uint senderId)
			{
				Name = senderName;
				Id = senderId;
			}
		}

		/// <summary>
		/// The unique ID of the application where this <see cref="Message"/> object should be sent.
		/// </summary>
		public readonly uint DestinationId;
		/// <summary>
		/// The name of the command or operation that should be executed by the application that receives this message.
		/// </summary>
		/// <remarks>
		/// The name of the desired command or operation depends on the context in which the message is being sent, and must be agreed upon beforehand by the applications who will communicate with eachother.
		/// </remarks>
		public readonly string CommandName;
		/// <summary>
		/// The data that is to be sent with the command or operation specified by <see cref="CommandName"/>. Can be null or empty for a command that does not require any data.
		/// </summary>
		public readonly string CommandData;
		/// <summary>
		/// Meta-data about the application that originated this <see cref="Message"/> instance.
		/// </summary>
		public SenderInfo Sender { get; private set; }

		/// <summary>
		/// Constructs a <see cref="Message"/> object with the appropriate information.
		/// </summary>
		/// <param name="destinationId">The unique ID of the application to whom this message should be sent.</param>
		/// <param name="commandName">The name of the command or operation that should be executed by the application whose ID is <see cref="DestinationId"/></param>
		/// <param name="commandData">Optional data that is to be sent with the command or operation specified by <see cref="CommandName"/></param>
		public Message (uint destinationId, string commandName, string commandData = null)
		{
			DestinationId = destinationId;
			CommandName = commandName;
			CommandData = commandData;
		}
	}

	/// <summary>
	/// Represents a callback that is executed when a <see cref="Message"/> is received with data of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The data type of the object that is contained in a message's <see cref="Message.CommandData"/> field.</typeparam>
	/// <param name="senderInfo">Parameter containing information about the application that sent this message</param>
	/// <param name="callbackData">Parameter containing data of type <typeparamref name="T"/>. Can be null.</param>
	public delegate void Callback<T>(Message.SenderInfo senderInfo, T callbackData);
	/// <summary>
	/// Represents a callback that is executed when a <see cref="Message"/> is received without any data.
	/// </summary>
	/// <param name="senderInfo">Parameter containing information about the application that sent this message.</param>
	public delegate void Callback(Message.SenderInfo senderInfo);

	/// <summary>
	/// Defines a contract for an object that maps names of commands and operations (as declared in <see cref="Message.CommandName"/>)
	/// to delegate invocations that should be executed when a <see cref="Message"/> is received.
	/// </summary>
	public interface IMessageCallbackHandler
	{
		/// <summary>
		/// Registers a callback containing data of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">The type of data that should be passed to this callback when a <see cref="Message"/> is received. The contents of <see cref="Message.CommandData"/> will be deserialized into <typeparamref name="T"/>.</typeparam>
		/// <param name="commandName">When a received <see cref="Message"/> object whose <see cref="Message.CommandName"/> field equals this parameter (case-insensitive), <paramref name="commandCallback"/> will be invoked.</param>
		/// <param name="commandCallback">The callback that should be invoked when a received message's <see cref="Message.CommandName"/> equals <paramref name="commandName"/> (case-insensitive).</param>
		void AddCallback<T>(string commandName, Callback<T> commandCallback);
		/// <summary>
		/// Registers a callback that contains no data.
		/// </summary>
		/// <param name="commandName">When a received <see cref="Message"/> object whose <see cref="Message.CommandName"/> field equals this parameter (case-insensitive), <paramref name="commandCallback"/> will be invoked.</param>
		/// <param name="commandCallback">The callback that should be invoked when a received message's <see cref="Message.CommandName"/> equals <paramref name="commandName"/> (case-insensitive).</param>
		void AddCallback(string commandName, Callback commandCallback);
	}

	/// <summary>
	/// A standard implementation of <see cref="IMessageCallbackHandler"/>
	/// </summary>
	public class MessageCallbackHandler : IMessageCallbackHandler
	{
		/// <summary>
		/// Stores information that is used in the mapping of command names to callbacks in <see cref="mCallbackMap"/>
		/// </summary>
		private class CallbackInfo
		{
			public bool HasData;
			public string CallbackId;
			public Delegate Callback;
			public Type DataType;
		}

		private const string REGEX = @"^[a-zA-Z0-9_]+$";

		private Dictionary<string, CallbackInfo> mCallbackMap;

		public MessageCallbackHandler() 
		{
			mCallbackMap = new Dictionary<string, CallbackInfo>();
		}

		/// <inheritdoc/>
		public void AddCallback<T> (string commandName, Callback<T> commandCallback)
		{
			AddCallback(commandName, commandCallback, typeof(T));
		}

		/// <inheritdoc/>
		public void AddCallback (string commandName, Callback commandCallback)
		{
			AddCallback(commandName, commandCallback, null);
		}

		private void AddCallback (string commandName, Delegate commandCallback, Type dataType = null)
		{
			if (string.IsNullOrWhiteSpace(commandName))
				throw new ArgumentException("CoffeeTable.Messaging: Cannot add a callback with a name that is null or consists only of whitespace characters.");
			if (!Regex.IsMatch(commandName, REGEX))
				throw new ArgumentException("CoffeeTable.Messaging: Callback names must consist only of alphanumeric characters and underscores.");

			CallbackInfo info = new CallbackInfo
			{
				HasData = dataType != null,
				CallbackId = commandName.ToLower(),
				Callback = commandCallback,
				DataType = dataType
			};

			if (mCallbackMap.ContainsKey(info.CallbackId))
				throw new ArgumentException($"CofeeTable.Messaging: A callback with the ID: {info.CallbackId} has already been added.");

			mCallbackMap[info.CallbackId] = info;
		}

		/// <summary>
		/// Receive a <see cref="Message"/> instance and invoke any relevant callbacks.
		/// </summary>
		/// <param name="message">The message to be received.</param>
		public void ReceiveMessage (Message message)
		{
			if (string.IsNullOrWhiteSpace(message.CommandName)) return;
			if (mCallbackMap.TryGetValue(message.CommandName.ToLower(), out CallbackInfo ci))
			{
				if (ci.HasData)
				{
					try
					{
						object data = JsonConvert.DeserializeObject(message.CommandData, ci.DataType);
						ci.Callback.DynamicInvoke(message.Sender, data);
					}
					catch (JsonException) {}
				} else ci.Callback.DynamicInvoke(message.Sender);
			}
		}

		/// <summary>
		/// Constructs a <see cref="Message"/> object with the relevant information.
		/// </summary>
		/// <param name="destinationId">The unique ID of the application to whom this message should be sent.</param>
		/// <param name="commandName">The name of the command or operation that should be executed by the application with ID <paramref name="destinationId"/> when the message is received.</param>
		/// <param name="commandData"><para>An object containing data that should be passed with the command or operation identified by <paramref name="commandName"/>.</para>
		/// This will be serialized into <see cref="Message.CommandData"/>. Can be null or empty.</param>
		/// <returns>A message object containing the relevant information.</returns>
		public Message GetMessage(uint destinationId, string commandName, object commandData = null)
		{
			return new Message(destinationId,
				commandName,
				JsonConvert.SerializeObject(commandData));
		}
	}
}
