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
		//static public async Task<int> WriteSetAsync( long handle, byte[] bufferToWrite, int bufferOffset, int maxBytesToWrite, ulong deviceOffsetToWriteTo )
		//{
		//	int bytesWritten = 0;
		//	try
		//	{
		//		// Call into Unosys's SetSystem to write bytes to the Set identified by handle
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
		//		// NOTE: The bytesWritten is the number of bytes we took from the supplied buffer to write to the set, we don't yet have a guarantee that they arrived there
		//		//int bytesSent = CreateWriteSetOperationRequest( clientSocketConnection, APIOperation.WriteSet, handle, bufferToWrite, bufferOffset, maxBytesToWrite, deviceOffsetToWriteTo, out bytesWritten );
		//		bytesWritten = CreateWriteSetOperationRequest( clientSocketConnection, APIOperation.WriteSet, handle, bufferToWrite, bufferOffset, maxBytesToWrite, deviceOffsetToWriteTo);

		//		#region Call Unosys.Processor on socket to close Set
		//		APIOperationResponseCode responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.WriteSet, bytesWritten );
		//		if (responseResult == APIOperationResponseCode.OK)
		//		{
		//			// NOTE: Pursuant to note above, now we can assume the bytes arrived where we sent them!
		//			Debug.Print( "SDK.WriteSet = {0}, handle={1}", responseResult, handle );
		//		}
		//		else
		//		{
		//			Debug.Print( "SDK.WriteSetAsync() failed with responseResult={0}", responseResult );
		//		}
		//		#endregion
		//	}
		//	catch (Exception)
		//	{
		//		//responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
		//		throw;
		//	}

		//	return bytesWritten;
		//}

		// %TODO% - NOTE:  THis routine is untested as of Dec/29/2017 - Remove this comment when tested
		static public async Task<int> WriteSetAsync( long handle, byte[] bufferToWrite, int bufferOffset, int maxBytesToWrite, ulong deviceOffsetToWriteTo )
		{
			int bytesWritten = 0;
			try
			{
				// Call into Unosys's SetSystem to write bytes to the Set identified by handle
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
				uint unsedSpaceInLastBlock = (uint) maxBytesToWrite % maxBlockSize;
				unsedSpaceInLastBlock = (unsedSpaceInLastBlock == 0 ? maxBlockSize : unsedSpaceInLastBlock);
				ulong startBlock = deviceOffsetToWriteTo / (uint) maxBlockSize;
				uint numBlocks = ((uint) maxBytesToWrite / maxBlockSize) + (((uint) maxBytesToWrite) % maxBlockSize == (uint) 0 ? (uint) 0 : (uint) 1);
				uint blockIndex = 0;
				// Loop for as many blocks as required
				for (ulong block = startBlock; block < startBlock + numBlocks; block++)
				{
					uint offsetBlock = blockIndex * maxBlockSize;
					uint actualBlockSize = ((blockIndex + 1) == numBlocks ? unsedSpaceInLastBlock : maxBlockSize);
					// Create the UnoSys.Process request
					bytesWritten = CreateWriteSetOperationRequest( clientSocketConnection, APIOperation.WriteSet, handle, bufferToWrite, bufferOffset, (int) actualBlockSize, (ulong) offsetBlock );
					Debug.Print( "blockIndex={0}, offsetBlock={1}, bytesWritten={2}", blockIndex, offsetBlock, bytesWritten );
					#region Call Unosys.Processor on socket to write to the Set
					APIOperationResponseCode responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.WriteSet, bytesWritten );
					if (responseResult != APIOperationResponseCode.OK)
					{
						Debug.Print( "SDK.WriteSetAsync() failed with responseResult={0}", responseResult );
						bytesWritten = 0;
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

			return bytesWritten;
		}
		#endregion

		#region Helpers
		//static private int CreateWriteSetOperationRequest( ClientSocketConnection clientConnection, APIOperation op, int handle, byte[] bufferToWrite, int bufferOffset, int maxBytesToWrite, ulong deviceOffsetToWriteTo, out int bytesWrittenToSet )
		static private int CreateWriteSetOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long handle, byte[] bufferToWrite, int bufferOffset, int numBytesToWrite, ulong deviceOffsetToWriteTo )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nounce + handle + deviceOffsetToWriteTo + byteCount + bytes )]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			Debug.Print( "CLIENT: CreateWriteStOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			//int numberOfBytesToWrite = bufferToWrite.Length - bufferOffset;
			//bytesWrittenToSet = numberOfBytesToWrite;
			//byte[] encryptedPayLoad = new byte[4 * sizeof( int ) + sizeof( ulong ) + numberOfBytesToWrite];
			byte[] encryptedPayLoad = new byte[3 * sizeof( int ) + sizeof(long) + sizeof( ulong ) + numBytesToWrite];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nounce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( handle ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );										// handle
			mainpayloadOffset += sizeof( long );
			Buffer.BlockCopy( BitConverter.GetBytes( deviceOffsetToWriteTo ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( ulong ) );					// deviceOffsetToWriteTo
			mainpayloadOffset += sizeof( ulong );
			//Buffer.BlockCopy( BitConverter.GetBytes( numberOfBytesToWrite ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );						// byteCount
			Buffer.BlockCopy( BitConverter.GetBytes( numBytesToWrite ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );							// byteCount
			mainpayloadOffset += sizeof( int );
			//Buffer.BlockCopy( bufferToWrite, bufferOffset, encryptedPayLoad, mainpayloadOffset, numberOfBytesToWrite );									// bytes
			Buffer.BlockCopy( bufferToWrite, bufferOffset, encryptedPayLoad, mainpayloadOffset, numBytesToWrite );											// bytes
			mainpayloadOffset += numBytesToWrite;
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
			Debug.Print( "CLIENT: CreateWriteSetOperationRequest - total size (including operator+payloadsize)={0}, payloadOffset={1}", bytesWritten, 8 );
			return bytesWritten;
		}
		#endregion
	}
}
