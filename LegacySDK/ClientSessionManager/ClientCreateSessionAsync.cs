using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	internal static partial class ClientSessionManager
	{
		static async internal Task<bool> CreateSessionAsync()
		{
			bool result = false;
			try
			{
				#region Call Unosys.Processor to establish a Session for this client
				result = await Task.FromResult(true);

				// %LEGACY Unosys SDK %
				//APIOperationResponseCode responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.CreateSession,
				//											CreateSessionOperationRequest( clientSocketConnection.socketAwaitable.m_eventArgs.Buffer,
				//												Process.GetCurrentProcess().Id, AppDomain.CurrentDomain.Id ) );
				//if (responseResult == APIOperationResponseCode.OK)
				//{
				//	ParseCreateSessionOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
				//			clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref sessionId, ref sessionSymKey, ref sessionSymIV );
				//	result = true;
				//	//Debug.Print( "API.CreateSession = {0}, sessionId={1}, sessionSymKey={2}, sessionSymIV={3}", result, sessionId, sessionSymKey.Length, sessionSymIV.Length );
				//}
				#endregion
			}
			catch (SocketException)
			{
				throw new Exception( "Failed to connect to UnoSys.Processor service.  Ensure service is running." );
			}
			catch (Exception)
			{
				throw;
			}
			return result;
		}

		static private int CreateSessionOperationRequest( byte[] request, int processID, int appDomainID )
		{
			int offset = 0;

			// API operation here...
			Buffer.BlockCopy( BitConverter.GetBytes( (int) APIOperation.CreateSession ), 0, request, offset, sizeof( int ) );
			offset += sizeof( int );

			// size of payload here...
			Buffer.BlockCopy( BitConverter.GetBytes( 2 * sizeof( int ) ), 0, request, offset, sizeof( int ) );  // 8 bytes
			offset += sizeof( int );

			// payload starts here... processorID + appDomainID
			Buffer.BlockCopy( BitConverter.GetBytes( processID ), 0, request, offset, sizeof( int ) );		// process id
			offset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( appDomainID ), 0, request, offset, sizeof( int ) );  // appDomain id
			offset += sizeof( int );
			return offset;
		}

		static private void ParseCreateSessionOperationResponsePayload( int offset, int payloadSize, byte[] response, ref uint sessionId, ref byte[] symmetricKey, ref byte[] symmetricIV )
		{
			// Response Payload = sessionId + symmetricKey + symmetricIV
			//
			// NOTE:  This is the only call whose response is not encyrpted
			//		  %TODO% - should encrypt with something known only to this node AND available to the Unosys.SDK client DLL
			// read sessionId
			sessionId = BitConverter.ToUInt32( response, offset );
			offset += sizeof( uint );
			// read symmetricKey
			symmetricKey = new byte[32];
			Buffer.BlockCopy( response, offset, symmetricKey, 0, 32 );
			offset += symmetricKey.Length;
			// read symmetricIV
			symmetricIV = new byte[16];
			Buffer.BlockCopy( response, offset, symmetricIV, 0, 16 );
			//offset += symmetricIV.Length;
		}
	}
}
