using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using AbluMQ.Broker;
using AbluMQ.ClientLib;
using System.Diagnostics;

namespace AbluMQ.Test
{
	class Program
	{
		static void Main(string[] args)
		{
			if(args.Length == 0)
			{
				Console.WriteLine("No arguments, program exits");
				return;
			}

			switch(args[0].ToUpper())
			{
				case "BROKER":
					RunBroker(args);
					break;

				case "ROUTE":
					RunRouteBroker(args);
					break;

				case "SENDER":
					RunSender(args);
					break;

				case "RECEIVER":
					RunReceiver(args);
					break;

				default:
					Console.WriteLine("Error");
					break;
			}
		}

		private static void RunBroker(string[] args)
		{
			//BROKER PORT

			int port = Convert.ToInt32(args[1]);

			var broker = new MessageBroker(IPAddress.Any, port);
			broker.Start();

			Console.WriteLine("Service started, Enter to exit");
			Console.ReadLine();

			broker.Stop();
		}

		private static void RunRouteBroker(string[] args)
		{
			//ROUTE PORT IP PORT
			int port = Convert.ToInt32(args[1]);
			string conn_ip = args[2];
			int conn_port = Convert.ToInt32(args[3]);

			var broker = new MessageBroker(IPAddress.Any, port);
			broker.Connect(conn_ip, conn_port);
			broker.Start();

			Console.WriteLine("Route Service started, Enter to exit");
			Console.ReadLine();

			broker.Stop();
		}

		private static void RunSender(string[] args)
		{
			//SENDER IP PORT TARGET

			string ip = args[1];
			int port = Convert.ToInt32(args[2]);
			string target = args[3];

			var sender = new Sender();
			sender.Connect(ip, port);

			var watch = new Stopwatch();
			watch.Start();

			for(int i = 0;i < 1000;++i)
				sender.Notify(target, Encoding.UTF8.GetBytes(i.ToString()));

			watch.Stop();
			Console.WriteLine("{0} ms", watch.ElapsedMilliseconds);

			sender.Close();
		}

		private static void RunReceiver(string[] args)
		{
			//RECEIVER IP PORT NAME

			string ip = args[1];
			int port = Convert.ToInt32(args[2]);
			string name = args[3];

			var receiver = new Receiver(name);
			receiver.OnLoseConnection += new ClientLib.LoseConnectionDelegate(OnLoseConnection);
			receiver.OnReceiveMessage += new ClientLib.ReceiveMessageDelegate(OnReceiveMessage);
			receiver.Connect(ip, port);

			Console.WriteLine("Waiting...");
			Console.ReadLine();

			receiver.Close();
		}

		private static void OnLoseConnection()
		{
			Console.WriteLine("Connection lost");
		}

		private static void OnReceiveMessage(MessageReceivedEventArgs e)
		{
			var message = e.Message;
			Console.WriteLine("{0} =={2}==> {1}: {3}", message.Source, message.Target, message.Type, Encoding.UTF8.GetString(message.Data));
		}
	}
}
