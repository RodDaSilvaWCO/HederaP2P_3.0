using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosys.Common.Types;

namespace Unosys.SDK
{
	internal static partial class ClientSetManager
	{
		#region SetManager Implementation
		//static public async Task<HandleWithFieldDefs> CreateTableSetAsync( string setName, SetSchema setSchema, uint setMode, uint desiredAccess, uint setAttributes, uint shareMode )
		//{
		//	long handle = -1;
		//	APIOperationResponseCode responseResult = APIOperationResponseCode.FALSE;
		//	HandleWithFieldDefs handleWithFieldDefs = new HandleWithFieldDefs( handle, 0, 0, false, null, responseResult );  // default
		//	FieldDef[] fields = null!;
		//	int fieldCount = 0;
		//	ulong recordCount = 0;
		//	ulong recordOrdinal = 0;
		//	bool isDeleted = false;
		//	try
		//	{
		//		// Call into Unosys's DatabaseSystem to create a Table Set and return a handle to it
		//		// NOTE:  Each Set that is opened has its own dedicated socket/handle pairing....
		//		//
		//		// Step #1 create the socket for the Set being requested
		//		ClientSocketConnection clientSocketConnection = new ClientSocketConnection( ClientSessionManager.basePort + 2 );  

		//		// Step #2  create the UnoSys.Process request
		//		int bytesWritten = CreateTableSetOperationRequest( clientSocketConnection, APIOperation.CreateSet, setName, setSchema, setMode, desiredAccess, setAttributes, shareMode );

		//		#region Call Unosys.Processor on socket to establish a conneciton for this Set - if succssful call will return a valid handle to the Set
		//		responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.CreateSet, bytesWritten );
		//		if (responseResult == APIOperationResponseCode.OK)
		//		{
		//			//ParseHandleOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
		//			//		clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref handle );
		//			ParseHandleAndSchemaOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
		//										clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref handle, ref recordCount, ref recordOrdinal, ref isDeleted, ref fieldCount, ref fields );
		//			if (handle > 0)
		//			{
		//				// We complete the Open Set operation by adding the handle/clientSocketConnection key/value pair to the handles dictionary
		//				await handlesLock.WaitAsync();
		//				handles.Add( handle, clientSocketConnection );
		//				handleWithFieldDefs = new HandleWithFieldDefs( handle, recordOrdinal, recordCount, isDeleted, setSchema, responseResult );  
		//			}
		//			else
		//			{
		//				// Bad handle 
		//				handleWithFieldDefs.responseCode = APIOperationResponseCode.INVALID_HANDLE;
		//			}
		//			//Debug.Print( "[INF] CLIENT:  ClientSetManager.CreateTableSetAsync = {0}, handle={1}", responseResult, handle );
		//		}
		//		else
		//		{
		//			// Bad responseResult 
		//			handleWithFieldDefs.responseCode = responseResult;
		//			Debug.Print( "[UNX] CLIENT:  ClientSetManager.CreateTableSetAsync() failed with responseResult={0}", responseResult );
		//		}
		//		#endregion
		//	}
		//	catch (Exception e)
		//	{
		//		// Unknown issue...assume invalide argument caused it
		//		handleWithFieldDefs.responseCode = APIOperationResponseCode.INVALID_ARGUMENT; ;
		//		Debug.Print( "[ERR] CLIENT:  ClientSetManager.CreateTableSetAsync() threw exception: {0}", e );
		//	}
		//	finally
		//	{
		//		if (responseResult == APIOperationResponseCode.OK && handle > 0)
		//		{
		//			handlesLock.Release();
		//		}
		//	}

		//	return handleWithFieldDefs;
		//}
		#endregion

		#region Private Helpers
		static private int CreateTableSetOperationRequest( ClientSocketConnection clientConnection, APIOperation op, string setName, SetSchema setSchema, uint setMode, uint desiredAccess, uint setAttributes, uint shareMode )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce + apiTokenLength + apiToken + userTokenLength + userToken + setNameLength + setName + setmode + desiredAccess, + setAttributes + shareMode + fieldCount + fieldDef... )]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			int fieldCount = setSchema.FieldCount;
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//byte[][] fieldDefBytes = new byte[setSchema.Fields.Length][];
			//// Compute the byte representation for each field of schema
			int totalFieldBytes = sizeof( int ) * fieldCount;
			//for (int i = 0; i < fieldCount; i++)
			//{
			//	fieldDefBytes[i] = TableSetFieldBytes( setSchema.Fields[i] );
			//	totalFieldBytes += fieldDefBytes[i].Length;
			//}
			byte[] fieldDefBytes = setSchema.SerializeAllFieldDefinitionsToBytes();
			//for (int i = 0; i < fieldCount; i++)
			//{
			//	totalFieldBytes += fieldDefBytes[i].Length;
			//}


			//Debug.Print( "CLIENT: CreateTableSetOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[(10 * sizeof( int )) + ClientSessionManager.apiToken.Length +
											ClientSessionManager.userToken.Length + (2 * setName.Length) + fieldDefBytes.Length];
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
			byte[] setNameUnicodeBytes = Encoding.Unicode.GetBytes( setName );
			Buffer.BlockCopy( BitConverter.GetBytes( setNameUnicodeBytes.Length ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// setNameLength
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( setNameUnicodeBytes, 0, encryptedPayLoad, mainpayloadOffset, setNameUnicodeBytes.Length );									// setName
			mainpayloadOffset += setNameUnicodeBytes.Length;
			Buffer.BlockCopy( BitConverter.GetBytes( setMode ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );									// setMode
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( desiredAccess ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );								// desiredAccess
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( setAttributes ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );								// setAttributes
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( shareMode ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );									// shareMode
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( fieldCount ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );									// field count
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( fieldDefBytes, 0, encryptedPayLoad, mainpayloadOffset, fieldDefBytes.Length );												// field...
			mainpayloadOffset += fieldDefBytes.Length;
			//// Add the bytes for each field of schema to payload
			//for (int i = 0; i < fieldCount; i++)
			//{
			//	int fieldLen = fieldDefBytes[i].Length;
			//	Buffer.BlockCopy( BitConverter.GetBytes( fieldLen ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );								// field i bytes length
			//	mainpayloadOffset += sizeof( int );
			//	Buffer.BlockCopy( fieldDefBytes[i], 0, encryptedPayLoad, mainpayloadOffset, fieldLen );														// field i bytes
			//	mainpayloadOffset += fieldLen;
			//}
			// Encrypted payload
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
			//Debug.Print( "CLIENT: CreateTableSetOperationRequest - total size (including operator+payloadsize)={0}, payloadOffset={1}", bytesWritten, 8 );
			return bytesWritten;
		}
		#endregion
	}
}


