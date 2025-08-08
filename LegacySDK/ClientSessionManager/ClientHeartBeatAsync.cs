using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	internal static partial class ClientSessionManager
	{
		static async internal Task<bool> HeartBeatAsync()
		{
			bool result = true;
			try
			{
				APIOperationResponseCode responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.HeartBeat,
																	HeartBeatOperationRequest( clientSocketConnection, APIOperation.HeartBeat ) );
				if (responseResult != APIOperationResponseCode.OK)
				{
					// %TODO - handle fact that heartbeat message was not replied to....
					Debug.Print( "[UNX] CLIENT:  ClientSessionManager.HeartBeatAsync() ***** HeartBeat message not replied to!" );
				}
			}
			catch (Exception ex)
			{
				Debug.Print( "[ERR] CLIENT:  ClientSessionManager.HeartBeatAsync() Error: {0}", ex );
				//throw;
			}
			return result;
		}


		static private int HeartBeatOperationRequest( ClientSocketConnection clientConnection, APIOperation op )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce )]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[(2 * sizeof( int ))];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nounce
			mainpayloadOffset += sizeof( int );
			encryptedPayLoad = EncryptionDecryption.Encrypt( encryptedPayLoad, ref ClientSessionManager.sessionSymKey, ref ClientSessionManager.sessionSymIV, false );
			#endregion

			// API operation here...
			Buffer.BlockCopy( BitConverter.GetBytes( (int) op ), 0, request, bytesWritten, sizeof( int ) );													// operation
			bytesWritten += sizeof( int );
			// Total PayLoadSize  (does NOT include the API Operation)
			Buffer.BlockCopy( BitConverter.GetBytes( encryptedPayLoad.Length + PAYLOAD_HEADER_SIZE ), 0, request, bytesWritten, sizeof( int ) );			// payload size
			bytesWritten += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ClientSessionManager.sessionId ), 0, request, bytesWritten, sizeof( int ) );							// sessionID
			bytesWritten += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, request, bytesWritten, sizeof( int ) );													// guard
			bytesWritten += sizeof( int );
			// payload here...
			Buffer.BlockCopy( encryptedPayLoad, 0, request, bytesWritten, encryptedPayLoad.Length );														// payload
			bytesWritten += encryptedPayLoad.Length;
			return bytesWritten;
		}
	}
}
