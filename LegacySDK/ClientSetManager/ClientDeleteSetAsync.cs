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
		static public async Task<APIOperationResponseCode> DeleteSetAsync( long handle )
		{
			// NOTE:  **** This method is used for BOTH Sets and TableSets ********
			APIOperationResponseCode responseResult = APIOperationResponseCode.FALSE;
			try
			{
				// Call into Unosys's SetSystem to delete the Set identified by handle
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
				int bytesWritten = CreateHandleOperationRequest( clientSocketConnection, APIOperation.DeleteSet, handle );

				#region Call Unosys.Processor on socket to close Set
				responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.DeleteSet, bytesWritten );
				if (responseResult == APIOperationResponseCode.OK)
				{
					// NOTE:  There is no payload to the response so the responseResult is all we need...
					try
					{
						// When you delete a Set the handle is no longer  valid (i.e.; DeleteSetAsync() closes the handle on the SERVER side) 
						// so we close the socket connection associated with the handle and remove it from handles collection
						await handlesLock.WaitAsync();  // acquire lock on handles dictionary 
						clientSocketConnection.Close();
						handles.Remove( handle );		// remove handle from dictionary
					}
					catch(Exception ex)
					{
						Debug.Print("CLIENT: SDK.ClientSetManager.DeleteSetAsync() error: {0}",ex);
					}
					finally
					{
						handlesLock.Release();
					}

					Debug.Print( "CLIENT: SDK.ClientSetManager.DeleteSetAsync = {0}, handle={1}", responseResult, handle );
				}
				else
				{
					Debug.Print( "CLIENT: SDK.ClientSetManager.DeleteSetAsync() failed with responseResult={0}", responseResult );
				}
				#endregion
			}
			catch (Exception)
			{
				responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
			}

			return responseResult;
		}
		#endregion


	}
}
