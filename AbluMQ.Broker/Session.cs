using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

using AbluMQ.ClientLib;

namespace AbluMQ.Broker
{
	public delegate void ReceiveMessageDelegate(string name, Message message);
	public delegate void LoseConnectionDelegate(string name, string innerName);

	public class Session
	{
		public string Name { get; private set; }
		public string InnerName { get; private set; }
		public SessionType Type { get; private set; }
		public string CurrentSessionId { get; set; }
		public event ReceiveMessageDelegate OnReceiveMessage;
		public event LoseConnectionDelegate OnLoseConnection;

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
					//Client offline, notify Broker
					if(OnLoseConnection != null)
						OnLoseConnection(this.Name, this.InnerName);
				}
			}
			catch
			{
				//Client offline, notify Broker
				if(OnLoseConnection != null)
					OnLoseConnection(this.Name, this.InnerName);
			}
		}
	}
}
