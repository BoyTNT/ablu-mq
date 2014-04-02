using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AbluMQ.ClientLib
{
	public class Message
	{
		public MessageType Type { get; set; }
		public string Source { get; set; }
		public string Target { get; set; }
		public byte[] Data { get; set; }
		public int Timeout { get; set; }
		public string SessionId { get; set; }

		public Message()
		{
			this.Timeout = 30;
		}

		public Message(byte[] messageBytes)
		{
			var stream = new MemoryStream(messageBytes, false);

			//Message type x 1
			this.Type = (MessageType)stream.ReadByte();

			//Length of source x 1
			int fromLength = stream.ReadByte();

			//Source x N
			var sourceBytes = new byte[fromLength];
			stream.Read(sourceBytes, 0, sourceBytes.Length);
			this.Source = Encoding.UTF8.GetString(sourceBytes);

			//Length of target x 1
			int toLength = stream.ReadByte();

			//Target x N
			var targetBytes = new byte[toLength];
			stream.Read(targetBytes, 0, targetBytes.Length);
			this.Target = Encoding.UTF8.GetString(targetBytes);

			//SessionID x 32
			var sessionBytes = new byte[32];
			stream.Read(sessionBytes, 0, sessionBytes.Length);
			this.SessionId = Encoding.UTF8.GetString(sessionBytes);

			//Overtime x 4
			var timeoutBytes = new byte[4];
			stream.Read(timeoutBytes, 0, timeoutBytes.Length);
			this.Timeout = BitConverter.ToInt32(timeoutBytes, 0);

			//Length of Data x 4
			var dataLenBytes = new byte[4];
			stream.Read(dataLenBytes, 0, dataLenBytes.Length);
			int dataLen = BitConverter.ToInt32(dataLenBytes, 0);

			//Data x N
			this.Data = new byte[dataLen];
			stream.Read(this.Data, 0, this.Data.Length);

			stream.Close();
		}

		/// <summary>
		/// Serialize and write to network stream
		/// </summary>
		/// <param name="stream"></param>
		public void WriteTo(NetworkStream stream)
		{
			try
			{
				//Generate SessionID if necessary
				if(string.IsNullOrEmpty(this.SessionId))
					this.SessionId = Guid.NewGuid().ToString("n");

				//Compute length of the message
				int messageLength = 0;
				messageLength += 1;			//Type
				messageLength += 1;			//Length of source
				messageLength += Encoding.UTF8.GetByteCount(this.Source);		//Source
				messageLength += 1;			//Length of target
				messageLength += Encoding.UTF8.GetByteCount(this.Target);		//Target
				messageLength += 32;		//SessionID
				messageLength += 4;			//Overtime
				messageLength += 4;			//Length of data
				messageLength += this.Data == null ? 0 : this.Data.Length;


				//Length of message
				var lengthBytes = BitConverter.GetBytes(messageLength);
				stream.Write(lengthBytes, 0, lengthBytes.Length);

				//Type
				stream.WriteByte((byte)this.Type);

				//Source
				var fromBytes = Encoding.UTF8.GetBytes(this.Source);
				stream.WriteByte((byte)fromBytes.Length);
				stream.Write(fromBytes, 0, fromBytes.Length);

				//Target
				var toBytes = Encoding.UTF8.GetBytes(this.Target);
				stream.WriteByte((byte)toBytes.Length);
				stream.Write(toBytes, 0, toBytes.Length);

				//SessionID
				var sessionBytes = Encoding.UTF8.GetBytes(this.SessionId);
				stream.Write(sessionBytes, 0, sessionBytes.Length);

				//Overtime
				var timeoutBytes = BitConverter.GetBytes(this.Timeout);
				stream.Write(timeoutBytes, 0, timeoutBytes.Length);


				if(this.Data != null)
				{
					//Length of data
					var dataLenBytes = BitConverter.GetBytes(this.Data.Length);
					stream.Write(dataLenBytes, 0, dataLenBytes.Length);

					//Data
					stream.Write(this.Data, 0, this.Data.Length);
				}
				else
				{
					//Length of data
					var dataLenBytes = BitConverter.GetBytes(0);
					stream.Write(dataLenBytes, 0, dataLenBytes.Length);
				}
			}
			catch { }
		}


		/// <summary>
		/// Read a messge from the strem(SYNC)
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public static Message Read(NetworkStream stream)
		{
			var lengthBytes = new byte[4];
			return Read(stream, lengthBytes, 0);
		}

		public static Message Read(NetworkStream stream, byte[] lengthBytes, int alreadyRead)
		{
			Message message = null;

			try
			{
				//Read length
				while(alreadyRead < lengthBytes.Length)
				{
					alreadyRead += stream.Read(lengthBytes, alreadyRead, lengthBytes.Length - alreadyRead);
				}
				int messageLength = BitConverter.ToInt32(lengthBytes, 0);

				//Read full packet
				alreadyRead = 0;
				var messageBytes = new byte[messageLength];
				do
				{
					alreadyRead += stream.Read(messageBytes, alreadyRead, messageBytes.Length - alreadyRead);
				}
				while(alreadyRead < messageBytes.Length);

				//Deserialize the message
				message = new Message(messageBytes);
			}
			catch { }

			return message;
		}

		public static Message Read(NetworkStream stream, int timeout)
		{
			Message message = null;

			try
			{
				//Read length of the message
				var lengthBytes = new byte[4];
				var asyncResult = stream.BeginRead(lengthBytes, 0, lengthBytes.Length, null, null);

				//Wait for result or timeout
				asyncResult.AsyncWaitHandle.WaitOne(timeout);

				if(asyncResult.IsCompleted)
				{
					int alreadyRead = stream.EndRead(asyncResult);
					message = Read(stream, lengthBytes, alreadyRead);
				}
			}
			catch { }

			return message;
		}
	}
}
