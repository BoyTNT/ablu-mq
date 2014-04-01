using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using AbluMQ.ClientLib;

namespace AbluMQ.Broker
{
	/// <summary>
	/// Message Broker
	/// </summary>
	public class MessageBroker
	{
		private bool m_Running;
		private TcpListener m_Server;
		private TcpClient m_Client;
		private ConcurrentDictionary<string, Endpoint> m_Endpoints;
		private ConcurrentDictionary<string, Endpoint> m_Brokers;
		private ConcurrentQueue<Message> m_Messages;
		private ConcurrentDictionary<string, DateTime> m_PendingRequests;
		private Thread m_DispatchThread;
		private Thread m_CheckOverdueThread;
		private ManualResetEvent m_MessageArrived;

		public MessageBroker(IPAddress address, int port)
		{
			m_Running = false;
			m_Server = new TcpListener(address, port);
			m_Client = new TcpClient();
			m_Endpoints = new ConcurrentDictionary<string, Endpoint>();
			m_Brokers = new ConcurrentDictionary<string, Endpoint>();
			m_PendingRequests = new ConcurrentDictionary<string, DateTime>();
			m_Messages = new ConcurrentQueue<Message>();
			m_MessageArrived = new ManualResetEvent(false);
			m_DispatchThread = new Thread(this.Dispatch);
			m_CheckOverdueThread = new Thread(this.CheckOverdue);
		}

		/// <summary>
		/// Connect to another Broker to route messages
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		public void Connect(string address, int port)
		{

		}

		/// <summary>
		/// Start working
		/// </summary>
		public void Start()
		{
			try
			{
				m_Running = true;
				m_Server.Start();

				//Start working threads
				m_DispatchThread.Start();
				m_CheckOverdueThread.Start();

				//Begin accept clients (asynchronous)
				m_Server.BeginAcceptTcpClient(new AsyncCallback(this.AcceptTcpClientCallback), null);
			}
			catch { }
		}

		/// <summary>
		/// Stop working
		/// </summary>
		public void Stop()
		{
			try
			{
				m_Running = false;

				//Wait for threads exit
				m_DispatchThread.Join(5000);
				m_CheckOverdueThread.Join(5000);

				m_Server.Stop();
			}
			catch { }

		}

		private void AcceptTcpClientCallback(IAsyncResult result)
		{
			try
			{
				//Use thread to handle client login
				var client = m_Server.EndAcceptTcpClient(result);
				ThreadPool.QueueUserWorkItem(new WaitCallback(this.ClientLogin), client);

				//Begin accept client again
				m_Server.BeginAcceptTcpClient(new AsyncCallback(this.AcceptTcpClientCallback), null);
			}
			catch { }
		}

		private void ClientLogin(object obj)
		{
			try
			{
				var client = obj as TcpClient;
				client.NoDelay = true;
				var stream = client.GetStream();

				//Read a message
				var message = Message.Read(stream, 5000);

				switch(message.Type)
				{
					//Client login
					case MessageType.ClientLogin:
						break;

					//Another broker login
					case MessageType.BrokerLogin:
						break;

					//Unrecognized message, close socket
					default:
						client.Close();
						break;
				}
			}
			catch { }
		}

		/// <summary>
		/// Thread for dispatching messages
		/// </summary>
		private void Dispatch()
		{
			while(m_Running)
			{
				//Wait for message arrival
				m_MessageArrived.WaitOne(2000);

				//Check the queue and dispatch messages

				m_MessageArrived.Reset();
			}
		}

		/// <summary>
		/// Thread for auto reply overdue requests
		/// </summary>
		private void CheckOverdue()
		{
			while(m_Running)
			{
				Thread.Sleep(1000);
			}
		}

		private void OnEndpointReceiveMessage(string name, Message message)
		{
			try
			{
				//Put message in queue and notify dispatch thread to start work
			}
			catch { }
		}

		private void OnEndpointLoseConnection(string name, string innerName)
		{
			try
			{
				//Remove connection
			}
			catch { }
		}
	}
}
