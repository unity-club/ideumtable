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
			public string Name { get; private set; }
			/// <summary>
			/// The unique ID used to identify the application that sent this message.
			/// </summary>
			public uint Id { get; private set; }

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
		public uint DestinationId { get; private set; }
		/// <summary>
		/// The name of the command or operation that should be executed by the application that receives this message.
		/// </summary>
		/// <remarks>
		/// The name of the desired command or operation depends on the context in which the message is being sent, and must be agreed upon beforehand by the applications who will communicate with eachother.
		/// </remarks>
		public string CommandName { get; private set; }
		/// <summary>
		/// The data that is to be sent with the command or operation specified by <see cref="CommandName"/>. Can be null or empty for a command that does not require any data.
		/// </summary>
		public string CommandData { get; private set; }
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
}
