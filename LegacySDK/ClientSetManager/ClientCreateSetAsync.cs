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
		static public async Task<long> CreateSetAsync( string setName, uint setMode, uint desiredAccess, uint setAttributes, uint shareMode, uint blockSize )
		{
			long handle = -1;
			APIOperationResponseCode responseResult = APIOperationResponseCode.FALSE;
			try
			{
				// Call into Unosys's SetSystem to create a Set and return a handle to it
				// NOTE:  Each Set that is created has its own dedicated socket/handle pairing....
				//
				// Step #1 create the socket for the Set being requested
				//ClientSocketConnection clientSocketConnection = new ClientSocketConnection( ClientSessionManager.basePort + 1 );  

				// Step #2  create the UnoSys.Process request
				//int bytesWritten = CreateOpenSetOperationRequest( clientSocketConnection, APIOperation.CreateSet, setName, setMode, desiredAccess, setAttributes, shareMode );
				int bytesWritten = CreateOpenSetOperationRequest( handles[0], APIOperation.CreateSet, setName, setMode, desiredAccess, setAttributes, shareMode, blockSize );

				#region Call Unosys.Processor on socket to establish a conneciton for this Set - if succssful call will return a valid handle to the Set
				//responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.CreateSet, bytesWritten );
				responseResult = await handles[0].ProcessRequestResponseAsync( APIOperation.CreateSet, bytesWritten );
				if (responseResult == APIOperationResponseCode.OK)
				{
					//ParseHandleOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
					//		clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref handle );
					uint notUsed = 0;
					ParseHandleOperationResponsePayload( handles[0].responsePayLoadOffset, handles[0].responsePayloadSize,
							handles[0].socketAwaitable.m_eventArgs.Buffer, ref handle, ref notUsed );

					if (handle > 0)
					{
						// We complete the CreateSet() operation by adding the handle/clientSocketConnection key/value pair to the handles dictionary
						await handlesLock.WaitAsync();
						//handles.Add( handle, clientSocketConnection );
						handles.Add( handle, new ClientSocketConnection( ClientSessionManager.basePort + 1, blockSize ) );
					}
					else
					{
						throw new InvalidOperationException();
					}
					Debug.Print( "API.CreateSet = {0}, handle={1}", responseResult, handle );
				}
				else
				{
					Debug.Print( "API.CreateSetAsnyc() failed with responseResult={0}", responseResult );
				}
				#endregion
			}
			catch (Exception e)
			{
				Debug.Print( "API.CreateSetAsnyc() threw exception: {0}", e );
				responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
			}
			finally
			{
				if (responseResult == APIOperationResponseCode.OK && handle > 0)
				{
					handlesLock.Release();
				}
			}

			return handle;
		}
		#endregion
	}
}


