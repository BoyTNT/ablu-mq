using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AbluMQ.ClientLib
{
	public enum MessageType : byte
	{
		Error = 0,
		ClientLogin = 1,
		BrokerLogin = 2,
		Notify = 10,
		Request = 11,
		Reply = 12
	}
}
