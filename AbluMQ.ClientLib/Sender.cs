﻿using System;
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

		}

		/// <summary>
		/// Connect to Broker
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		public void Connect(string host, int port)
		{

		}

		/// <summary>
		/// Disconnect from Broker
		/// </summary>
		public void Close()
		{

		}

		/// <summary>
		/// Send a notification
		/// </summary>
		/// <param name="target"></param>
		/// <param name="data"></param>
		public void Notify(string target, byte[] data)
		{

		}

		/// <summary>
		/// Send a request and wait for reply
		/// </summary>
		/// <param name="target"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public byte[] Request(string target, byte[] data)
		{
			return Request(target, data, 30);
		}

		/// <summary>
		/// Send a request and wait for reply
		/// </summary>
		/// <param name="target"></param>
		/// <param name="data"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public byte[] Request(string target, byte[] data, int timeout)
		{
			throw new NotImplementedException();
		}
	}
}