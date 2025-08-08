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
		//static public async Task<APIOperationResponseCode> CloseSetAsync( long handle )
		//{
		//	APIOperationResponseCode responseResult = APIOperationResponseCode.FALSE;
		//	try
		//	{
		//		// Call into Unosys's SetSystem to close the Set identified by handle
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
		//		int bytesWritten = CreateHandleOperationRequest( clientSocketConnection, APIOperation.CloseSet, handle );

		//		#region Call Unosys.Processor on socket to close Set
		//		responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.CloseSet, bytesWritten );
		//		if (responseResult == APIOperationResponseCode.OK)
		//		{
		//			// NOTE:  There is no payload to the response so the responseResult is all we need...
		//			try
		//			{
		//				await handlesLock.WaitAsync();  // acquire lock on handles dictionary 
		//				clientSocketConnection.Close();
		//				handles.Remove( handle );		// remove handle from dictionary
		//			}
		//			finally
		//			{
		//				handlesLock.Release();
		//			}

		//			//Debug.Print( "CLIENT: SDK.ClientSetManager.CloseSetAsync() = {0}, handle={1}", responseResult, handle );
		//		}
		//		else
		//		{
		//			Debug.Print( "[INF] CLIENT: SDK.ClientSetManager.CloseSetAsnyc() failed with responseResult={0}", responseResult );
		//		}
		//		#endregion
		//	}
		//	catch (Exception)
		//	{
		//		responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
		//	}

		//	return responseResult;
		//}
		#endregion
	}
}
