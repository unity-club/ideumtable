using System;

namespace CoffeeTable.Comm
{
	internal class TcpMessage
	{

		public static int CLIENT_ID_BYTES = 4;
		public static int MODULE_ID_BYTES = 4;
		public static int MESSAGE_ID_BYTES = 4;

		public uint ClientId;
		public uint ModuleId;
		public uint MessageId;
		public byte[] Payload;

		public byte[] CreateMessage()
		{
			var clientId = BitConverter.GetBytes(ClientId);
			var moduleId = BitConverter.GetBytes(ModuleId);
			var messageId = BitConverter.GetBytes(MessageId);

			var msg = new byte[clientId.Length + moduleId.Length + messageId.Length + Payload.Length];
			int count = 0;

			Array.Copy(clientId, 0, msg, count, clientId.Length);
			count += clientId.Length;

			Array.Copy(moduleId, 0, msg, count, moduleId.Length);
			count += moduleId.Length;

			Array.Copy(messageId, 0, msg, count, messageId.Length);
			count += messageId.Length;

			Array.Copy(Payload, 0, msg, count, Payload.Length);
			return msg;
		}

		public bool DecodeMessage(byte[] raw)
		{
			byte[] clientIdBytes = new byte[CLIENT_ID_BYTES];
			byte[] moduleIdBytes = new byte[MODULE_ID_BYTES];
			byte[] messageIdBytes = new byte[MESSAGE_ID_BYTES];

			int count = 0;

			Array.Copy(raw, count, clientIdBytes, 0, CLIENT_ID_BYTES);
			count += CLIENT_ID_BYTES;

			Array.Copy(raw, count, moduleIdBytes, 0, MODULE_ID_BYTES);
			count += MODULE_ID_BYTES;

			Array.Copy(raw, count, messageIdBytes, 0, MESSAGE_ID_BYTES);
			count += MESSAGE_ID_BYTES;

			Payload = new byte[raw.Length - count];

			Array.Copy(raw, count, Payload, 0, Payload.Length);

			ClientId = BitConverter.ToUInt32(clientIdBytes, 0);
			ModuleId = BitConverter.ToUInt32(moduleIdBytes, 0);
			MessageId = BitConverter.ToUInt32(messageIdBytes, 0);

			return true;
		}
	}
}
