﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AbluMQ.ClientLib
{
	public delegate void ReceiveMessageDelegate(MessageReceivedEventArgs e);
	public delegate void LoseConnectionDelegate();

	public class Receiver
	{
		private TcpClient m_Client;
		private NetworkStream m_Stream;

		public string Name { get; private set; }
		public event ReceiveMessageDelegate OnReceiveMessage;
		public event LoseConnectionDelegate OnLoseConnection;

		public Receiver(string name)
		{
			this.Name = name;
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

				//Login
				var message = new Message();
				message.Type = MessageType.ClientLogin;
				message.Source = this.Name;
				message.Target = string.Empty;
				message.WriteTo(m_Stream);

				//Begin reading
				var lengthBytes = new byte[4];
				m_Stream.BeginRead(lengthBytes, 0, lengthBytes.Length, new AsyncCallback(ReadCallback), lengthBytes);
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

					//Pass to handler
					if(OnReceiveMessage != null)
					{
						var args = new MessageReceivedEventArgs(m_Stream, message);
						OnReceiveMessage(args);
					}

					//Read again
					Array.Clear(lengthBytes, 0, lengthBytes.Length);
					m_Stream.BeginRead(lengthBytes, 0, lengthBytes.Length, new AsyncCallback(ReadCallback), lengthBytes);
				}
				else
				{
					//Connection closed, notify handler
					if(OnLoseConnection != null)
						OnLoseConnection();
				}
			}
			catch
			{
				//Connection broken, notify handler
				if(OnLoseConnection != null)
					OnLoseConnection();
			}
		}
	}
}
