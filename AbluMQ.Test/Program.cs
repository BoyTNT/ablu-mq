using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using AbluMQ.Broker;
using AbluMQ.ClientLib;

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
					RunBroker();
					break;

				case "ROUTE":
					RunRouteBroker();
					break;

				case "SENDER":
					break;

				case "RECEIVER":
					break;

				default:
					Console.WriteLine("Error");
					break;
			}
		}

		private static void RunBroker()
		{
			var broker = new MessageBroker(IPAddress.Any, 8080);
			broker.Start();

			Console.WriteLine("Enter to exit");
			Console.ReadLine();

			broker.Stop();
		}

		private static void RunRouteBroker()
		{

		}

		private static void RunSender()
		{

		}

		private static void RunReceiver()
		{

		}
	}
}
