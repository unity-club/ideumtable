using Newtonsoft.Json;
using System;

namespace CoffeeTable.Messaging.Core
{
	/// <summary>
	/// Represents a discrete message that can be sent between applications running on the Ideum Coffee Table.
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public sealed class Message
	{
		private static uint _messageId = 1;

		/// <summary>
		/// A meta-data class containing miscellaneous information about a <see cref="Message"/> object.
		/// </summary>
		[JsonObject(MemberSerialization.Fields)]
		public sealed class MessageInfo
		{
			/// <summary>
			/// The name of the application that this message, as in its application manifest file.
			/// </summary>
			public string SenderName { get; set; }
			/// <summary>
			/// The unique ID used to identify the application that sent this message.
			/// </summary>
			public uint SenderId { get; set; }
			/// <summary>
			/// The time when the sender application sent the message
			/// </summary>
			public DateTime SentTime { get; }
			/// <summary>
			/// The time when the consumer application received the message
			/// </summary>
			public DateTime ReceivedTime { get; set; }
			/// <summary>
			/// The number of milliseconds that passed between when the message was sent and when it was received.
			/// </summary>
			[JsonIgnore]
			public int Delay => (ReceivedTime - SentTime).Milliseconds;

			public MessageInfo ()
			{
				SentTime = DateTime.Now;
			}
		}

		public string Request { get; private set; }
		public string Data { get; private set; }
		public uint DestinationId { get; private set; }
		public uint Id { get; private set; }
		public uint CorrelationId { get; private set; }
		public MessageInfo Info { get; private set; }

		public Message (string request, uint destinationId, string data = null, uint correlationId = 0)
		{
			Request = request;
			Data = data;
			DestinationId = destinationId;
			Id = _messageId++;
			CorrelationId = correlationId;
			Info = new MessageInfo();
		}

		public Message (string request, uint destinationId, object data = null, uint correlationId = 0) 
			: this(request, destinationId, JsonConvert.SerializeObject(data), correlationId) {}
	}
}
