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
		#region static Field Members
		static private SortedDictionary<long, ClientSocketConnection> handles = null;
		static private AsyncSemaphore handlesLock = null;
		static private Random guardGenerator = null;
		#endregion

		#region Constructor
		static ClientSetManager()
		{
			// %LEGACY Unosys SDK% 
			//handles = new SortedDictionary<long, ClientSocketConnection>();
			//// Populate the special "bootstrap" connection to be used for opening handles 
			//// necessary for obtain the blocksize of the handle before creating the real 
			//// ClientSocketConnection with the correct buffer size to accomodate entire blocks
			//handles.Add( 0L, new ClientSocketConnection( ClientSessionManager.basePort + 1, 4096 ) );  
			//handlesLock = new AsyncSemaphore( 1 );
			//guardGenerator = new Random();
		}


		// %TODO% - static destructor to tear down handles dictionary and reclaim ClientSocketConnections
		#endregion

		#region Helpers
		static private int CreateOpenSetOperationRequest( ClientSocketConnection clientConnection, APIOperation op, string setName, uint setMode, uint desiredAccess, uint setAttributes, uint shareMode, uint blockSize = 0 /* Open doesn't pass it */)
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce + apiTokenLength + apiToken + userTokenLength + userToken + setNameLength + setMask + setmode + desiredAccess, + setAttributes + shareMode )]
			//int bytesWritten = 0;
			int guard = guardGenerator.Next();
			//const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//Debug.Print( "CLIENT: CreateSetOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[(10 * sizeof( int )) + ClientSessionManager.apiToken.Length + ClientSessionManager.userToken.Length + (2 * setName.Length)];  // NOTE:  2 * setName.Length because of Unicode encoding below
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
			Buffer.BlockCopy( setNameUnicodeBytes, 0, encryptedPayLoad, mainpayloadOffset, setNameUnicodeBytes.Length );									// setMask
			mainpayloadOffset += setNameUnicodeBytes.Length;
			Buffer.BlockCopy( BitConverter.GetBytes( setMode ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );									// setMode
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( desiredAccess ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );								// desiredAccess
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( setAttributes ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );								// setAttributes
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( shareMode ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );									// shareMode
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( blockSize ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( uint ) );									// blockSize
			mainpayloadOffset += sizeof( uint );
			encryptedPayLoad = EncryptionDecryption.Encrypt( encryptedPayLoad, ref ClientSessionManager.sessionSymKey, ref ClientSessionManager.sessionSymIV, false );
			#endregion
			return WriteRequest( op, request, guard, encryptedPayLoad );
		}

		static private int CreateHandleOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long handle )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce + handle )]
			//int bytesWritten = 0;
			int guard = guardGenerator.Next();
			//const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//Debug.Print( "CLIENT: CreateHandleOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[(2 * sizeof( int )) + sizeof(long)];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nounce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( handle ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );									// handle
			mainpayloadOffset += sizeof( long );
			encryptedPayLoad = EncryptionDecryption.Encrypt( encryptedPayLoad, ref ClientSessionManager.sessionSymKey, ref ClientSessionManager.sessionSymIV, false );
			#endregion

			return WriteRequest( op, request, guard, encryptedPayLoad );
		}

		static private int CreateRecordNavigationOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long handle, int recordsToMove )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce + handle + recordsToMove )]
			//int bytesWritten = 0;
			int guard = guardGenerator.Next();
			//const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//Debug.Print( "CLIENT: CreateHandleOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[(3 * sizeof( int )) + sizeof( long )];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nounce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( handle ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );									// handle
			mainpayloadOffset += sizeof( long );
			Buffer.BlockCopy( BitConverter.GetBytes( recordsToMove ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );								// recordsToMove
			mainpayloadOffset += sizeof( int );
			encryptedPayLoad = EncryptionDecryption.Encrypt( encryptedPayLoad, ref ClientSessionManager.sessionSymKey, ref ClientSessionManager.sessionSymIV, false );
			#endregion

			return WriteRequest( op, request, guard, encryptedPayLoad );
		}

		static private int CreateGoToRecordOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long handle, ulong recordOrdinal, bool isRollback )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nonce + handle + recordOrdinal + isRollback )]
			
			int guard = guardGenerator.Next();

			//Debug.Print( "CLIENT: CreateHandleOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[(2 * sizeof( int )) + sizeof( long ) + sizeof(ulong) + sizeof(byte)];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nounce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( handle ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );									// handle
			mainpayloadOffset += sizeof( long );
			Buffer.BlockCopy( BitConverter.GetBytes( recordOrdinal ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( ulong ) );							// recordOrdinal
			mainpayloadOffset += sizeof( ulong );
			encryptedPayLoad[mainpayloadOffset] = (byte)( isRollback ? 1 : 0 );																				// isRollback
			mainpayloadOffset++;
			encryptedPayLoad = EncryptionDecryption.Encrypt( encryptedPayLoad, ref ClientSessionManager.sessionSymKey, ref ClientSessionManager.sessionSymIV, false );
			#endregion

			return WriteRequest( op, request, guard, encryptedPayLoad );
		}

		static private void ParseHandleOperationResponsePayload( int payloadOffset, int payloadSize, byte[] response, ref long handle, ref uint blocksize )
		{
			int offset = 0;
			// Response: PAYLOAD[  ENCRYPT( handle )]
			//Debug.Print( "[INF] CLIENT:  ClientSetManager.ParseHandleOperationResponsePayLoad() {0}", payloadSize );
			byte[] encryptedbytes = new byte[payloadSize];
			Buffer.BlockCopy( response, payloadOffset, encryptedbytes, 0, payloadSize );
			byte[] decryptedPayLoad = EncryptionDecryption.Decrypt( encryptedbytes, ClientSessionManager.sessionSymKey, ClientSessionManager.sessionSymIV, false );
			handle = BitConverter.ToInt64( decryptedPayLoad, offset );		// handle
			offset += sizeof( long );
			blocksize = BitConverter.ToUInt32( decryptedPayLoad, offset );
			offset += sizeof( int );
		}

		static private int ParseSetMemberOrdinalOperationResponsePayload( int payloadOffset, int payloadSize, byte[] response, OrdinalResponse ordResponse )
		{
			int offset = 0;
			// Response: PAYLOAD[  ENCRYPT( setMemberOrdinal + lastSetMemberOrdinal + EoT + BoT + IsDirty + IsDeleted )]
			byte[] encryptedbytes = new byte[payloadSize];
			Buffer.BlockCopy( response, payloadOffset, encryptedbytes, 0, payloadSize );
			byte[] decryptedPayLoad = EncryptionDecryption.Decrypt( encryptedbytes, ClientSessionManager.sessionSymKey, ClientSessionManager.sessionSymIV, false );
			ordResponse.Ordinal = BitConverter.ToUInt64( decryptedPayLoad, offset );					// setMemberOrdinal
			offset += sizeof( ulong );
			ordResponse.LastOrdinal = BitConverter.ToUInt64( decryptedPayLoad, offset );				// lastSetMemberOrdinal
			offset += sizeof( ulong );
			ordResponse.IsEoT =  decryptedPayLoad[offset] == 1 ? true : false;							// IsEoT
			offset++;
			ordResponse.IsBoT = decryptedPayLoad[offset] == 1 ? true : false;							// IsBoT
			offset++;
			ordResponse.IsDirty = decryptedPayLoad[offset] == 1 ? true : false;							// IsDirty
			offset++;
			ordResponse.IsDeleted = decryptedPayLoad[offset] == 1 ? true : false;						// IsDeleted
			offset++;
			return offset;
		}

		static private void ParseHandleAndSchemaOperationResponsePayload( int payloadOffset, int payloadSize, byte[] response, ref long handle, ref ulong recordCount, ref ulong recordOrdinal, ref bool isDeleted, ref int fieldCount, ref FieldDef[] fields  )
		{
			int offset = 0;
			// Response: PAYLOAD[  ENCRYPT( handle + recordCount + recordOrdinal + fieldDefListLength + fieldDefList )]
			//Debug.Print( "[INF] CLIENT:  ClientSetManager.ParseHandleOperationResponsePayLoad() {0}", payloadSize );
			byte[] encryptedbytes = new byte[payloadSize];
			Buffer.BlockCopy( response, payloadOffset, encryptedbytes, 0, payloadSize );
			byte[] decryptedPayLoad = EncryptionDecryption.Decrypt( encryptedbytes, ClientSessionManager.sessionSymKey, ClientSessionManager.sessionSymIV, false );
			handle = BitConverter.ToInt64( decryptedPayLoad, offset );							// handle
			offset += sizeof( long );
			recordCount = BitConverter.ToUInt64( decryptedPayLoad, offset );					// recordCount
			offset += sizeof( ulong );
			recordOrdinal = BitConverter.ToUInt64( decryptedPayLoad, offset );					// recordOrdinal
			offset += sizeof( ulong );
			isDeleted = decryptedPayLoad[offset] == 1 ? true : false;							// IsDeleted
			offset++;
			fieldCount = BitConverter.ToInt32( decryptedPayLoad, offset );						// fieldCount
			offset += sizeof( int );
			// Loop to parse FieldDefs that make up the Table's schema
			fields = new FieldDef[fieldCount];
			for(int i = 0; i< fieldCount; i++ )
			{
				fields[i] = SetSchema.DeserializeFieldDefinitionFromBytes(decryptedPayLoad, ref offset, i);
			}
			
		}

		static private int WriteRequest( APIOperation op, byte[] request, int guard, byte[] encryptedPayLoad )
		{
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			int bytesWritten = 0;
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
			//Debug.Print( "CLIENT: CreateHandleOperationRequest - total size (including operator+payloadsize)={0}, payloadOffset={1}", bytesWritten, 8 );
			return bytesWritten;
		}
		#endregion
	}

	public class OrdinalResponse
	{
		public APIOperationResponseCode ResponseCode;
		public ulong Ordinal;
		public ulong LastOrdinal;
		public bool IsDeleted = false;
		public bool IsEoT = true;
		public bool IsBoT = true;
		public bool IsDirty = false;
	}
}
