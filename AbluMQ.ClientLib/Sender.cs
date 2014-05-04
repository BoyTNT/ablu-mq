using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AbluMQ.ClientLib
{
	public class Sender
	{
		private TcpClient m_Client;
		private NetworkStream m_Stream;

		public string Name { get; private set; }

		public Sender()
		{
			this.Name = Guid.NewGuid().ToString("N");
			m_Client = new TcpClient();
			m_Client.NoDelay = true;
		}

		/// <summary>
		/// Connect to Broker
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		public void Connect(string host, int port)
		{
			try
			{
				m_Client.Connect(host, port);
				m_Stream = m_Client.GetStream();

				var message = new Message();
				message.Type = MessageType.ClientLogin;
				message.Source = this.Name;
				message.Target = string.Empty;
				message.Path = string.Empty;
				message.WriteTo(m_Stream);
			}
			catch { }
		}

		/// <summary>
		/// Disconnect from Broker
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
		/// Send a notification
		/// </summary>
		/// <param name="target"></param>
		/// <param name="data"></param>
		public void Notify(string target, byte[] data)
		{
			try
			{
				var message = new Message();
				message.Type = MessageType.Notify;
				message.Source = this.Name;
				message.Target = target;
				message.Data = data;
				message.WriteTo(m_Stream);
			}
			catch { }
		}

		/// <summary>
		/// Send a request and wait for reply
		/// </summary>
		/// <param name="target"></param>
		/// <param name="data"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public byte[] Request(string target, byte[] data, short timeout = 30)
		{
			byte[] response = null;

			try
			{
				//Send request
				var message = new Message();
				message.Type = MessageType.Request;
				message.Source = this.Name;
				message.Target = target;
				message.Data = data;
				message.Timeout = timeout;
				message.WriteTo(m_Stream);

				//Wait for reply
				var reply = Message.Read(m_Stream);
				if(reply.Type == MessageType.Reply)
				{
					response = reply.Data;
				}
				else if(reply.Type == MessageType.Error)
				{
					string reason = Encoding.UTF8.GetString(reply.Data);
					throw new Exception(reason);
				}
			}
			catch { }

			return response;
		}
	}
}
