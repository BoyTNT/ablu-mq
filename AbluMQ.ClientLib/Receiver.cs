using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AbluMQ.ClientLib
{
	public delegate void ReceiveMessageDelegate(MessageReceivedEventArgs e);

	public class Receiver
	{
		private TcpClient m_Client;
		private NetworkStream m_Stream;

		public string Name { get; private set; }
		public event ReceiveMessageDelegate OnReceiveMessage;

	}
}
