using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosys.Common.Types;

namespace Unosys.SDK
{
	internal static partial class ClientSetManager
	{
		#region SetManager Implementation
		static public async Task<FieldValue> PutTableFieldAsync( long handle, FieldDef fieldDef, object fldvalue )
		{
			byte[] fieldBytes = null;
			APIOperationResponseCode responseResult = APIOperationResponseCode.OK; // assume successful outcome
			FieldValue fieldValue = new FieldValue( null, responseResult );
			try
			{
				// Call into Unosys's DatabaseSystem to read a field value from the current record of the table identified by handle
				//
				// Step #1:  Lookup clientConnection from handle
				ClientSocketConnection clientSocketConnection = null;
				if (handles.ContainsKey( handle ))
				{
					clientSocketConnection = handles[handle];
				}
				else
				{
					throw new ArgumentException( "Unknown handle" );
				}
				fieldBytes = fieldDef.GetBytesFromObject( fldvalue );
				int bytesWritten = CreatePutTableFieldByNameOperationRequest( clientSocketConnection, APIOperation.PutTableFieldByName, handle, fieldDef.Name, fieldBytes );

				#region Call Unosys.Processor on socket to establish a conneciton for this Set - if succssful call will return a valid handle to the Set
				responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.PutTableFieldByName, bytesWritten );
				if (responseResult == APIOperationResponseCode.OK)
				{
					// NOTE:  PutTableFieldAsync() behaves just like GetTableFieldAsync() in that we want to update the client-sided cached field value
					//		  with that which was returned ensuring that the client-side cache is exactly equal to the server-side actual value
					ParseGetFieldBytesOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
							clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref fieldBytes );
					// Use the FieldDef to deserialize the fieldBytes
					fieldValue.Value = fieldDef.GetObjectFromBytes( fieldBytes );
					//Debug.Print( "[INF] CLIENT:  ClientSetManager.PutTableFieldAsync() = {0}, fieldValue={1}", responseResult, fieldValue );
				}
				else
				{
					fieldValue.ResponseCode = responseResult;  
					Debug.Print( "[INF] CLIENT:  ClientSetManager.PutTableFieldAsync() failed with responseResult={0}", responseResult );
				}
				#endregion
			}
			catch (Exception e)
			{
				// Unknown problem...assume invalid argument caused it
				fieldValue.ResponseCode = APIOperationResponseCode.INVALID_ARGUMENT;
				Debug.Print( "[ERR] CLIENT:  ClientSetManager.PutTableFieldAsync() threw exception: {0}", e );
			}
			finally
			{
				if (responseResult == APIOperationResponseCode.OK && handle > 0)
				{
					handlesLock.Release();
				}
			}
			return fieldValue;
		}
		#endregion

		#region Private Helpers
		static private int CreatePutTableFieldByNameOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long handle, string fieldName, byte[] fieldBytes )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce + apiTokenLength + apiToken + userTokenLength + userToken + handle + fieldNameLength + fieldName + fieldBytesLength + fieldBytes )]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//Debug.Print( "CLIENT: CreateSetOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[(6 * sizeof( int )) + sizeof(long) + ClientSessionManager.apiToken.Length + 
						ClientSessionManager.userToken.Length + (2 * fieldName.Length) + (fieldBytes == null ? 0 : fieldBytes.Length)]; // NOTE:  2 * setName.Length because of Unicode encoding below
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nounce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ClientSessionManager.apiToken.Length ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );		// apiTokenLength
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( ClientSessionManager.apiToken, 0, encryptedPayLoad, mainpayloadOffset, ClientSessionManager.apiToken.Length );				// apiToken
			mainpayloadOffset += ClientSessionManager.apiToken.Length;
			Buffer.BlockCopy( BitConverter.GetBytes( ClientSessionManager.userToken.Length ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );		// userTokenLength
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( ClientSessionManager.userToken, 0, encryptedPayLoad, mainpayloadOffset, ClientSessionManager.userToken.Length );				// userToken
			mainpayloadOffset += ClientSessionManager.userToken.Length;
			Buffer.BlockCopy( BitConverter.GetBytes( handle ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );									// handle
			mainpayloadOffset += sizeof( long );
			byte[] fieldNameUnicodeBytes = Encoding.Unicode.GetBytes( fieldName );
			Buffer.BlockCopy( BitConverter.GetBytes( fieldNameUnicodeBytes.Length ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );				// fieldNameLength
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( fieldNameUnicodeBytes, 0, encryptedPayLoad, mainpayloadOffset, fieldNameUnicodeBytes.Length );								// fieldName
			mainpayloadOffset += fieldNameUnicodeBytes.Length;
			if( fieldBytes == null )
			{
				Buffer.BlockCopy( BitConverter.GetBytes( 0 ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// zero - fieldBytesLength
				mainpayloadOffset += sizeof( int );
			}
			else
			{
				Buffer.BlockCopy( BitConverter.GetBytes( fieldBytes.Length ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );						// fieldBytesLength
				mainpayloadOffset += sizeof( int );
				Buffer.BlockCopy( fieldBytes, 0, encryptedPayLoad, mainpayloadOffset, fieldBytes.Length );													// fieldBytes
				mainpayloadOffset += fieldBytes.Length;
			}
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
			//Debug.Print( "CLIENT: CreateSetOperationRequest - total size (including operator+payloadsize)={0}, payloadOffset={1}", bytesWritten, 8 );
			return bytesWritten;
		}


		//static private void ParseGetFieldBytesOperationResponsePayload( int offset, int payloadSize, byte[] response, ref byte[] fieldBytes )
		//{
		//	// Response: PAYLOAD[  ENCRYPT( fieldBytesLength + fieldBytes )]
		//	//Debug.Print( "[INF] CLIENT:  ClientSetManager.ParseGetValueOperationResponsePayload() {0}", payloadSize );
		//	byte[] encryptedbytes = new byte[payloadSize];
		//	Buffer.BlockCopy( response, offset, encryptedbytes, 0, payloadSize );
		//	byte[] decryptedPayLoad = EncryptionDecryption.Decrypt( encryptedbytes, ClientSessionManager.sessionSymKey, ClientSessionManager.sessionSymIV, false );
		//	int mainpayloadOffset = 0;

		//	int fieldBytesLength = BitConverter.ToInt32( decryptedPayLoad, mainpayloadOffset );					// fieldBytesLength

		//	mainpayloadOffset += sizeof( int );
		//	fieldBytes = new byte[fieldBytesLength];
		//	Buffer.BlockCopy( decryptedPayLoad, mainpayloadOffset, fieldBytes, 0, fieldBytesLength );			// fieldBytes
		//	mainpayloadOffset += fieldBytesLength;
		//}
		#endregion
	}


}


