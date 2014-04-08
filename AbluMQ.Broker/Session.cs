using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

using AbluMQ.ClientLib;

namespace AbluMQ.Broker
{
	internal delegate void ReceiveMessageDelegate(string name, Message message);
	internal delegate void LoseConnectionDelegate(string name, string innerName);

	internal class Session
	{
		internal string Name { get; private set; }
		internal string InnerName { get; private set; }
		internal SessionType Type { get; private set; }
		internal string CurrentSessionId { get; set; }
		internal event ReceiveMessageDelegate OnReceiveMessage;
		internal event LoseConnectionDelegate OnLoseConnection;

		private TcpClient m_Client;
		private NetworkStream m_Stream;

		public Session(string name, SessionType type, TcpClient client)
		{
			this.Name = name;
			this.InnerName = Guid.NewGuid().ToString("N");		//Generate inner name
			this.Type = type;

			m_Client = client;
			m_Stream = client.GetStream();
		}

		/// <summary>
		/// Start receiving messages
		/// </summary>
		public void Start()
		{
			try
			{
				//Begin reading
				var lengthBytes = new byte[4];
				m_Stream.BeginRead(lengthBytes, 0, lengthBytes.Length, new AsyncCallback(ReadCallback), lengthBytes);
			}
			catch { }
		}

		/// <summary>
		/// Close the socket
		/// </summary>
		public void Close()
		{
			try
			{
				m_Client.Close();
			}
			catch { }
		}

		/// <summary>
		/// Serialize the message and write to stream
		/// </summary>
		/// <param name="message"></param>
		public void WriteMessage(Message message)
		{
			lock(m_Client)
			{
				message.WriteTo(m_Stream);
			}
		}

		private void ReadCallback(IAsyncResult result)
		{
			try
			{
				int totalRead = m_Stream.EndRead(result);
				var lengthBytes = result.AsyncState as byte[];

				if(totalRead > 0)
				{
					//Read the full message
					var message = Message.Read(m_Stream, lengthBytes, totalRead);

					//Pass to Broker
					if(OnReceiveMessage != null)
						OnReceiveMessage(this.Name, message);

					//Read again
					Array.Clear(lengthBytes, 0, lengthBytes.Length);
					m_Stream.BeginRead(lengthBytes, 0, lengthBytes.Length, new AsyncCallback(ReadCallback), lengthBytes);
				}
				else
				{
					//Socket closed by remote, notify Broker
					if(OnLoseConnection != null)
						OnLoseConnection(this.Name, this.InnerName);
				}
			}
			catch
			{
				//Socket broken or connect error, notify Broker
				if(OnLoseConnection != null)
					OnLoseConnection(this.Name, this.InnerName);
			}
		}
	}
}
