using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	public static class SocketExtensions
	{
		public static bool IsConnected( this Socket socket )
		{
			try
			{
				return !(socket.Poll( 1, SelectMode.SelectRead ) && socket.Available == 0);
			}
			catch (SocketException) { return false; }
		}

		public static SocketAwaitable ReceiveAsync( this Socket socket, SocketAwaitable awaitable )
		{
			awaitable.Reset();
			if (!socket.ReceiveAsync( awaitable.m_eventArgs ))
				awaitable.m_wasCompleted = true;
			return awaitable;
		}

		public static SocketAwaitable SendAsync( this Socket socket, SocketAwaitable awaitable )
		{
			awaitable.Reset();
			if (!socket.SendAsync( awaitable.m_eventArgs ))
				awaitable.m_wasCompleted = true;
			return awaitable;
		}

		//public static SocketAwaitable SendAsync( this Socket socket, SocketAwaitable awaitable, AsyncManualResetEvent bytesSentEvent )
		//{
		//	awaitable.Reset();
		//	if (!socket.SendAsync( awaitable.m_eventArgs ))
		//		awaitable.m_wasCompleted = true;
		//	return awaitable;
		//}
	}

}
