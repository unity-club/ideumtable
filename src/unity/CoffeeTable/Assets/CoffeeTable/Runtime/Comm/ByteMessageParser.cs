using CoffeeTable.Logging;
using System;
using System.Diagnostics;
using System.Text;

namespace CoffeeTable.Comm
{
	internal class ByteMessageParser
	{
		public static byte[] CreateMessage(byte[] payload)
		{
			var headerBytes = BitConverter.GetBytes(payload.Length);
			var msg = new byte[headerBytes.Length + payload.Length];
			Array.Copy(headerBytes, msg, headerBytes.Length);
			Array.Copy(payload, 0, msg, headerBytes.Length, payload.Length);
			return msg;
		}

		private int _headerCount = 0;
		private byte[] _header = new byte[4];
		private int _payloadCount;
		private byte[] _payload = null;

		private Action<byte[]> _payloadCallback;
		private Action<Exception> _exceptionCallback;

		public ByteMessageParser(Action<byte[]> payloadCallback, Action<Exception> exceptionCallback = null)
		{
			_payloadCallback = payloadCallback;
			_exceptionCallback = exceptionCallback;
		}

		public void Clear()
		{
			_headerCount = 0;
			_payloadCount = 0;
			_payload = null;
		}

		public bool IsClear()
		{
			return _headerCount == 0 && _payloadCount == 0;
		}

		public int BytesNeededForPayload()
		{
			if (_payload != null)
			{
				return _payload.Length - _payloadCount;
			}
			else if (_headerCount == 4)
			{
				return BitConverter.ToInt32(_header, 0);
			}
			else
			{
				return -1;
			}
		}

		public void Consume(byte[] raw)
		{
			try
			{
				while (raw.Length > 0)
				{
					var rawReadCount = 0;
					while (rawReadCount < raw.Length && _headerCount < _header.Length)
					{
						_header[_headerCount] = raw[rawReadCount];
						rawReadCount++;
						_headerCount++;
					}
					if (rawReadCount >= raw.Length) return;
					if (_headerCount != _header.Length) return;

					if (_payload == null)
					{
						var tlen = BitConverter.ToInt32(_header, 0);
						if (tlen <= 0)
						{
							_payloadCallback(null);
							Clear();
							return;
						}
						_payload = new byte[tlen];
						_payloadCount = 0;
					}

					var availablePayloadBytes = _payload.Length - _payloadCount;
					var availableRawBytes = raw.Length - rawReadCount;
					var min = Math.Min(availablePayloadBytes, availableRawBytes);

					Array.Copy(raw, rawReadCount, _payload, _payloadCount, min);

					if (min == availablePayloadBytes && min == availableRawBytes)
					{
						Notify();
						return; //PERFECT SCORE!
					}
					else if (min == availableRawBytes)
					{
						_payloadCount += min;
						return; //WE REQUIRE MORE BYTES
					}
					else if (min == availablePayloadBytes)
					{
						rawReadCount += availablePayloadBytes;
						Notify();

						var leftovers = new byte[raw.Length - rawReadCount]; //this should never be 0.
						Array.Copy(raw, rawReadCount, leftovers, 0, leftovers.Length);
						raw = leftovers;
						continue;
					}
					break;
				}
			}
			catch (OutOfMemoryException e)
			{
				Log.Error("Out of memory exception thrown on ByteMessageParser. Dumping all data.");
				Clear();
				_exceptionCallback?.Invoke(e);
			}
		}

		private void Notify()
		{
			var bytes = _payload;
			Clear();
			_payloadCallback(bytes);
		}
	}
}