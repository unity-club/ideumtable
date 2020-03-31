using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace CoffeeTable.Common.Messaging.Core
{
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public sealed class Message
	{
		private static uint _id = 1;

		#region Core
		public uint DestinationId { get; set; }
		public string Data { get; set; }
		public uint Id { get; private set; }
		#endregion

		#region Request
		public string Request { get; set; }
		#endregion Request

		#region Response
		public uint CorrelationId { get; set; }
		public bool Success { get; set; }
		public string Details { get; set; }
		#endregion

		#region Meta
		public DateTime Sent { get; private set; }
		public DateTime? Received { get; set; }
		public string SenderName { get; set; }
		public uint SenderId { get; set; }
		#endregion

		public Message()
		{
			Sent = DateTime.Now;
			Id = _id++;
		}

		[JsonConstructor]
		private Message (uint id)
		{
			Id = id;
		}
	}
}
