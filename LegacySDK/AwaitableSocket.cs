using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	// https://blogs.msdn.microsoft.com/pfxteam/2011/12/15/awaiting-socket-operations/	
	public class SocketAwaitable : INotifyCompletion
    {
        private readonly static Action SENTINEL = () => { };

        internal bool m_wasCompleted;
        internal Action m_continuation;
        public SocketAsyncEventArgs m_eventArgs;

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null) throw new ArgumentNullException("eventArgs");
            m_eventArgs = eventArgs;
            eventArgs.Completed += delegate
            {
                var prev = m_continuation ?? Interlocked.CompareExchange(
                    ref m_continuation, SENTINEL, null);
                if (prev != null) prev();
            };
        }

        internal void Reset()
        {
            m_wasCompleted = false;
            m_continuation = null;
        }

        public SocketAwaitable GetAwaiter() { return this; }

        public bool IsCompleted { get { return m_wasCompleted; } }

        public void OnCompleted(Action continuation)
        {
            if (m_continuation == SENTINEL ||
                Interlocked.CompareExchange(
                    ref m_continuation, continuation, null) == SENTINEL)
            {
                Task.Run(continuation);
            }
        }

        public virtual object GetResult()
        {
			object result = null;
			if (m_eventArgs.SocketError == SocketError.Success)
			{
				switch (m_eventArgs.LastOperation)
				{
					case SocketAsyncOperation.Accept:
						result = m_eventArgs.AcceptSocket;
						break;

					case SocketAsyncOperation.Receive:
						//Console.WriteLine( "Received bytes...{0}",m_eventArgs. );
						result = m_eventArgs.BytesTransferred;
						break;

					case SocketAsyncOperation.Send:
						//Console.WriteLine( "Sent bytes...." );
						result = m_eventArgs.BytesTransferred;
						break;

					//case SocketAsyncOperation.Disconnect:
					//	Console.WriteLine( "Disconnected...." );
					//	result = -1; //m_eventArgs.BytesTransferred;
					//	break;

					//case SocketAsyncOperation.Disconnect:
					//	Console.WriteLine( "Disconnected...." );
					//	result = -1;
					//	break;

					default:
						Console.WriteLine( m_eventArgs.LastOperation );
						throw new Exception( "Unknown SocketAsyncOperation" );
				}
			}
			else
			{
				throw new SocketException( (int) m_eventArgs.SocketError );
			}
			return result;
        }

		public void Dispose()
		{
			if (m_eventArgs != null)
			{
				m_eventArgs.Dispose();
				m_eventArgs = null;
			}
		}
    }
}
