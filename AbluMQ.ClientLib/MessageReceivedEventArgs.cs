using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AbluMQ.ClientLib
{
	public class MessageReceivedEventArgs : EventArgs
	{
		private NetworkStream m_Stream;
		public Message Message { get; private set; }

		public MessageReceivedEventArgs(NetworkStream stream, Message message)
		{
			m_Stream = stream;
			Message = message;
		}

		/// <summary>
		/// Response for the request
		/// </summary>
		/// <param name="data"></param>
		public void Reply(byte[] data)
		{
			try
			{
				var reply = new Message();
				reply.Type = MessageType.Reply;
				reply.Source = this.Message.Target;
				reply.Target = this.Message.Source;
				reply.SessionId = this.Message.SessionId;
				reply.Data = data;

				reply.WriteTo(m_Stream);
			}
			catch { }
		}
	}
}
