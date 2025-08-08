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
		static public async Task<APIOperationResponseCode> SetSetPropAsync( long handle, SetPropertyDTO propertyValuesToApply )
		{
			APIOperationResponseCode responseResult = APIOperationResponseCode.FALSE;
			try
			{
				// Call into Unosys's SetSystem to apply the supplied property values of the Set identified by handle
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
				int bytesWritten = CreateGetPropertyOperationRequest( clientSocketConnection, APIOperation.SetProperty, handle, propertyValuesToApply );

				#region Call Unosys.Processor on socket to close Set
				responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.SetProperty, bytesWritten );
				if (responseResult == APIOperationResponseCode.OK)
				{
					// NOTE:  There is no payload to the response so the responseResult is all we need...
					Debug.Print( "SDK.SetSetProp = {0}, handle={1}", responseResult, handle );
				}
				else
				{
					Debug.Print( "SDK.SetSetPropAsync() failed with responseResult={0}", responseResult );
				}
				#endregion
			}
			catch (Exception)
			{
				//responseResult = APIOperationResponseCode.INVALID_ARGUMENT;
				throw;
			}

			return responseResult;
		}
		#endregion
	}
}
