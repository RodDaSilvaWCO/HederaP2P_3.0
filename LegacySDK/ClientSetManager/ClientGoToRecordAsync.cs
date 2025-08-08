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
		static public async Task<OrdinalResponse> GoToRecordAsync( long handle, ulong recordOrdinal, bool isRollback = false )
		{
			OrdinalResponse ordResponse = new OrdinalResponse();
			int bytesWritten = 0;
			try
			{
				// Call into Unosys's DatabaseSystem to GoTo the desired record of the Table identified by handle
				//
				// Step #1:  Lookup clientConnection from handle
				ClientSocketConnection clientSocketConnection = null;
				if (handles.ContainsKey( handle ))
				{
					clientSocketConnection = handles[handle];
				}
				else
				{
					ordResponse.ResponseCode = APIOperationResponseCode.INVALID_HANDLE;
					return ordResponse;
				}

				// Step #2  create the UnoSys.Process request
				bytesWritten = CreateGoToRecordOperationRequest( clientSocketConnection, APIOperation.GoTo, handle, recordOrdinal, isRollback );

				#region Call Unosys.Processor on socket 
				ordResponse.ResponseCode = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.GoTo, bytesWritten );
				if (ordResponse.ResponseCode == APIOperationResponseCode.OK)
				{
					// Obtain the ordinal of the record moved to from the response payload
					ParseSetMemberOrdinalOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
							clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ordResponse );
					//Debug.Print( "[INF] CLIENT:  ClientSetManager.WriteTableRecordAsnyn() result={0}, handle={1}", responseResult, handle );
				}
				else
				{
					// There was a problem so just return the default ordResponse
					// NOP
					Debug.Print( "[INF] CLIENT:  ClientSetManager.GoToRecordAsync() failed with responseResult={0}", ordResponse.ResponseCode );
				}
				#endregion
			}
			catch (Exception ex)
			{
				// Unknown issue...assume caused by invalid argument
				ordResponse.ResponseCode = APIOperationResponseCode.INVALID_ARGUMENT;
				Debug.Print( "[ERR] CLIENT:  ClientSetManager.GoToRecordAsync() failed with error={0}", ex );
			}
			return ordResponse;
		}
		#endregion
	}
}
