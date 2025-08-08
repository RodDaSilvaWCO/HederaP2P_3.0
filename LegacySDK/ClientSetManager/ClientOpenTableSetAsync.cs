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
		#region SetManager Implementation
		//static public async Task<HandleWithFieldDefs> OpenTableSetAsync( string tableName, /*SetSchema tableSchema,*/ uint setMode, uint desiredAccess, uint setAttributes, uint shareMode, uint blockSize )
		//{
		//	long handle = -1;
		//	APIOperationResponseCode responseResult = APIOperationResponseCode.FALSE;
		//	HandleWithFieldDefs handleWithFieldDefs = new HandleWithFieldDefs( handle, 0, 0, false, null!, responseResult);  // default
		//	FieldDef[] fields = null;
		//	int fieldCount = 0;
		//	ulong recordCount = 0;
		//	ulong recordOrdinal = 0;
		//	bool isDeleted = false;
		//	try
		//	{
		//		// Call into Unosys's DatabaseSystem to open a Set and return a handle to it
		//		// NOTE:  Each Set that is opened has its own dedicated socket/handle pairing....
		//		//
		//		// Step #1 create the socket for the Set being requested
		//		ClientSocketConnection clientSocketConnection = new ClientSocketConnection( 5002, 8 * 1024 );  // %TODO:  Get these constants from Registry

		//		// Step #2  create the UnoSys.Process request
		//		int bytesWritten = CreateOpenSetOperationRequest( clientSocketConnection, APIOperation.OpenSet, tableName, setMode, desiredAccess, setAttributes, shareMode, blockSize );

		//		#region Call Unosys.Processor on socket to establish a conneciton for this Set - if succssful call will return a valid handle to the Set
		//		responseResult = await clientSocketConnection.ProcessRequestResponseAsync( APIOperation.OpenSet, bytesWritten );
		//		if (responseResult == APIOperationResponseCode.OK)
		//		{
		//			//ParseHandleOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
		//			//		clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref handle );
		//			ParseHandleAndSchemaOperationResponsePayload( clientSocketConnection.responsePayLoadOffset, clientSocketConnection.responsePayloadSize,
		//										clientSocketConnection.socketAwaitable.m_eventArgs.Buffer, ref handle, ref recordCount, ref recordOrdinal, ref isDeleted, ref fieldCount, ref fields );
		//			if (handle > 0)
		//			{
		//				//Debug.Print( "[INF] CLIENT:  ClientSetManager.OpenTableSetAsync fieldCount={0},{1}", fieldCount, fields.Length );
		//				// We complete the Open Set operation by adding the handle/clientSocketConnection key/value pair to the handles dictionary
		//				await handlesLock.WaitAsync();
		//				handles.Add( handle, clientSocketConnection );
		//				handleWithFieldDefs = new HandleWithFieldDefs( handle, recordOrdinal, recordCount, isDeleted, null! /* %TODO% */, responseResult );
		//			}
		//			else
		//			{
		//				// Bad handle 
		//				handleWithFieldDefs.responseCode = APIOperationResponseCode.INVALID_HANDLE;
		//			}
		//			//Debug.Print( "[INF] CLIENT:  ClientSetManager.OpenTableSetAsync() = {0}, handle={1}", responseResult, handle );
		//		}
		//		else
		//		{
		//			// Bad responseResult 
		//			handleWithFieldDefs.responseCode = responseResult;
		//			Debug.Print( "[INF] CLIENT:  ClientSetManager.OpenTableSetAsnyc() failed with responseResult={0}", responseResult );
		//		}
		//		#endregion
		//	}
		//	catch (Exception e)
		//	{
		//		// Unknown issue...assume invalide argument caused it
		//		handleWithFieldDefs.responseCode = APIOperationResponseCode.INVALID_ARGUMENT; ;
		//		Debug.Print( "[ERR] CLIENT:  ClientSetManager.OpenTableSetAsnyc() threw exception: {0}", e );
		//	}
		//	finally
		//	{
		//		if (responseResult == APIOperationResponseCode.OK && handle > 0)
		//		{
		//			handlesLock.Release();
		//		}
		//	}

		//	return handleWithFieldDefs;
		//}
		#endregion
	}

	//public class HandleWithFieldDefs
	//{
	//	public long Handle;
	//	public SetSchema TableSchema = null!;
	//	public APIOperationResponseCode responseCode = APIOperationResponseCode.OK;
	//	public ulong InitialRecordOrdinal = 0;
	//	public ulong LastRecordOrdinal = 0;
	//	public bool IsDeleted = false;

	//	public HandleWithFieldDefs( long handle, ulong initialRecordOrdinal, ulong lastRecordOrdinal, bool isDeleted, SetSchema tableSchema, APIOperationResponseCode result )
	//	{
	//		Handle = handle;
	//		InitialRecordOrdinal = initialRecordOrdinal;  // the record ordinal of the first record to be positioned on after Open/Create
	//		LastRecordOrdinal = lastRecordOrdinal;		  // the last record of the Open/Create Table
	//		IsDeleted = isDeleted;						  // the deleted status of the first record to be positioned on after Open/Create
	//		TableSchema = tableSchema;
	//		responseCode = result;
	//	}
	//}
}


