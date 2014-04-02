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
		private string m_Name;
		private TcpListener m_Server;
		private TcpClient m_Client;
		private NetworkStream m_ClientStream;
		private ConcurrentDictionary<string, Session> m_Endpoints;
		private ConcurrentDictionary<string, Session> m_Brokers;
		private ConcurrentQueue<Message> m_Messages;
		private ConcurrentDictionary<string, DateTime> m_PendingRequests;
		private Thread m_DispatchThread;
		private Thread m_CheckOverdueThread;
		private ManualResetEvent m_MessageArrived;

		public MessageBroker(IPAddress address, int port)
		{
			m_Running = false;
			m_Name = Guid.NewGuid().ToString("N");
			m_Server = new TcpListener(address, port);
			m_Client = new TcpClient();
			m_Endpoints = new ConcurrentDictionary<string, Session>();
			m_Brokers = new ConcurrentDictionary<string, Session>();
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
			try
			{
				//Connect to another Broker
				m_Client.Connect(address, port);
				m_ClientStream = m_Client.GetStream();

				//Login
				var message = new Message();
				message.Type = MessageType.BrokerLogin;
				message.Source = m_Name;
				message.Target = string.Empty;
				message.WriteTo(m_ClientStream);

				//Begin to read message from parent Broker
				var lengthBytes = new byte[4];
				m_ClientStream.BeginRead(lengthBytes, 0, lengthBytes.Length, new AsyncCallback(this.ReadCallback), lengthBytes);
			}
			catch { }
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
				//Handle client login in thread
				var client = m_Server.EndAcceptTcpClient(result);
				ThreadPool.QueueUserWorkItem(new WaitCallback(ClientLogin), client);

				//Begin accept client again
				m_Server.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), null);
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

				//Read login message (wait max 5 seconds)
				var message = Message.Read(stream, 5000);

				if(message != null)
				{

					switch(message.Type)
					{
						//Client login
						case MessageType.ClientLogin:
							var clientSession = new Session(message.Source, SessionType.Client, client);
							clientSession.OnReceiveMessage += OnEndpointReceiveMessage;
							clientSession.OnLoseConnection += OnEndpointLoseConnection;
							m_Endpoints[clientSession.Name] = clientSession;
							clientSession.Start();

							break;

						//Another broker login
						case MessageType.BrokerLogin:
							var brokerSession = new Session(message.Source, SessionType.Broker, client);
							brokerSession.OnReceiveMessage += OnEndpointReceiveMessage;
							brokerSession.OnLoseConnection += OnEndpointLoseConnection;
							m_Brokers[brokerSession.Name] = brokerSession;
							brokerSession.Start();
							break;

						//Unrecognized message, close socket
						default:
							client.Close();
							break;
					}

					Console.WriteLine("{0} connected, {1} endpoints in queue", message.Source, m_Endpoints.Count);
				}
				else
				{
					//No login message, close socket
					client.Close();
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
				while(m_Running && m_Messages.Count > 0)
				{
					try
					{
						//Dequeue a message
						Message message = null;
						m_Messages.TryDequeue(out message);

						if(message == null)
							continue;

						//Dispatch it
						switch(message.Type)
						{
							//Notification
							case MessageType.Notify:
								//If target exists, deliver it
								if(m_Endpoints.ContainsKey(message.Target))
								{
									m_Endpoints[message.Target].WriteMessage(message);
								}
								//Deliver it to other Brokers
								else
								{
									foreach(var broker in m_Brokers.Values)
									{
										broker.WriteMessage(message);
									}
								}
								break;

							//Otherwise, drop it
							default:
								break;
						}
					}
					catch { }
				}

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
				Console.WriteLine("{0} =={3}==> {1}: {2}", message.Source, message.Target, Encoding.UTF8.GetString(message.Data), message.Type);

				m_Messages.Enqueue(message);
				m_MessageArrived.Set();
			}
			catch { }
		}

		private void OnEndpointLoseConnection(string name, string innerName)
		{
			try
			{
				//Remove connection
				if(m_Endpoints.ContainsKey(name) && m_Endpoints[name].InnerName == innerName)
				{
					Session session = null;
					do
					{
						m_Endpoints.TryRemove(name, out session);
					}
					while(session == null);

					session.Close();
				}

				Console.WriteLine("{0} lost connection, {1} endpoints left", name, m_Endpoints.Count);
			}
			catch { }
		}

		private void ReadCallback(IAsyncResult result)
		{
			try
			{
				int totalRead = m_ClientStream.EndRead(result);
				var lengthBytes = result.AsyncState as byte[];

				if(totalRead > 0)
				{
					//Read the full message
					var message = Message.Read(m_ClientStream, lengthBytes, totalRead);

					//Put message in queue and notify dispatch thread to start work
					Console.WriteLine("{0} =={3}==> {1}: {2}", message.Source, message.Target, Encoding.UTF8.GetString(message.Data), message.Type);

					m_Messages.Enqueue(message);
					m_MessageArrived.Set();

					//Read again
					Array.Clear(lengthBytes, 0, lengthBytes.Length);
					m_ClientStream.BeginRead(lengthBytes, 0, lengthBytes.Length, new AsyncCallback(ReadCallback), lengthBytes);
				}
				else
				{
				}
			}
			catch{}
		}
	}
}
