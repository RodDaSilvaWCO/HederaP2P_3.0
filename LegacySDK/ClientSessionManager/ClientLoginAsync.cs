using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	internal static partial class ClientSessionManager
	{
		static internal bool UserLogin( string username, string password )
		{
			bool result = false;
			try
			{
				// %TODO% - Replace with IdentityServer call to obtain Token for this "applications" right to access the UnoSys API
				APIOperationResponseCode responseResult = clientSocketConnection.ProcessRequestResponseAsync( APIOperation.UserLogin,
																	LoginOperationRequest( clientSocketConnection, APIOperation.UserLogin,
																		username, password ) ).Result;
				if (responseResult == APIOperationResponseCode.OK)
				{
					ParseLoginOperationResponsePayLoad( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
										clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref userToken );
					result = true;
				}
			}
			catch (Exception)
			{

				throw;
			}
			return result;
		}


		static private int LoginOperationRequest( ClientSocketConnection clientConnection, APIOperation op, string username, string password)
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + nonce + usernameLength + userName + passwordLength + password )]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//Debug.Print( "CLIENT: CreateWriteStOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			int usernameLength = Encoding.Unicode.GetByteCount( username );
			int passwordLength = Encoding.Unicode.GetByteCount( password );
			byte[] encryptedPayLoad = new byte[4 * sizeof( int ) + usernameLength + passwordLength];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nonce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( usernameLength ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );								// username length
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( Encoding.Unicode.GetBytes( username ), 0, encryptedPayLoad, mainpayloadOffset, usernameLength );								// username
			mainpayloadOffset += usernameLength;
			Buffer.BlockCopy( BitConverter.GetBytes( passwordLength ), 0, encryptedPayLoad, mainpayloadOffset, sizeof(int) );								// password length
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( Encoding.Unicode.GetBytes( password ), 0, encryptedPayLoad, mainpayloadOffset, passwordLength );								// password
			mainpayloadOffset += passwordLength;
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

		static private void ParseLoginOperationResponsePayLoad( int offset, int payloadSize, byte[] response, ref byte[] userToken )
		{
			// Response: PAYLOAD[  ENCRYPT( userId )]
			//Debug.Print( "[INF] CLIENT:  ClientSetManager.ParseHandleOperationResponsePayLoad() {0}", payloadSize );
			byte[] encryptedbytes = new byte[payloadSize];
			Buffer.BlockCopy( response, offset, encryptedbytes, 0, payloadSize );
			userToken = EncryptionDecryption.Decrypt( encryptedbytes, ClientSessionManager.sessionSymKey, ClientSessionManager.sessionSymIV, false );
		}
	}
}
