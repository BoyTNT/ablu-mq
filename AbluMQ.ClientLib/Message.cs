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

		/// <summary>
		/// Read a messge from the strem(SYNC)
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public static Message Read(NetworkStream stream)
		{
			throw new NotImplementedException();
		}

		public static Message Read(NetworkStream stream, byte[] lengthBytes, int alreadyRead)
		{
			throw new NotImplementedException();
		}

		public static Message Read(NetworkStream stream, int timeout)
		{
			throw new NotImplementedException();
		}
	}
}
