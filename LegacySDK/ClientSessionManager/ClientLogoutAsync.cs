using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	internal static partial class ClientSessionManager
	{
		static internal bool UserLogout( )
		{
			bool result = false;
			try
			{
				APIOperationResponseCode responseResult = clientSocketConnection.ProcessRequestResponseAsync( APIOperation.UserLogout,
																	LogoutOperationRequest( clientSocketConnection, APIOperation.UserLogout,
																		BitConverter.ToInt32( userToken ,0 ) ) ).Result;
				if (responseResult == APIOperationResponseCode.OK)
				{
					userToken = null;  // clear current logged in user.. 
					result = true;
				}
			}
			catch (Exception)
			{

				throw;
			}
			return result;
		}


		//static private int LoginOperationRequest( byte[] request, string username, string password )
		static private int LogoutOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long userId)
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + nonce + userId)]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//Debug.Print( "CLIENT: CreateWriteStOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[2 * sizeof( int ) + sizeof(long)];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nonce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( userId ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );									// userId
			mainpayloadOffset += sizeof( long );
			encryptedPayLoad = EncryptionDecryption.Encrypt( encryptedPayLoad, ref ClientSessionManager.sessionSymKey, ref ClientSessionManager.sessionSymIV, false );
			#endregion

			// API operation here...
			Buffer.BlockCopy( BitConverter.GetBytes( (int) op ), 0, request, bytesWritten, sizeof( int ) );													// operation
			bytesWritten += sizeof( int );
			// Total PayLoadSize  (does NOT include the API Operation)
			//Debug.Print( "[INF] CLIENT:  SDK.ClientLoginAsync() payloadSize={0}", encryptedPayLoad.Length + PAYLOAD_HEADER_SIZE );
			Buffer.BlockCopy( BitConverter.GetBytes( encryptedPayLoad.Length + PAYLOAD_HEADER_SIZE ), 0, request, bytesWritten, sizeof( int ) );			// payload size
			bytesWritten += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ClientSessionManager.sessionId ), 0, request, bytesWritten, sizeof( uint ) );							// sessionID
			bytesWritten += sizeof( uint );
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, request, bytesWritten, sizeof( int ) );													// guard
			bytesWritten += sizeof( int );
			// payload here...
			Buffer.BlockCopy( encryptedPayLoad, 0, request, bytesWritten, encryptedPayLoad.Length );														// payload
			bytesWritten += encryptedPayLoad.Length;
			//Debug.Print( "CLIENT: CreateWriteSetOperationRequest - total size (including operator+payloadsize)={0}, payloadOffset={1}", bytesWritten, 8 );
			return bytesWritten;

		}
	}
}
