using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	/// <summary>
	/// All the data required to construct a FileInfo object
	/// </summary>
	[Serializable]
	internal struct SetPropertyDTO
	{
		//public System.IO.FileAttributes? Attributes;
		public DateTime? CreationTime;
		//public System.IO.DirectoryInfo? ParentDirectory;
		//public bool? Exists;		// False if file does not exist (?!?) or is a directory
		//public bool? IsReadOnly;
		public uint? Attributes;	// Use this instead of IsReadOnly or similar boolean attribute flags
		public DateTime? LastAccessTime;
		public DateTime? LastWriteTime;
		public ulong? Length;
		//public string Name;		// Variable-length string would turn this into a variable-length structure, no good for byte[]-based DTO!

		public override string ToString()
		{
			return string.Format( "{{CreationTime: {0}, LastAccessTime: {1}, LastWriteTime: {2}, Attributes: {3}, Length: {4}}}", CreationTime, LastAccessTime, LastWriteTime, Attributes, Length );
		}

		public static SetPropertyDTO FromBytes( byte[] value, int startIndex, int bytesToRead )
		{
			SetPropertyDTO result = new SetPropertyDTO();
			if (bytesToRead > 0)
			{
				var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal( bytesToRead );
				System.Runtime.InteropServices.Marshal.Copy( value, startIndex, ptr, bytesToRead );
				result = System.Runtime.InteropServices.Marshal.PtrToStructure<SetPropertyDTO>( ptr );
				System.Runtime.InteropServices.Marshal.FreeHGlobal( ptr );
			}

			return result;
		}

		public static byte[] ToBytes( SetPropertyDTO data )
		{
			int size = System.Runtime.InteropServices.Marshal.SizeOf( data );
			byte[] result = new byte[size];
			var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal( size );
			System.Runtime.InteropServices.Marshal.StructureToPtr<SetPropertyDTO>( data, ptr, false );
			System.Runtime.InteropServices.Marshal.Copy( ptr, result, 0, size );
			System.Runtime.InteropServices.Marshal.FreeHGlobal( ptr );
			return result;
		}
	}

	internal static partial class ClientSetManager
	{
		#region SetManager Implementation
		static public async Task<SetPropertyDTO> GetSetPropAsync( long handle, SetPropertyDTO propertyValuesToFetch )
		{
			SetPropertyDTO result = new SetPropertyDTO();	// A blank value to return if all else fails
			try
			{
				// Call into Unosys's SetSystem to fetch the desired property value of the Set identified by handle
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

				// Step #2  create the UnoSys.Process request
				int bytesWritten = CreateGetPropertyOperationRequest( clientSocketConnection, APIOperation.GetProperty, handle, propertyValuesToFetch );

				#region Call Unosys.Processor on socket to close Set
				var responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.GetProperty, bytesWritten );
				if (responseResult == APIOperationResponseCode.OK)
				{
					// Fetch the returned DTO and return it!
					result = SetPropertyDTO.FromBytes( clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize );
					//Buffer.BlockCopy( clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, clientSocketConnection.responsePayLoadOffset, bufferToFill, bufferOffset, bytesRead );
					//Debug.Print( "SDK.GetSetProp = {0}, handle = {1}, property = {2}", responseResult, handle, result );
				}
				else
				{
					Debug.Print( "SDK.GetSetPropAsync() failed with responseResult={0}", responseResult );
				}
				#endregion
			}
			catch (Exception)
			{
				//responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
				throw;
			}

			return result;
		}
		#endregion

		#region Helpers
		static private int CreateGetPropertyOperationRequest( ClientSocketConnection clientConnection, APIOperation op, long handle, SetPropertyDTO propertyValuesToFetch )
		{
			byte[] request = clientConnection.socketAwaitable.m_eventArgs.Buffer;
			// Request:  Operation + PayLoadSize + PAYLOAD[ HEADER(SessionID + Guard) + ENCRYPT(Guard + Nonce + handle + propertiesLength + propertyValuesToFetch )]
			int bytesWritten = 0;
			int guard = guardGenerator.Next();
			const int PAYLOAD_HEADER_SIZE = 2 * sizeof( int );
			//Debug.Print( "CLIENT: CreateGetPropertyOperationRequest - ClientSessionManager.sessionSymIV .Length={0}", ClientSessionManager.sessionSymIV.Length );

			#region Compute payload portion to be encrypted
			var buffer = SetPropertyDTO.ToBytes( propertyValuesToFetch );
			int propertiesLength = buffer.Length;
			byte[] encryptedPayLoad = new byte[(3 * sizeof( int )) + sizeof(long) + propertiesLength];
			int mainpayloadOffset = 0;
			Buffer.BlockCopy( BitConverter.GetBytes( guard ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );										// guard
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( ++clientConnection.nounce ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );					// nonce
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( BitConverter.GetBytes( handle ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( long ) );									// handle
			mainpayloadOffset += sizeof( long );
			Buffer.BlockCopy( BitConverter.GetBytes( propertiesLength ), 0, encryptedPayLoad, mainpayloadOffset, sizeof( int ) );							// propertiesLength
			mainpayloadOffset += sizeof( int );
			Buffer.BlockCopy( buffer, 0, encryptedPayLoad, mainpayloadOffset, propertiesLength );															// propertyValuesToFetch
			mainpayloadOffset += propertiesLength;
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
			//Debug.Print( "CLIENT: CreateGetPropertyOperationRequest - total size (including operator+payloadsize)={0}, payloadOffset={1}", bytesWritten, 8 );
			return bytesWritten;
		}

		//static private void ParseOpenSetOperationResponsePayload( int offset, int payloadSize, byte[] response, ref int handle )
		//{
		//	handle = ++_temphandle;
		//}
		#endregion
	}
}
