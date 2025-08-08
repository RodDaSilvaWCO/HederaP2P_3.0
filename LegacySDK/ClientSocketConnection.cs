using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	internal class ClientSocketConnection
	{
		#region Constants
		const int MAX_BUFFER_SIZE = 16 * 1024;  
		#endregion

		#region Field Members
		private bool lDisposed;
		public Socket ClientSocket;
		public SocketAwaitable socketAwaitable = null;
		private AsyncSemaphore connectionLock = null;
		private uint maxbuffersize = 0;
		private uint maxdatablocksize = 0;
		public int responsePayloadSize = 0;
		public int responsePayLoadOffset = 0;
		internal int nounce = 0;
		#endregion


		#region Constructors, Destructors
		//public ClientSocketConnection() : this( 5000, IPAddress.Loopback, MAX_BUFFER_SIZE ) { }

		public ClientSocketConnection( int port, uint maxDataBlockSize = MAX_BUFFER_SIZE ) : this( port, IPAddress.Loopback, maxDataBlockSize ) { }

		public ClientSocketConnection( int port, IPAddress serverIPAddress, uint maxDataBlockSize )
			: base()
		{
			try
			{
				// Create the listening socket class
				ClientSocket = new Socket( AddressFamily.InterNetwork,
					SocketType.Stream,
					ProtocolType.Tcp );
				// Create the local end point to associate with the socket
				IPEndPoint serverEndPoint = new IPEndPoint( serverIPAddress, port );
				maxdatablocksize = maxDataBlockSize;
				maxbuffersize = maxDataBlockSize + (2 * 1024);  // NOTE:  We add 2K to the Buffer size (which is for data only) to allow for the protocol header
				this.ConnectToServer( serverEndPoint ).Wait();
				// Create a connection lock semaphore object
				connectionLock = new AsyncSemaphore( 1 );
			}
			catch (Exception e)
			{
				Debug.Print( "CLIENT: Error in ClientSocketConnection.ctor() {0}", e.Message );
				throw;
			}
		}




		// Destructor ensures Dispose is called if it hasn't been already
		~ClientSocketConnection()
		{
			if (!lDisposed)
				this.Dispose();

		}
		#endregion

		#region Public Methods
		internal uint MaxBlockSize
		{
			get { return maxdatablocksize; }
		}

		public async Task<APIOperationResponseCode> ProcessRequestResponseAsync( APIOperation operation, int requestLength )
		{
			byte[] buffer = socketAwaitable.m_eventArgs.Buffer;
			//Debug.Print( "Entering ProcessRequestAsync()...requestLength={0}", requestLength );
			APIOperationResponseCode result = APIOperationResponseCode.BAD_RESPONSE;
			if (socketAwaitable.m_eventArgs.SocketError == SocketError.Success)
			{
				// Lock to ensure only one request/response occuring at a time on this connection
				// in order to prevent "interleaving" of requests and their responses
				await connectionLock.WaitAsync();  
				try
				{
					// Copy data into socket buffer
					//Debug.Print( "==========C SetBuffer={0}", requestLength );
					socketAwaitable.m_eventArgs.SetBuffer( 0, requestLength );
					// Send bytes and wait until sent
					int bytesSent = (int) await ClientSocket.SendAsync( socketAwaitable );

					if (socketAwaitable.m_eventArgs.SocketError == SocketError.Success && bytesSent == requestLength)
					{
						// If all went well on the send, now wait for and then process the response...
						//
						// Read the result
						int offset = 0;
						int bytesReceived = await ReadBytes( 0, sizeof( int ) );
						if (socketAwaitable.m_eventArgs.SocketError == SocketError.Success && bytesReceived == sizeof(int))
						{
							int opResponse = BitConverter.ToInt32( buffer, offset );
							offset += sizeof( int );
							if (opResponse < (int) APIOperationResponseCode.FIRST || opResponse > (int) APIOperationResponseCode.LAST)
							{
								result = APIOperationResponseCode.BAD_RESPONSE;
								IgnoreRestOfResponse( offset );  // throw away rest of response
								//Debug.Print( "CLIENT:  ResponseCode invalid" );
							}
							else
							{
								result = (APIOperationResponseCode) opResponse;
								// Read the payloadsize
								bytesReceived = await ReadBytes( offset, sizeof( int ) );
								if (socketAwaitable.m_eventArgs.SocketError == SocketError.Success && bytesReceived == sizeof( int ))
								{
									responsePayloadSize = BitConverter.ToInt32( buffer, offset );
									offset += sizeof( int );
									if (responsePayloadSize < 0 || responsePayloadSize > buffer.Length - sizeof( int ))
									{
										result = APIOperationResponseCode.BAD_RESPONSE;
										IgnoreRestOfResponse( offset );  // throw away rest of response
									}
									else
									{
										// Read the payload
										if (responsePayloadSize > 0 )
										{
											bytesReceived = await ReadBytes( offset, responsePayloadSize );
											if (socketAwaitable.m_eventArgs.SocketError == SocketError.Success && bytesReceived == responsePayloadSize)
											{
												responsePayLoadOffset = offset;
											}
											else
											{
												Debug.Print( "[UNX] CLIENT: ClientSocketConnectionProcessRequestResponseAsync() Incomplete Payload BytesReceived = {0} (should be {1})", bytesReceived, sizeof( int ) );
												result = APIOperationResponseCode.BAD_RESPONSE;
												IgnoreRestOfResponse( offset );  // throw away rest of response
											}
										}
									}
								}
								else
								{
									Debug.Print( "[UNX] CLIENT: ClientSocketConnectionProcessRequestResponseAsync() Incomplete PayloadSize BytesReceived = {0} (should be {1})", bytesReceived, sizeof( int ) );
									throw new SocketException();
								}
							}
						}
						else
						{
							Debug.Print( "[UNX] CLIENT: ClientSocketConnectionProcessRequestResponseAsync() Incomplete result BytesReceived = {0} (should be {1})", bytesReceived, sizeof( int ) );
							throw new SocketException();
						}

//						Debug.Print( "Client Sent {0} bytes and Received {1} bytes on connection ID={2}", bytesSent, bytesReceived, ClientSocket.GetHashCode() );//, HostCryptology.ConvertBytesToHexString( args.Buffer ) );	
					}
					else
					{
						Debug.Print( "[UNX] CLIENT: ClientSocketConnectionProcessRequestResponseAsync() Incomplete BytesSent = {0} (should be {1})", bytesSent, requestLength );
						throw new SocketException();
					}
				}
				catch (SocketException)
				{
					// NOP - means client aborted the connection...simply return
					Debug.Print( "[ERR] CLIENT: ClientSocketConnectionProcessRequestResponseAsync() Socket Exception...." );
				}
				catch (Exception ex)
				{
					Debug.Print( "[ERR] CLIENT: ClientSocketConnectionProcessRequestResponseAsync() Unexpected error {0}", ex );
				}
				finally
				{
					connectionLock.Release();
				}
			}
			else
			{
				Debug.Print( "[UNX] CLIENT: ClientSocketConnectionProcessRequestResponseAsync() Socket not valid ignoring call...." );
				// NOP - means client aborted the connection...simply return
			}
			//Debug.Print( "...Leaving ProcessRequestAsync()" );
			return result;
		}

		public void Close()
		{
			Dispose();
		}

		public virtual void Dispose()
		{
			// Shutdown and Close the client socket
			if (ClientSocket != null)
			{
				ClientSocket.Shutdown( SocketShutdown.Both );
				ClientSocket.Close();
				ClientSocket = null;
			}

			if (socketAwaitable != null)
			{
				socketAwaitable.Dispose();
				socketAwaitable = null;
			}

			lDisposed = true;
		}
		#endregion


		#region Helpers
		private async Task ConnectToServer( IPEndPoint oServerEndPoint )
		{
			// Attempt to connect to server in a retry loop - in case server is busy
			int iNumRetries = 0;
			while (iNumRetries != 5)
			{
				try
				{
					ClientSocket.Connect( oServerEndPoint );
					break;
				}
				catch (SocketException e)
				{
					System.Diagnostics.Debug.WriteLine( e.Message );
					iNumRetries++;
				}
				//Thread.Sleep( 1 );
				await Task.Delay( 1 );
			}
			if (iNumRetries == 5)
			{
				ClientSocket = null;
				throw new Exception( "Client socket connection retry exceeded" );

			}

			var args = new SocketAsyncEventArgs();
			//Debug.Print( "==========A SetBuffer={0}", buffersize );
			args.SetBuffer( new byte[maxbuffersize], 0, (int)maxbuffersize );

			socketAwaitable = new SocketAwaitable( args );

		}

		private async Task<int> ReadBytes( int startOffset, int bytesToRead )
		{
			//Debug.Print( "CLIENT: ReadBytes() BEGIN... startOffset={0}, bytesToRead={1}", startOffset, bytesToRead );
			SocketAsyncEventArgs args = socketAwaitable.m_eventArgs;
			int totalBytesRead = 0;
			// Loop to receive bytesToRead bytes
			do
			{
				//Debug.Print( "==========B SetBuffer={0}", bytesToRead - totalBytesRead );
				args.SetBuffer( startOffset, bytesToRead - totalBytesRead );
				int bytesRead = (int) await ClientSocket.ReceiveAsync( socketAwaitable );
				if (bytesRead > 0)
				{
					totalBytesRead += bytesRead;
					//Debug.Print( "CLIENT: Received byte1: {0},{1},{2},{3}", bytesRead, args.Buffer[startOffset], args.Buffer[startOffset + 1], args.Buffer[startOffset + 2] );
					startOffset += bytesRead;
				}
				else
				{
					//Debug.Print( "CLIENT: bytesread={0}, SocketError={1}", bytesRead, args.SocketError );
					if (!ClientSocket.IsConnected())
					{
						throw new SocketException();
					}
				}
			} while (totalBytesRead < bytesToRead);
			if (totalBytesRead != bytesToRead)
			{
				//Debug.Print( "CLIENT: ReadBytes:  Read too many bytes = {0} (should be {1})", totalBytesRead, bytesToRead );
				throw new SocketException();

			}
			//else
			//{
			//	//Debug.Print( "CLIENT: Received byte2: {0},{1},{2},{3}", totalBytesRead, args.Buffer[startOffset], args.Buffer[startOffset + 1], args.Buffer[startOffset + 2] );
			//}
			//Debug.Print( "CLIENT: ReadBytes() END... startOffset={0}, bytesToRead={1}, totalBytesRead={2}", startOffset, bytesToRead, totalBytesRead );
			return totalBytesRead;
		}

		private async void IgnoreRestOfResponse( int offset)
		{
			// Read and ignore the remaining bytes
			int bytesRead = 1;
			//Debug.Print( "IgnoreRest offset={0}", offset );
			while (bytesRead > 0)
			{
				
				bytesRead = (int) await ReadBytes( offset++, 1 );
				//Debug.Print( "IgnoreRest bytesRead ={1}, offset={0}", offset, bytesRead );
			}
		}
		#endregion
	}
}
