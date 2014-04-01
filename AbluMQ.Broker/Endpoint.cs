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

	public class Endpoint
	{
		public Endpoint(string name, TcpClient client)
		{

		}

		/// <summary>
		/// Start receiving messages
		/// </summary>
		public void Start()
		{

		}

		/// <summary>
		/// Close the socket
		/// </summary>
		public void Close()
		{

		}

		/// <summary>
		/// Serialize the message and write to stream
		/// </summary>
		/// <param name="message"></param>
		public void WriteMessage(Message message)
		{

		}

		private void ReadCallback(IAsyncResult result)
		{

		}
	}
}
