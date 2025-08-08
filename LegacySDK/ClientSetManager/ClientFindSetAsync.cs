using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	internal static partial class ClientSetManager
	{
		#region SetManager Implementation
		static public async Task<Tuple<long, string, SetPropertyDTO>> FindSetAsync( string setMask )
		{
			SetPropertyDTO foundData = new SetPropertyDTO();
			string foundSetPath = string.Empty;
			long handle = -1;
			APIOperationResponseCode responseResult = APIOperationResponseCode.FALSE;
			try
			{
				// Call into Unosys's SetSystem to find a Set enumeration and return a handle to it
				// NOTE:  Each Set enumeration that is created has its own dedicated socket/handle pairing....
				//
				// Step #1 create the socket for the Set enumeration being requested
				ClientSocketConnection clientSocketConnection = new ClientSocketConnection( 5001, 8 * 1024 );  // %TODO:  Get these constants from Registry

				// Step #2  create the UnoSys.Process request
				int bytesWritten = FindSetOperationRequest( clientSocketConnection, APIOperation.FindSet, handle, setMask );

				#region Call Unosys.Processor on socket to establish a connection for this Set - if successful call will return a valid handle to the Set
				responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.FindSet, bytesWritten );
				if (responseResult == APIOperationResponseCode.OK)
				{
					ParseFindSetOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
							clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref handle, out foundSetPath, out foundData );
					if (handle > 0)
					{
						// We complete the FindSet() operation by adding the handle/clientSocketConnection key/value pair to the handles dictionary
						await handlesLock.WaitAsync();
						handles.Add( handle, clientSocketConnection );
					}
					Debug.Print( "CLIENT: SDK.ClientSetManager.FindSetAsync = {0}, handle={1}, path={2}, data={3}", responseResult, handle, foundSetPath, foundData );
				}
				else
				{
					Debug.Print( "CLIENT: SDK.ClientSetManager.FindSetAsync() failed with responseResult={0}", responseResult );
				}
				#endregion
			}
			catch (Exception)
			{
				responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
			}
			finally
			{
				if (responseResult == APIOperationResponseCode.OK && handle > 0)
				{
					handlesLock.Release();
				}
			}

			return new Tuple<long, string, SetPropertyDTO>( handle, foundSetPath, foundData );
		}

		static public async Task<Tuple<string, SetPropertyDTO>> FindNextSetAsync( long handle )
		{
			SetPropertyDTO foundData = new SetPropertyDTO();
			string foundSetPath = string.Empty;
			APIOperationResponseCode responseResult = APIOperationResponseCode.FALSE;
			try
			{
				// Call into Unosys's SetSystem to find a Set enumeration and return a handle to it
				// NOTE:  Each Set enumeration that is created has its own dedicated socket/handle pairing....
				//
				// Step #1 create the socket for the Set enumeration being requested
				ClientSocketConnection clientSocketConnection = new ClientSocketConnection( 5001, 8 * 1024 );  // %TODO:  Get these constants from Registry

				// Step #2  create the UnoSys.Process request
				int bytesWritten = FindSetOperationRequest( clientSocketConnection, APIOperation.FindSet, handle, string.Empty );

				#region Call Unosys.Processor on socket to establish a conneciton for this Set - if succssful call will return a valid handle to the Set
				responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.FindSet, bytesWritten );
				if (responseResult == APIOperationResponseCode.OK)
				{
					long responseHandle = handle;
					ParseFindSetOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
							clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref responseHandle, out foundSetPath, out foundData );
					Debug.Print( "SDK.FindNextSet = {0}, handle={1} -> {2}, path={3}, data={4}", responseResult, handle, responseHandle, foundSetPath, foundData );
					if (responseHandle < 1)
					{
						// FindNextSet() has completed, so we can close the handle
						await handlesLock.WaitAsync();  // acquire lock on handles dictionary 
						clientSocketConnection.Close();
						handles.Remove( handle );		// remove handle from dictionary
						handle = responseHandle;
					}
				}
				else
				{
					Debug.Print( "SDK.FindNextSetAsync() failed with responseResult={0}", responseResult );
				}
				#endregion
			}
			catch (Exception)
			{
				responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
				throw;
			}
			finally
			{
				if (responseResult == APIOperationResponseCode.OK && handle < 1)
				{
					handlesLock.Release();
				}
			}

			return new Tuple<string, SetPropertyDTO>( foundSetPath, foundData );
		}
		#endregion

		#region Helpers
		static private int FindSetOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long handle, string setMask )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce + handle + setMaskLength + setMask )]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			Debug.Print( "CLIENT: FindSetOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[(5 * sizeof( int )) + sizeof(long) + ClientSessionManager.apiToken.Length + ClientSessionManager.userToken.Length + (2 * setMask.Length)];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nounce
			mainpayloadOffset += sizeof( int );
			//Buffer.BlockCopy( BitConverter.GetBytes( ClientSessionManager.apiToken.Length ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );		// apiTokenLength
			//mainpayloadOffset += sizeof( int );
			//Buffer.BlockCopy( ClientSessionManager.apiToken, 0, encryptedPayLoad, mainpayloadOffset, ClientSessionManager.apiToken.Length );				// apiToken
			//mainpayloadOffset += ClientSessionManager.apiToken.Length;
			//Buffer.BlockCopy( BitConverter.GetBytes( ClientSessionManager.userToken.Length ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );	// userTokenLength
			//mainpayloadOffset += sizeof( int );
			//Buffer.BlockCopy( ClientSessionManager.userToken, 0, encryptedPayLoad, mainpayloadOffset, ClientSessionManager.userToken.Length );			// userToken
			//mainpayloadOffset += ClientSessionManager.userToken.Length;
			Buffer.BlockCopy( BitConverter.GetBytes( handle ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );									// handle
			mainpayloadOffset += sizeof( long );
			byte[] setMaskUnicodeBytes = Encoding.Unicode.GetBytes( setMask );
			Buffer.BlockCopy( BitConverter.GetBytes( setMaskUnicodeBytes.Length ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// setMaskLength
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( setMaskUnicodeBytes, 0, encryptedPayLoad, mainpayloadOffset, setMaskUnicodeBytes.Length );									// setMask
			mainpayloadOffset += setMaskUnicodeBytes.Length;
			encryptedPayLoad = EncryptionDecryption.Encrypt( encryptedPayLoad, ref ClientSessionManager.sessionSymKey, ref ClientSessionManager.sessionSymIV, false );
			#endregion

			// API operation here...
			Buffer.BlockCopy( BitConverter.GetBytes( (int) op ), 0, request, bytesWritten, sizeof( int ) );
			bytesWritten += sizeof( int );
			// Total PayLoadSize  (does NOT include the API Operation)
			Buffer.BlockCopy( BitConverter.GetBytes( encryptedPayLoad.Length + PAYLOAD_HEADER_SIZE ), 0, request, bytesWritten, sizeof( int ) );				// payload length
			bytesWritten += sizeof( int );
			// SessionID
			Buffer.BlockCopy( BitConverter.GetBytes( ClientSessionManager.sessionId ), 0, request, bytesWritten, sizeof( int ) );
			bytesWritten += sizeof( int );
			// Guard
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, request, bytesWritten, sizeof( int ) );
			bytesWritten += sizeof( int );
			// payload here...
			Buffer.BlockCopy( encryptedPayLoad, 0, request, bytesWritten, encryptedPayLoad.Length );															// payload
			bytesWritten += encryptedPayLoad.Length;
			Debug.Print( "CLIENT: FindSetOperationRequest - total size (including operator+payloadsize)={0}, payloadOffset={1}", bytesWritten, 8 );
			return bytesWritten;
		}

		static private void ParseFindSetOperationResponsePayload( int offset, int payloadSize, byte[] response, ref long handle, out string foundSetPath, out SetPropertyDTO foundData )
		{
			// Read handle
			handle = BitConverter.ToInt64( response, offset );
			offset += sizeof( long );
			// Read found set data
			var sizeOfData = BitConverter.ToInt32( response, offset );
			offset += sizeof( int );
			foundData = SetPropertyDTO.FromBytes( response, offset, sizeOfData );
			offset += sizeOfData;
			// Read found set path
			var foundPathLength = BitConverter.ToInt32( response, offset );
			offset += sizeof( int );
			foundSetPath = Encoding.Unicode.GetString( response, offset, foundPathLength );
			//Debug.Print( "ClientSetManager.ParseFindSetOperationResponsePayload() - handle={0}, foundSetPath={1}", handle, foundSetPath );
		}
		#endregion
	}
}
