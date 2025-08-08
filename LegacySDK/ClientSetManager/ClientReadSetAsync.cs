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
		//static public async Task<int> ReadSetAsync( long handle, byte[] bufferToFill, int bufferOffset, int numBytesToRead, ulong deviceOffsetToReadFrom )
		//{
		//	int bytesRead = 0;
		//	try
		//	{
		//		// Call into Unosys's SetSystem to read bytes from the Set identified by handle
		//		//
		//		// Step #1:  Lookup clientConnection from handle
		//		ClientSocketConnection clientSocketConnection = null;
		//		if (handles.ContainsKey( handle ))
		//		{
		//			clientSocketConnection = handles[handle];
		//		}
		//		else
		//		{
		//			throw new ArgumentException( "Unknown handle" );
		//		}

		//		// Step #2  create the UnoSys.Process request
		//		int bytesWritten = CreateReadSetOperationRequest( clientSocketConnection, APIOperation.ReadSet, handle, numBytesToRead, deviceOffsetToReadFrom );

		//		#region Call Unosys.Processor on socket to read from the Set
		//		APIOperationResponseCode responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.ReadSet, bytesWritten );
		//		if (responseResult == APIOperationResponseCode.OK)
		//		{
		//			// Copy read bytes into bufferToFill
		//			bytesRead = clientSocketConnection.responsePayloadSize;
		//			Buffer.BlockCopy( clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, clientSocketConnection.responsePayLoadOffset, bufferToFill, bufferOffset, bytesRead );
		//		}
		//		else
		//		{
		//			Debug.Print( "SDK.ReadSetAsnyc() failed with responseResult={0}", responseResult );
		//		}
		//		#endregion
		//	}
		//	catch (Exception)
		//	{
		//		//responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
		//		throw;
		//	}

		//	return bytesRead;
		//}

		static public async Task<int> ReadSetAsync( long handle, byte[] bufferToFill, int bufferOffset, int numBytesToRead, ulong deviceOffsetToReadFrom )
		{
			int bytesRead = 0;
			try
			{
				// Call into Unosys's SetSystem to read bytes from the Set identified by handle
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

				// Determine the Max Block Size that can be passed to the clientSocketConnection
				uint maxBlockSize = (uint) clientSocketConnection.MaxBlockSize;
				uint unsedSpaceInLastBlock = (uint) numBytesToRead % maxBlockSize;
				unsedSpaceInLastBlock = (unsedSpaceInLastBlock == 0 ? maxBlockSize : unsedSpaceInLastBlock);
				ulong startBlock = deviceOffsetToReadFrom / (uint) maxBlockSize;
				uint numBlocks = ((uint) numBytesToRead / maxBlockSize) + (((uint) numBytesToRead) % maxBlockSize == (uint) 0 ? (uint) 0 : (uint) 1);
				uint blockIndex = 0;
				// Loop for as many blocks as required
				for (ulong block = startBlock; block < startBlock + numBlocks; block++)
				{
					uint offsetBlock = blockIndex * maxBlockSize;
					uint actualBlockSize = ((blockIndex + 1) == numBlocks ? unsedSpaceInLastBlock : maxBlockSize);
					// Create the UnoSys.Process request
					int bytesWritten = CreateReadSetOperationRequest( clientSocketConnection, APIOperation.ReadSet, handle, (int) actualBlockSize, (ulong) offsetBlock );
					#region Call Unosys.Processor on socket to read from the Set
					APIOperationResponseCode responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.ReadSet, bytesWritten );
					if (responseResult == APIOperationResponseCode.OK)
					{
						// Copy read bytes into bufferToFill
						bytesRead = clientSocketConnection.responsePayloadSize;
//						Debug.Print( "blockIndex={0}, offsetBlock={1}, bytesRead={2}", blockIndex, offsetBlock, bytesRead );
						Buffer.BlockCopy( clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, clientSocketConnection.responsePayLoadOffset,
									bufferToFill, (int)offsetBlock, bytesRead );
					}
					else
					{
						Debug.Print( "SDK.ReadSetAsnyc() failed with responseResult={0}", responseResult );
					}
					#endregion
					blockIndex++;
				}
			}
			catch (Exception)
			{
				//responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
				throw;
			}

			return bytesRead;
		}
		#endregion

		#region Helpers
		static private int CreateReadSetOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long handle, int numBytesToRead, ulong deviceOffsetToReadFrom )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce + handle + deviceOffsetToReadFrom + maxBytesToRead )]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//Debug.Print( "CLIENT: CreateReadSetOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			byte[] encryptedPayLoad = new byte[3 * sizeof( int ) + sizeof(long) +  sizeof( ulong )];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );								// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );			// nounce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( handle ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );							// handle
			mainpayloadOffset += sizeof( long );
			Buffer.BlockCopy( BitConverter.GetBytes( deviceOffsetToReadFrom ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( ulong ) );			// deviceOffsetToReadFrom
			mainpayloadOffset += sizeof( ulong );
			Buffer.BlockCopy( BitConverter.GetBytes( numBytesToRead ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );						// maxBytesToRead
			mainpayloadOffset += sizeof( int );
			encryptedPayLoad = EncryptionDecryption.Encrypt( encryptedPayLoad, ref ClientSessionManager.sessionSymKey, ref ClientSessionManager.sessionSymIV, false );
			#endregion

			// API operation here...
			Buffer.BlockCopy( BitConverter.GetBytes( (int) op ), 0, request, bytesWritten, sizeof( int ) );											// operation
			bytesWritten += sizeof( int );
			// Total PayLoadSize  (does NOT include the API Operation)
			Buffer.BlockCopy( BitConverter.GetBytes( encryptedPayLoad.Length + PAYLOAD_HEADER_SIZE ), 0, request, bytesWritten, sizeof( int ) );	// payload size
			bytesWritten += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ClientSessionManager.sessionId ), 0, request, bytesWritten, sizeof( int ) );					// sessionID
			bytesWritten += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, request, bytesWritten, sizeof( int ) );											// guard
			bytesWritten += sizeof( int );
			// payload here...
			Buffer.BlockCopy( encryptedPayLoad, 0, request, bytesWritten, encryptedPayLoad.Length );												// payload
			bytesWritten += encryptedPayLoad.Length;
			//Debug.Print( "CLIENT: CreateReadSetOperationRequest - total size (including operator+payloadsize)={0}, payloadOffset={1}", bytesWritten, 8 );
			return bytesWritten;
		}
		#endregion
	}
}
