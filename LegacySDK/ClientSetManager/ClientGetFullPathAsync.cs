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
		static public async Task<string> GetFullSetPath( long handle )
		{
			string result = null;
			try
			{
				// Call into Unosys's SetSystem to find the full path of the Set identified by handle
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
				int bytesWritten = CreateHandleOperationRequest( clientSocketConnection, APIOperation.GetFullPath, handle );

				#region Call Unosys.Processor on socket to read from the Set
				APIOperationResponseCode responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.GetFullPath, bytesWritten );
				if (responseResult == APIOperationResponseCode.OK)
				{
					// Read the returned bytes as a string value
					result = Encoding.Unicode.GetString( clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize );
					Debug.Print( "SDK.GetFullSetPath = {0}, handle={1}, value: {2}", responseResult, handle, result );
				}
				else
				{
					Debug.Print( "SDK.GetFullSetPath() failed with responseResult={0}", responseResult );
				}
				#endregion
			}
			catch (Exception)
			{
				throw;
			}

			return result;
		}
		#endregion
	}
}
