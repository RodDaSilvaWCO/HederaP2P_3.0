using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Unosys.Common.Types;
using UnoSys.Api;
using UnoSys.Api.Models;
using UnoSys.Api.Exceptions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Text.Json;
using System.Xml.Linq;
using UnoSys.Api.Interfaces;
using System.Reflection.Metadata;
using Unosys.Kernel;
using System.IO;

namespace Unosys.SDK
{
	static public class API
	{
        #region Fields
        static private string UserSessionToken = null!;
		static private Guid VirtualDiskID = default(Guid);
		static private Guid VolumeID = default(Guid);
		static private Guid UserSessionTokenID = default(Guid);
		static private Dictionary<long, Tuple<string, SetPropertyDTO>> findResults;
		static private Dictionary<long, FileStreamContext> handles;
		static private Dictionary<string, long> setNames;
		static private bool initialized = false;
        static private UnoSysApi unoSysApi = null!;

		private const string WC_OSUSER_SESSION_TOKEN = "U00000000000000000000000000000000";
        private const int MAX_FILENAME_LENGTH = 260;
		private const int INVALID_HANDLE = -1;
		#endregion

		//static ctor will run the first time the API type is used
		static API()
		{
			findResults = new Dictionary<long, Tuple<string, SetPropertyDTO>>();
			handles = new Dictionary<long, FileStreamContext>();
            setNames = new Dictionary<string, long>();
            //initialized = ClientSessionManager.CreateSessionAsync().Result;
            unoSysApi = new UnoSysApi();
            initialized = true;
        }

		static public void Initialize()
		{
			var virtualDiskSessionToken = unoSysApi.VirtualDiskCreate(WC_OSUSER_SESSION_TOKEN, WC_OSUSER_SESSION_TOKEN, 1, 1);
			VirtualDiskID = new Guid(virtualDiskSessionToken.Substring(1));
			var volumeSessionToken = unoSysApi.VirtualDiskMount(WC_OSUSER_SESSION_TOKEN, virtualDiskSessionToken, 64 * 1024);
			VolumeID = new Guid(volumeSessionToken.Substring(1));
			unoSysApi.UserCreate(WC_OSUSER_SESSION_TOKEN, "DemoUser", "DEMOUSER");
			UserSessionToken = unoSysApi.UserLogin("DemoUser", "DEMOUSER");
			UserSessionTokenID = new Guid(UserSessionToken.Substring(1));
			//Console.WriteLine($"VDiskID:{VirtualDiskID}, VolID:{VolumeID}, UserSessionToken:{UserSessionToken}");
		}

		static public void Initialize( Guid virtualDiskID, Guid volumeID, string userSessionToken  )
		{
			VirtualDiskID = virtualDiskID;
			VolumeID = volumeID;
			UserSessionToken = userSessionToken;
            UserSessionTokenID = new Guid(UserSessionToken.Substring(1));
        }

		static public void Uninitialize()
		{
			try
			{
                unoSysApi.UserLogout(UserSessionToken);
            }
			catch (Exception)
			{
				// NOP
			}
			try
			{
                unoSysApi.UserDelete(WC_OSUSER_SESSION_TOKEN, "DemoUser", "DEMOUSER");
            }
			catch (Exception)
			{

				// NOP
			}
			try
			{
                unoSysApi.VirtualDiskUnmount(WC_OSUSER_SESSION_TOKEN, "V" + VolumeID.ToString("N").ToUpper());
            }
			catch (Exception)
			{
				// NOP
			}
			try
			{
                unoSysApi.VirtualDiskDelete(WC_OSUSER_SESSION_TOKEN, "Z" + VirtualDiskID.ToString("N").ToUpper());
            }
			catch (Exception)
			{
				// NOP
            }
			
            
        }


        #region User API
        static public bool UserLogin( string username, string password )
		{
			bool result = true;
			try
			{
				UserSessionToken = unoSysApi.UserLogin(username, password);
			}
			catch
			{
				result = false;
			}
			return result;
		}

		static public bool UserLogout( )
		{
			bool result = true;
			try
			{
                unoSysApi.UserLogout(UserSessionToken);
            }
			catch (Exception)
			{
				result = false;
			}
			
			return result;
		}
        #endregion

        #region File & Directory Set API
        #region Implemented
        /// <summary>
        /// Creates and opens a set for further reading, writing, or querying operations
        /// </summary>
        /// <param name="setMask">The set to create</param>
        /// <param name="setMode">Why isn't this a FileMode?</param>
        /// <param name="desiredAccess">Why isn't this a FileAccess?</param>
        /// <param name="setAttributes">Why isn't this a FileOptions?</param>
        /// <param name="shareMode">Why isn't this a FileShare?</param>
        /// <returns>A handle to the opened set</returns>
        static public async Task<long> CreateSetAsync( string setName, int setMode, int desiredAccess, int setAttributes, int shareMode )
		{
			long handle = INVALID_HANDLE;
			try
			{
                #region Validate Parameters
                if (setName.Length > MAX_FILENAME_LENGTH)
				{
					throw new UnoSysArgumentException("File name too long.");
				}
                #endregion

                #region Create FileStreamContext
                FileStreamContext fsContext = new FileStreamContext(setName);
				fsContext.NTCreateDisposition = setMode;
                fsContext.Attributes = setAttributes;
                fsContext.FileLength = 0;
                #endregion

                #region Call unoSysApi.VirtualDiskVolumeMetaDataOperationAsync()
                FILECREATE_Operation operation = new FILECREATE_Operation(UserSessionTokenID , //NOTE: We pass in a userSessionTokenID - OwnerID will be returned
															VirtualDiskID, VolumeID, fsContext.FileID,
						setName, desiredAccess, (setAttributes & 0xFFFF), shareMode, fsContext.NTCreateDisposition, 0);
				//Debug.Print($"CreateSetAsync(A) - FileID={fsContext.FileID} NTCreateDisposition={fsContext.NTCreateDisposition}, IsDir={(fsContext.Attributes & (uint)0x10 /*CBFileAttributes.Directory*/) != 0} F={setName}");
                var opBase64Result = await unoSysApi.VirtualDiskVolumeMetaDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                #endregion 

                #region Set the FileStreamContext.OwnerID to the OwnerID passed back from the API call 
                var opCreateResult = new FILECREATE_Operation(Convert.FromBase64String(opBase64Result));
				fsContext.OwnerID = opCreateResult.OwnerID;
                #endregion

                #region Register Context to generate a handle to return to caller
                handle = RegisterFileStreamContextAndSetName(fsContext, setName);
                #endregion 
            }
            catch (Exception ex)
			{
				Debug.Print($"API.CreateSetAsync(ERROR) - {ex}");
				throw;
			}
			return handle;
            //return  await ClientSetManager.CreateSetAsync( setName, setMode, desiredAccess, setAttributes, shareMode, blockSize );
        }



        /// <summary>
        /// Opens a set for further reading, writing, or querying operations
        /// </summary>
        /// <param name="setMask">The set to open</param>
        /// <param name="setMode">Why isn't this a FileMode?</param>
        /// <param name="desiredAccess">Why isn't this a FileAccess?</param>
        /// <param name="setAttributes">Why isn't this a FileOptions?</param>
        /// <param name="shareMode">Why isn't this a FileShare?</param>
        /// <returns>A handle to the opened set</returns>
        static public async Task<long> OpenSetAsync(string setName, uint setMode, uint desiredAccess, uint setAttributes, uint shareMode)
        {
            //Debug.Print($"CBOpenFile(ENTER) - fileName={e.FileName} NTCreateDisposition={e.NTCreateDisposition}");
            //FileStreamContext Ctx = null;

            //if (setName == "/" || setName == "\\")  // NOP when opening root directory
            //    return;
            long handle = INVALID_HANDLE;
            try
            {
				(FileStreamContext, long) CtxAndHandle = LookupFileStreamContextAndHandleBySetName(setName);
				//if (e.FileContext != IntPtr.Zero)
				//if( Ctx.OpenCount > 1)
				if (CtxAndHandle.Item1 != null)
				{
					//if (Ctx.hStream != null && isWriteRightRequested((CBFileAccess)e.DesiredAccess))
					//{
					//    if (!Ctx.hStream.CanWrite) throw new UnauthorizedAccessException();
					//}
					CtxAndHandle.Item1.IncrementCounter();
					handle = CtxAndHandle.Item2;
				}
				else
				{
					FileStreamContext Ctx = new FileStreamContext(setName);
					Ctx.NTCreateDisposition = (int)setMode;

					#region Call unoSysApi.VirtualDiskVolumeMetaDataOperationAsync()
					FILEOPEN_Operation operation = new FILEOPEN_Operation(UserSessionTokenID, VirtualDiskID, VolumeID, setName, (setAttributes & (uint)0x10 /*CBFileAttributes.Directory*/) != 0);
                    var opBase64Result = await unoSysApi.VirtualDiskVolumeMetaDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
					try
					{
						FILECLOSE_Operation opAttributes = new FILECLOSE_Operation(Convert.FromBase64String(opBase64Result)); // JsonSerializer.Deserialize<FILECLOSE_Operation>(opResult)!;
                        //Ctx.CreateTime = opAttributes.CreateTime;
                        //Ctx.LastAccessTime = opAttributes.LastAccessTime;
                        //Ctx.LastWriteTime = opAttributes.LastWriteTime;
                        Ctx.OwnerID = opAttributes!.OwnerID;
                        Ctx.Attributes = opAttributes!.Attributes;
						Ctx.FileLength = opAttributes.FileLength;
						Ctx.FileID = opAttributes.FileID;
						//Ctx.EventOrigin = opAttributes.EventOrigin;

						#region Register Context to generate a handle to return to caller
						handle = RegisterFileStreamContextAndSetName(Ctx, setName);
						#endregion
					}
					catch(Exception ex1) 
					{
						Debug.Print($"API.OpenSetAsync(ERROR 1) {ex1.Message}");
						// NOP
					}
					//Debug.Print($"API.OpenSetAsync(A) - FileID={Ctx.FileID} F={setName}, Len={Ctx.FileLength}, IsDir={(Ctx.Attributes & (uint)0x10 /*CBFileAttributes.Directory*/) != 0}");
					#endregion
				}
                //if (Ctx != null && Ctx.hStream != null)
                //{ 
                //    Debug.Print($"CBOpenFile(B) - fileName={Ctx.hStream.Name} NTCreateDisposition={e.NTCreateDisposition}");
                //}
                //else
                //{
                //    Debug.Print($"CBOpenFile(C) - *** hStream not defined *** -  fileName={e.FileName} NTCreateDisposition={e.NTCreateDisposition}");
                //}
                //e.FileContext = GCHandle.ToIntPtr(GCHandle.Alloc(Ctx));
            }
            catch (Exception ex)
            {
                Debug.Print($"API.OpenSetAsync(ERROR2) - {ex}");
				throw;
            }
			return handle;
        }

		

        static public async Task<ulong> GetSetLengthAsync(long handle)
        {
            ulong result = 0;
            lock (handles)
            {
                result = (ulong)handles[handle].FileLength;
            }
            return await Task.FromResult(result);
            //SetPropertyDTO result;
            //if (findResults.ContainsKey( handle ))
            //	result = findResults[handle].Item2;
            //else
            //{
            //	SetPropertyDTO getLengthProperty = new SetPropertyDTO() { Length = 0 };
            //	result = await ClientSetManager.GetSetPropAsync( handle, getLengthProperty );
            //}

            //if (result.Length.HasValue)
            //	return result.Length.Value;

            //throw new Exception( "Failure in GetSetLengthAsync: Set properties does not have a valid value for Length" );
        }


        static public async Task<bool> CloseSetAsync(long handle)
        {
            Debug.Print($"CloseSetAsync(ENTER)");
            // This API is used to close File & Directory sets 
            bool result = true;
            var fsContext = handles[handle];
            try
            {
                if (fsContext != null)
                {
                    Debug.Print($"CloseSetAsync(Root Check) - fileName={fsContext.SetName} Ctx.NTCreateDisposition:{fsContext.NTCreateDisposition}, Ctx.FileID={fsContext.FileID}, IsDirectory={(fsContext.Attributes & (uint)0x10 /*CBFileAttributes.Directory*/) != 0}, FileLength:{fsContext.FileLength}");
                    #region NOP when closing root directory
                    if (fsContext.SetName == "/" || fsContext.SetName == "\\")
                        return await Task.FromResult(true);
                    #endregion
                    
                    if (fsContext.OpenCount == 1)
                    {
                        Debug.Print($"CloseSetAsync(Actual 'final' close begin)");
                        // If we make it here we are closing a file.  If it is a "create" file then we need to call into the API
                        if (fsContext.NTCreateDisposition == 2 /*Create*/)
                        {
                            Debug.Print($"CloseSetAsync(Create 'final' close causing propagation to all nodes)");
                            var operation = new FILECLOSE_Operation(fsContext.OwnerID, VirtualDiskID, VolumeID, fsContext.SetName, fsContext.FileID, fsContext.CreateTime,
                                            fsContext.LastAccessTime, fsContext.LastWriteTime, fsContext.ChangeTime, fsContext.Attributes, fsContext.EventOrigin, (ulong)fsContext.FileLength);
                            var opResult = await unoSysApi.VirtualDiskVolumeMetaDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                        }
						else
						{
                            Debug.Print($"CloseSetAsync(Local-only 'final' close)");
                        }
                        #region Remove the handle from the handles collection
                        UnRegisterFileStreamContextByHandle( handle);
                        #endregion
                        Debug.Print($"CloseSetAsync(Actual 'final' close end)");
                    }
                    else
                    {
                        Debug.Print($"CloseSetAsync(Decrement 'virtual' close only)");
                        fsContext.DecrementCounter();
                    }
                }
				else
				{
                    Debug.Print($"CloseSetAsync(No FileStreamContext found for handle:{handle})");
                }
            }
            catch (Exception ex)
            {
				// NOP - Best effort
				result = false;
				Debug.Print($"API.CloseSetAsync(ERROR) - {ex}");
				//throw;
            }
            Debug.Print($"CloseSetAsync(LEAVE)");
            return result;
        }


        static public async Task<int> WriteSetAsync(long handle, byte[] bufferToWrite, int bufferOffset, int numBytesToWrite, ulong deviceOffsetToWriteTo)
        {
            int bytesWritten = 0;
            try
            {
				FileStreamContext Ctx = handles[handle];
                if (Ctx.NTCreateDisposition != 2 /*CREATE*/)
                {
					throw new UnoSysConflictException("WORM (Write Once Read Many) sematics violated - can only write to a file in 'Create' mode.");
                }
                Ctx.LastWriteTime = DateTime.UtcNow;
                var operation = new VolumeDataOperation(VirtualDiskID, VolumeID, Ctx.FileID, VolumeDataOperationType.FILE_WRITE,
                                           deviceOffsetToWriteTo, Convert.ToUInt32(numBytesToWrite), Convert.ToUInt64(Ctx.FileLength), bufferToWrite);
                var opResult = await unoSysApi.VirtualDiskVolumeDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                VolumeDataOperation opAttributes = JsonSerializer.Deserialize<VolumeDataOperation>(opResult)!;
                bytesWritten = (int)opAttributes.ByteCount;
                Ctx.FileLength = opAttributes.FileLength;
                //Debug.Print($"API.WriteSetAsync() - bufferOffset:{bufferOffset}, numBytesToWrite:{numBytesToWrite}, BufferSize:{bufferToWrite.Length}, deviceOffsetToWriteTo:{deviceOffsetToWriteTo}, FileLength={Ctx.FileLength}");
            }
            catch (Exception ex)
            {
                Debug.Print($"API.WriteSetAsync(ERROR) - {ex}");
				throw;
            }
			return bytesWritten;
        }

        static public async Task<int> ReadSetAsync(long handle, byte[] bufferToFill, int bufferOffset, int numBytesToRead, ulong deviceOffsetToReadFrom)
        {
            Debug.Print($"API.ReadSetAsync(ENTER)");
            int bytesRead = 0;
            try
            {
                FileStreamContext Ctx = handles[handle];
                var operation = new VolumeDataOperation(VirtualDiskID, VolumeID, Ctx.FileID, VolumeDataOperationType.FILE_READ,
                                            Convert.ToUInt64(deviceOffsetToReadFrom), Convert.ToUInt32(numBytesToRead), Convert.ToUInt64(Ctx.FileLength));
                var opResult = await unoSysApi.VirtualDiskVolumeDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                VolumeDataOperation operationResponse = JsonSerializer.Deserialize<VolumeDataOperation>(opResult)!;
				Buffer.BlockCopy(operationResponse!.ByteBuffer, 0, bufferToFill, bufferOffset, (int)operationResponse.ByteCount);
                bytesRead = (int)operationResponse.ByteCount;
				if( bytesRead != numBytesToRead)
				{
					Debug.Print("PI.ReadSetAsync() - 88888888888888888888888888888888888888888888888888888888888888888888888888888");
				}
                Debug.Print($"API.ReadSetAsync() - bufferOffset:{bufferOffset}, numBytesToRead:{numBytesToRead}, BufferSize:{bufferToFill.Length}, deviceOffsetToWriteTo:{deviceOffsetToReadFrom}, FileLength={Ctx.FileLength}");
            }
            catch (Exception ex)
            {
                Debug.Print($"API.ReadSetAsync(ERROR) - {ex}");
                throw;
            }
            Debug.Print($"API.ReadSetAsync(LEAVE)");
            return bytesRead;
        }
        #endregion

        #region Not Yet Implemented
        /// <summary>
        /// Initial implementation
        /// </summary>
        /// <param name="setMask"></param>
        /// <returns></returns>
        static public async Task<long> FindSetAsync( string setMask )
		{
			throw new NotImplementedException();
            // Call FindSetAsync to return a handle to an enumeration of find results
            var foundResult = await ClientSetManager.FindSetAsync( setMask );

			// If that's given us a valid handle, then we add it to our collection of locally managed handles
			var handle = foundResult.Item1;
			if (handle > 0)
				findResults.Add( handle, new Tuple<string, SetPropertyDTO>( foundResult.Item2, foundResult.Item3 ) );
			return handle;
		}

		static public async Task<bool> FindNextSetAsync( long handle )
		{
            throw new NotImplementedException();
            bool result = false;
			if (findResults.ContainsKey( handle ))
			{
				// Call FindNextSetAsync to fetch the next result from our enumeration of find results
				var foundResult = await ClientSetManager.FindNextSetAsync( handle );
				findResults[handle] = foundResult;
				if (!string.IsNullOrEmpty( foundResult.Item1 ))
					result = true;
			}

			return result;
		}


		static public async Task<bool> DeleteSetAsync( long handle )
		{
            throw new NotImplementedException();
            // NOTE:  This API is used to close File & Directory sets as well as Table Sets as well
            //
            // The handle passed in will cause the call to be routed to either FileSystem ore DatabaseSystem kernel objects respectively

            // The handle is going away (Note: call to DeleteSetAsync() below causes it to be closed) so remove it from the findResults collection if it exists...
            if (findResults.ContainsKey( handle ))
			{
				findResults.Remove( handle );
			}
			APIOperationResponseCode deleteResult = await ClientSetManager.DeleteSetAsync( handle );
			return deleteResult == APIOperationResponseCode.OK;
		}

		static public async Task<string> GetFullPathNameAsync( long handle )
		{
            throw new NotImplementedException();
            if (findResults.ContainsKey( handle ))
				return findResults[handle].Item1;

			return await ClientSetManager.GetFullSetPath( handle );
		}

		static internal async Task<SetPropertyDTO> GetSetPropertiesAsync( long handle )
		{
            throw new NotImplementedException();
            if (findResults.ContainsKey( handle ))
				return findResults[handle].Item2;
			
			SetPropertyDTO getProperties = new SetPropertyDTO() { 
				Attributes = 0, Length = 0, 
				CreationTime = DateTime.MaxValue, 
				LastAccessTime = DateTime.MaxValue, 
				LastWriteTime = DateTime.MaxValue
			};
			var properties = await ClientSetManager.GetSetPropAsync( handle, getProperties );
			if (!properties.Attributes.HasValue)
				throw new Exception( "Failure in GetSetAttributesAsync: Set properties does not have a valid value for Attributes" );
			if (!properties.Length.HasValue)
				throw new Exception( "Failure in GetSetAttributesAsync: Set properties does not have a valid value for Length" );
			if (!properties.CreationTime.HasValue)
				throw new Exception( "Failure in GetSetAttributesAsync: Set properties does not have a valid value for CreationTime" );
			if (!properties.LastAccessTime.HasValue)
				throw new Exception( "Failure in GetSetAttributesAsync: Set properties does not have a valid value for LastAccessTime" );
			if (!properties.LastWriteTime.HasValue)
				throw new Exception( "Failure in GetSetAttributesAsync: Set properties does not have a valid value for LastWriteTime" );

			//FileInfo result = new FileInfo();	// This would be better, but we don't have a Name, and FileInfo is Name based
			return properties;
		}

		static public async Task<uint> GetSetAttributesAsync( long handle )
		{
            await Task.CompletedTask;
			throw new NotImplementedException();
			//SetPropertyDTO result;
			//if (findResults.ContainsKey( handle ))
			//	result = findResults[handle].Item2;
			//else
			//{
			//	SetPropertyDTO getAttributesProperty = new SetPropertyDTO() { Attributes = 0 };
			//	result = await ClientSetManager.GetSetPropAsync( handle, getAttributesProperty );
			//}

			//if (result.Attributes.HasValue)
			//	return result.Attributes.Value;

			throw new Exception( "Failure in GetSetAttributesAsync: Set properties does not have a valid value for Attributes" );
		}

		static public async Task<APIOperationResponseCode> SetSetAttributesAsync( long handle, uint attributes )
		{
            throw new NotImplementedException();
            SetPropertyDTO setAttributesProperty = new SetPropertyDTO() { Attributes = attributes };
			var result = await ClientSetManager.SetSetPropAsync( handle, setAttributesProperty );
			return result;
		}

		static public async Task<DateTime> GetSetCreationTimeAsync( long handle )
		{
            throw new NotImplementedException();
            SetPropertyDTO result;
			if (findResults.ContainsKey( handle ))
				result = findResults[handle].Item2;
			else
			{
				SetPropertyDTO getCreationTimeProperty = new SetPropertyDTO() { CreationTime = DateTime.MaxValue };
				result = await ClientSetManager.GetSetPropAsync( handle, getCreationTimeProperty );
			}

			if (result.CreationTime.HasValue)
				return result.CreationTime.Value;

			throw new Exception( "Failure in GetSetCreationTimeAsync: Set properties does not have a valid value for CreationTime" );
		}

		static public async Task<APIOperationResponseCode> SetSetCreationTimeAsync( long handle, DateTime creationTime )
		{
            throw new NotImplementedException();
            SetPropertyDTO setCreationTimeProperty = new SetPropertyDTO() { CreationTime = creationTime };
			var result = await ClientSetManager.SetSetPropAsync( handle, setCreationTimeProperty );
			return result;
		}

		static public async Task<DateTime> GetSetLastAccessTimeAsync( long handle )
		{
            throw new NotImplementedException();
            SetPropertyDTO result;
			if (findResults.ContainsKey( handle ))
				result = findResults[handle].Item2;
			else
			{
				SetPropertyDTO getLastAccessTimeProperty = new SetPropertyDTO() { LastAccessTime = DateTime.MaxValue };
				result = await ClientSetManager.GetSetPropAsync( handle, getLastAccessTimeProperty );
			}

			if (result.LastAccessTime.HasValue)
				return result.LastAccessTime.Value;

			throw new Exception( "Failure in GetSetLastAccessTimeAsync: Set properties does not have a valid value for LastAccessTime" );
		}

		static public async Task<APIOperationResponseCode> SetSetLastAccessTimeAsync( long handle, DateTime lastAccessTime )
		{
            throw new NotImplementedException();
            SetPropertyDTO setLastAccessTimeProperty = new SetPropertyDTO() { LastAccessTime = lastAccessTime };
			var result = await ClientSetManager.SetSetPropAsync( handle, setLastAccessTimeProperty );
			return result;
		}

		static public async Task<DateTime> GetSetLastWriteTimeAsync( long handle )
		{
            throw new NotImplementedException();
            SetPropertyDTO result;
			if (findResults.ContainsKey( handle ))
				result = findResults[handle].Item2;
			else
			{
				SetPropertyDTO getLastWriteTimeProperty = new SetPropertyDTO() { LastWriteTime = DateTime.MaxValue };
				result = await ClientSetManager.GetSetPropAsync( handle, getLastWriteTimeProperty );
			}

			if (result.LastWriteTime.HasValue)
				return result.LastWriteTime.Value;

			throw new Exception( "Failure in GetSetLastWriteTimeAsync: Set properties does not have a valid value for LastWriteTime" );
		}

		static public async Task<APIOperationResponseCode> SetSetLastWriteTimeAsync( long handle, DateTime lastWriteTime )
		{
            throw new NotImplementedException();
            SetPropertyDTO setLastWriteTimeProperty = new SetPropertyDTO() { LastWriteTime = lastWriteTime };
			var result = await ClientSetManager.SetSetPropAsync( handle, setLastWriteTimeProperty );
			return result;
		}

		

		static public async Task<APIOperationResponseCode> SetSetLengthAsync( long handle, ulong length )
		{
            throw new NotImplementedException();
            SetPropertyDTO setLengthProperty = new SetPropertyDTO() { Length = length };
			var result = await ClientSetManager.SetSetPropAsync( handle, setLengthProperty );
			return result;
		}




        #endregion
        #endregion

        #region Database API
        /// <summary>
        /// Creates a Database
        /// </summary>
        /// <param name="dbName">The name of the Database to create</param>
        /// <returns>A handle to the opened Database</returns>
        static public async Task CreateDatabaseAsync(string dbName)
		{
			try
			{
				#region Validate Parameters
				if (dbName.Length > MAX_FILENAME_LENGTH)
				{
					throw new UnoSysArgumentException("File name too long.");
				}
				#endregion

				#region Create FileStreamContext
				//FileStreamContext fsContext = new FileStreamContext(dbName);
				//fsContext.NTCreateDisposition = /*CreateNew =*/ 1;
				//fsContext.Attributes = setAttributes;
				//fsContext.FileLength = 0;
				#endregion

				#region Call unoSysApi.VirtualDiskVolumeMetaDataOperationAsync()
				DATABASECREATE_Operation operation = new DATABASECREATE_Operation(UserSessionTokenID, //NOTE: We pass in a userSessionTokenID - OwnerID will be returned
															VirtualDiskID, VolumeID, /*fsContext.FileID*/Guid.NewGuid(),
						dbName, 0,  /*0xFFFF*/0, 0, /*fsContext.NTCreateDisposition*/0, 0);
				var opBase64Result = await unoSysApi.VirtualDiskVolumeMetaDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
				#endregion

				#region Set the FileStreamContext.OwnerID to the OwnerID passed back from the API call 
				var opCreateResult = new FILECREATE_Operation(Convert.FromBase64String(opBase64Result));
				//fsContext.OwnerID = opCreateResult.OwnerID;
				#endregion

				#region Register Context to generate a handle to return to caller
				//RegisterFileStreamContextAndSetName(fsContext, "DB_" + dbName);
				#endregion
			}
			catch (Exception ex)
			{
				Debug.Print($"API.CreateDatabaseAsync(ERROR) - {ex}");
				throw;
			}
			//return  await ClientSetManager.CreateSetAsync( setName, setMode, desiredAccess, setAttributes, shareMode, blockSize );
		}


        /// <summary>
        /// Deletes an existing Database
        /// </summary>
        /// <param name="long">Handle to Dataabase to be closed</param>
        /// <returns>A boolean indicating success (true) or failure (false) of operation</returns>
        static public async Task DeleteDatabaseAsync(string dbName)
        {
            try
            {
				//#region Create FileStreamContext
    //            (FileStreamContext, long) CtxAndHandle = LookupFileStreamContextAndHandleBySetName("DB_" + dbName);
    //            if (CtxAndHandle.Item1 == null)
				//{
				//	throw new UnoSysResourceNotFoundException();
    //            }
    //            #endregion

                #region Call unoSysApi.VirtualDiskVolumeMetaDataOperationAsync()
                DATABASEDELETE_Operation operation = new DATABASEDELETE_Operation(UserSessionTokenID, VirtualDiskID, VolumeID, dbName, true);
                var opBase64Result = await unoSysApi.VirtualDiskVolumeMetaDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                #endregion

                #region Unregister Context 
                //UnRegisterFileStreamContextByHandle(CtxAndHandle.Item2);
				#endregion
            }
            catch (Exception ex)
            {
                Debug.Print($"API.DeleteDatabaseAsync(ERROR) - {ex}");
                throw;
            }
        }

        #endregion


        #region Table API
        // NOTE:  The following DatabaseSystem APIs simply reuse the equivalent base Set (i.e.; File and Directory) APIs
        //
        // DeleteSetAsync()
        // CloseSetAsync()
        static public async Task<HandleWithFieldDefs> CreateTableSetAsync(string tableName, Common.Types.SetSchema setSchema, uint setMode, uint desiredAccess, uint setAttributes, uint shareMode)
        {
            HandleWithFieldDefs handleWithFieldDefs = null!;
            try
            {
                #region Validate Parameters
                string[] tableNameParts = tableName.Split(new char[] { '/', '\\' });
                if (tableNameParts.Length != 2 || string.IsNullOrEmpty(tableNameParts[0]) || string.IsNullOrEmpty(tableNameParts[1]))
                {
                    throw new UnoSysArgumentException("Invalid Table Name.");
                }
                if (tableName.Length > MAX_FILENAME_LENGTH)
                {
                    throw new UnoSysArgumentException("Table Name too long.");
                }
                #endregion

                #region Create FileStreamContext
                FileStreamContext fsContext = new FileStreamContext(tableName);
                fsContext.NTCreateDisposition = (int)setMode;
                fsContext.Attributes = (int)setAttributes;
                fsContext.FileLength = 0;
                #endregion

                #region Call unoSysApi.VirtualDiskVolumeMetaDataOperationAsync()
                TABLECREATE_Operation operation = new TABLECREATE_Operation(UserSessionTokenID, //NOTE: We pass in a userSessionTokenID - OwnerID will be returned
                                                            VirtualDiskID, VolumeID, /*fsContext.FileID*/Guid.NewGuid(),
                        tableNameParts[0], tableNameParts[1], setSchema, (int)desiredAccess, (int)setAttributes, (int)shareMode,  /*fsContext.NTCreateDisposition:CreateNew*/1, (int)desiredAccess);
                var opBase64Result = await unoSysApi.VirtualDiskVolumeMetaDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                #endregion

                #region Set the FileStreamContext.OwnerID to the OwnerID passed back from the API call 
                var opCreateResult = new TABLECREATE_Operation(Convert.FromBase64String(opBase64Result));
                fsContext.OwnerID = opCreateResult.OwnerID;
                #endregion

                #region Register Context to generate a handle to return to caller
                var handle = RegisterFileStreamContextAndSetName(fsContext, tableName);
                handleWithFieldDefs = new HandleWithFieldDefs(handle, 0, 0, false, setSchema, APIOperationResponseCode.OK);
                #endregion
            }
            catch (Exception ex)
            {
                Debug.Print($"API.CreateTableSetAsync(ERROR) - {ex}");
                throw;
            }
            return handleWithFieldDefs;
            //HandleWithFieldDefs handleWithFieldDefs = await ClientSetManager.CreateTableSetAsync( setName, setSchema, setMode, desiredAccess, setAttributes, shareMode );
            //if (handleWithFieldDefs.responseCode != APIOperationResponseCode.OK )
            //{
            //	throw APIOperationToException( handleWithFieldDefs.responseCode );
            //}
            //return handleWithFieldDefs;
        }

       
        static public async Task<HandleWithFieldDefs> OpenTableSetAsync(string tableName, uint setMode, uint desiredAccess, uint setAttributes, uint shareMode )
		{
            //Debug.Print($"CBOpenFile(ENTER) - fileName={e.FileName} NTCreateDisposition={e.NTCreateDisposition}");
            HandleWithFieldDefs handleWithFieldDefs = null!;
            try
            {
                #region Validate Parameters
                string[] tableNameParts = tableName.Split(new char[] { '/', '\\' });
                if (tableNameParts.Length != 2 || string.IsNullOrEmpty(tableNameParts[0]) || string.IsNullOrEmpty(tableNameParts[1]))
                {
                    throw new UnoSysArgumentException("Invalid Table Name.");
                }
                if (tableName.Length > MAX_FILENAME_LENGTH)
                {
                    throw new UnoSysArgumentException("Table Name too long.");
                }
                #endregion

                (FileStreamContext, long) CtxAndHandle = LookupFileStreamContextAndHandleBySetName(tableName);
                if (CtxAndHandle.Item1 != null)
                {
                    CtxAndHandle.Item1.IncrementCounter();
                    var handle = CtxAndHandle.Item2;
                }
                else
                {
                    FileStreamContext Ctx = new FileStreamContext(tableName);
                    Ctx.NTCreateDisposition = (int)setMode;

                    #region Call unoSysApi.VirtualDiskVolumeMetaDataOperationAsync()
                    TABLEOPEN_Operation operation = new TABLEOPEN_Operation(UserSessionTokenID, VirtualDiskID, VolumeID, tableNameParts[0], tableNameParts[1], null! );
                    var opBase64Result = await unoSysApi.VirtualDiskVolumeMetaDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                    try
                    {
                        TABLECLOSE_Operation tcOperation = new TABLECLOSE_Operation(Convert.FromBase64String(opBase64Result)); 
                        //Ctx.CreateTime = opAttributes.CreateTime;
                        //Ctx.LastAccessTime = opAttributes.LastAccessTime;
                        //Ctx.LastWriteTime = opAttributes.LastWriteTime;
                        Ctx.OwnerID = tcOperation.OwnerID;
                        Ctx.Attributes = tcOperation.Attributes;
                        Ctx.FileLength = tcOperation.RecordCount;
                        Ctx.FileID = tcOperation.FileID;
                        Ctx.TableSchema = tcOperation.TableSchema;
                        //Ctx.EventOrigin = opAttributes.EventOrigin;

                        #region Register Context to generate a handle to return to caller
                        var handle = RegisterFileStreamContextAndSetName(Ctx, tableName);
                        handleWithFieldDefs = new HandleWithFieldDefs(handle, 1, tcOperation.RecordCount, false, Ctx.TableSchema, APIOperationResponseCode.OK);
                        #endregion
                    }
                    catch (Exception ex1)
                    {
                        Debug.Print($"API.OpenTableSetAsync(ERROR 1) {ex1.Message}");
                        // NOP
                    }
                    //Debug.Print($"API.OpenSetAsync(A) - FileID={Ctx.FileID} F={setName}, Len={Ctx.FileLength}, IsDir={(Ctx.Attributes & (uint)0x10 /*CBFileAttributes.Directory*/) != 0}");
                    #endregion
                }
                //if (Ctx != null && Ctx.hStream != null)
                //{ 
                //    Debug.Print($"CBOpenFile(B) - fileName={Ctx.hStream.Name} NTCreateDisposition={e.NTCreateDisposition}");
                //}
                //else
                //{
                //    Debug.Print($"CBOpenFile(C) - *** hStream not defined *** -  fileName={e.FileName} NTCreateDisposition={e.NTCreateDisposition}");
                //}
                //e.FileContext = GCHandle.ToIntPtr(GCHandle.Alloc(Ctx));
            }
            catch (Exception ex)
            {
                Debug.Print($"API.OpenTableSetAsync(ERROR2) - {ex}");
                throw;
            }
            return handleWithFieldDefs;
            //HandleWithFieldDefs handleWithFieldDefs = await ClientSetManager.OpenTableSetAsync(tableName, setMode, desiredAccess, setAttributes, shareMode, 4 * 1024 /*Default block size*/ );
            //if (handleWithFieldDefs.responseCode != APIOperationResponseCode.OK)
            //{
            //	throw APIOperationToException( handleWithFieldDefs.responseCode );
            //}
            //return handleWithFieldDefs;
        }

        static public async Task<bool> CloseTableSetAsync(HandleWithFieldDefs handleWithFieldDefs)
        {
            Debug.Print($"CloseTableSetAsync(ENTER)");
            // This API is used to close Table Sets
            bool result = true;
            var fsContext = handles[handleWithFieldDefs.Handle];
            try
            {
                if (fsContext != null)
                {
                    Debug.Print($"CloseTableSetAsync(Root Check) - fileName={fsContext.SetName} Ctx.NTCreateDisposition:{fsContext.NTCreateDisposition}, Ctx.FileID={fsContext.FileID}, IsDirectory={(fsContext.Attributes & (uint)0x10 /*CBFileAttributes.Directory*/) != 0}, FileLength:{fsContext.FileLength}");
                    if (fsContext.OpenCount == 1)
                    {
                        Debug.Print($"CloseTableSetAsync(Actual 'final' close begin)");
                        // If we make it here we are closing a Table.  If it is a "create" Table then we need to call into the API
                        if (fsContext.NTCreateDisposition == 2 /*Create*/)
                        {
                            Debug.Print($"CloseTableSetAsync(Create 'final' close causing propagation to all nodes)");
                            string[] tableNameParts = fsContext.SetName.Split(new char[] { '/', '\\' });
                            var operation = new TABLECLOSE_Operation(fsContext.OwnerID, VirtualDiskID, VolumeID, tableNameParts[0], tableNameParts[1], handleWithFieldDefs.TableSchema, fsContext.FileID, fsContext.CreateTime,
                                            fsContext.LastAccessTime, fsContext.LastWriteTime, fsContext.ChangeTime, fsContext.Attributes, fsContext.EventOrigin, (ulong)fsContext.FileLength);
                            var x = new TABLECLOSE_Operation(operation.AsBytes());
                            var opResult = await unoSysApi.VirtualDiskVolumeMetaDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);

                        }
                        else
                        {
                            Debug.Print($"CloseTableSetAsync(Local-only 'final' close)");
                        }
                        #region Remove the handle from the handles collection
                        UnRegisterFileStreamContextByHandle(handleWithFieldDefs.Handle);
                        #endregion
                        Debug.Print($"CloseTableSetAsync(Actual 'final' close end)");
                    }
                    else
                    {
                        Debug.Print($"CloseTableSetAsync(Decrement 'virtual' close only)");
                        fsContext.DecrementCounter();
                    }
                }
                else
                {
                    Debug.Print($"CloseTableSetAsync(No FileStreamContext found for handle:{handleWithFieldDefs.Handle})");
                }
            }
            catch (Exception ex)
            {
                // NOP - Best effort
                result = false;
                Debug.Print($"API.CloseTableSetAsync(ERROR) - {ex}");
                //throw;
            }
            Debug.Print($"CloseSetAsync(LEAVE)");
            return result;
        }


        static public async Task<OrdinalResponse> WriteTableSetMemberAsync(TypedSetMember setMember, HandleWithFieldDefs handleWithFieldDefs, bool isNew)
        {
            OrdinalResponse result = null!;
            int bytesWritten = 0;
            try
            {
                FileStreamContext Ctx = handles[handleWithFieldDefs.Handle];
                if (Ctx.NTCreateDisposition != 2 /*CREATE*/)
                {
                    throw new UnoSysConflictException("WORM (Write Once Read Many) sematics violated - can only write to a file in 'Create' mode.");
                }
                Ctx.LastWriteTime = DateTime.UtcNow;
                Ctx.FileLength = Ctx.FileLength + (isNew ? 1UL : 0UL);  // NOTE:  In the Context of a Table this is the number of records written, not the byte length of the file - +1 if a new record is being added
                int byteCount = 0;
                byte[] bufferToWrite = setMember.GetRecordBytes(ref byteCount);
                var operation = new VolumeDataOperation(VirtualDiskID, VolumeID, Ctx.FileID, VolumeDataOperationType.TABLE_WRITE,
                                           0, Convert.ToUInt32(byteCount), Convert.ToUInt64(Ctx.FileLength), bufferToWrite);
                var opResult = await unoSysApi.VirtualDiskVolumeDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                VolumeDataOperation opAttributes = JsonSerializer.Deserialize<VolumeDataOperation>(opResult)!;
                bytesWritten = (int)opAttributes.ByteCount;
                
                result = new OrdinalResponse{
                    ResponseCode = APIOperationResponseCode.OK,
                    Ordinal = Ctx.FileLength,  // FileLength is the number of records in the table
                    LastOrdinal = Ctx.FileLength, // FileLength is the number of records in the table
                    IsDeleted = false, 
                    IsBoT = false, 
                    IsEoT = false, 
                    IsDirty = false
                    };
            }
            catch (Exception ex)
            {
                Debug.Print($"API.WriteTableSetMemberAsync(ERROR) - {ex}");
                throw;
            }
            return result;
            //OrdinalResponse result = await ClientSetManager.WriteTableRecordAsync(handle);
            //if (result.ResponseCode != APIOperationResponseCode.OK)
            //{
            //    throw APIOperationToException(result.ResponseCode);
            //}

            //return result;
        }



        static public async Task WriteTableOrderAsync(long handle, ulong orderOrdinal, byte[] bufferToWrite)
        {
            try
            {
                FileStreamContext Ctx = handles[handle];
                if (Ctx.NTCreateDisposition != 2 /*CREATE*/)
                {
                    throw new UnoSysConflictException("WORM (Write Once Read Many) sematics violated - can only write Order files if Table in 'Create' mode.");
                }
                var fileName = Ctx.FileID.ToString("N").ToUpper() + "_" + orderOrdinal.ToString() + ".ord"; // NOTE:  This is the file name used to store the Order
                using (var wcfs = new Unosys.SDK.FileStream(fileName, Unosys.SDK.FileMode.Create, Unosys.SDK.FileAccess.Write, Unosys.SDK.FileShare.None))
                {
                    MemoryStream memoryStream = new MemoryStream(bufferToWrite);
                    await memoryStream.CopyToAsync(wcfs).ConfigureAwait(false);
                    wcfs.Flush();
                    wcfs.Close();
                }
                //Console.WriteLine($"API.WriteTableOrderAsync() - bufferToWrite.Length:{bufferToWrite.Length}, FileName:{fileName}");
                //var operation = new VolumeDataOperation(VirtualDiskID, VolumeID, Ctx.FileID, VolumeDataOperationType.ORDER_WRITE,
                //                           0, Convert.ToUInt32(bufferToWrite.Length), Convert.ToUInt64(orderOrdinal), bufferToWrite);
                //var opResult = await unoSysApi.VirtualDiskVolumeDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                //VolumeDataOperation opAttributes = JsonSerializer.Deserialize<VolumeDataOperation>(opResult)!;
                //bytesWritten = (int)opAttributes.ByteCount;
            }
            catch (Exception ex)
            {
                Debug.Print($"API.WriteTableOrderAsync(ERROR) - {ex}");
                throw;
            }
        }


        static public async Task<byte[]> ReadTableOrderAsync(long handle, ulong orderOrdinal)
        {
            byte[] orderBytes = null!;
            try
            {
                
                FileStreamContext Ctx = handles[handle];
                var fileName = Ctx.FileID.ToString("N").ToUpper() + "_" + orderOrdinal.ToString() + ".ord"; // NOTE:  This is the file name used to store the Order
                using (var wcfs = new Unosys.SDK.FileStream(fileName, Unosys.SDK.FileMode.Open, Unosys.SDK.FileAccess.Read, Unosys.SDK.FileShare.None))
                {
                    orderBytes = new byte[wcfs.Length];
                    await wcfs.ReadAsync(orderBytes, 0, orderBytes.Length).ConfigureAwait(false);
                    await wcfs.FlushAsync().ConfigureAwait(false);
                    wcfs.Close();
                }
                //Console.WriteLine($"API.ReadTableOrderAsync() - orderBytes.Length:{orderBytes.Length}, FileName:{fileName}");
                //var operation = new VolumeDataOperation(VirtualDiskID, VolumeID, Ctx.FileID, VolumeDataOperationType.ORDER_WRITE,
                //                           0, Convert.ToUInt32(bufferToWrite.Length), Convert.ToUInt64(orderOrdinal), bufferToWrite);
                //var opResult = await unoSysApi.VirtualDiskVolumeDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                //VolumeDataOperation opAttributes = JsonSerializer.Deserialize<VolumeDataOperation>(opResult)!;
                //bytesWritten = (int)opAttributes.ByteCount;
            }
            catch (Exception ex)
            {
                Debug.Print($"API.WriteTableOrderAsync(ERROR) - {ex}");
                throw;
            }
            return orderBytes;
        }

        static public async Task<OrdinalResponse> ReadTableSetMemberAsync(TypedSetMember setMember, HandleWithFieldDefs handleWithFieldDefs)
        {
            OrdinalResponse result = null!;
            try
            {
                FileStreamContext Ctx = handles[handleWithFieldDefs.Handle];
                Ctx.LastAccessTime = DateTime.UtcNow;
                var operation = new VolumeDataOperation(VirtualDiskID, VolumeID, Ctx.FileID, VolumeDataOperationType.TABLE_READ,
                                            //0, Convert.ToUInt32(setMember.SetMemberSize), Convert.ToUInt64(Ctx.FileLength));
                                            0, Convert.ToUInt32(setMember.SetMemberSize), Convert.ToUInt64(setMember.Ordinal));
                var opResult = await unoSysApi.VirtualDiskVolumeDataOperationAsync(UserSessionToken, operation.AsBase64String()).ConfigureAwait(false);
                VolumeDataOperation operationResponse = JsonSerializer.Deserialize<VolumeDataOperation>(opResult)!;
                setMember.PutRecordBytes(operationResponse!.ByteBuffer);
                
                Ctx.FileLength = operationResponse.FileLength;  // NOTE:  In the Context of a Table this is the number of records written, not the byte length of the file 
                result = new OrdinalResponse
                {
                    ResponseCode = APIOperationResponseCode.OK,
                    Ordinal = setMember.Ordinal,  
                    LastOrdinal = Ctx.FileLength, // FileLength is the number of records in the table
                    IsDeleted = false,
                    IsBoT = false,
                    IsEoT = false,
                    IsDirty = false
                };
            }
            catch (Exception ex)
            {
                Debug.Print($"API.ReadTableSetMemberAsync(ERROR) - {ex}");
                throw;
            }
            return result;
            //OrdinalResponse result = await ClientSetManager.WriteTableRecordAsync(handle);
            //if (result.ResponseCode != APIOperationResponseCode.OK)
            //{
            //    throw APIOperationToException(result.ResponseCode);
            //}

            //return result;
        }


        static public OrdinalResponse TableOrderMoveNext(TypedSetMember ts, HandleWithFieldDefs handleWithFieldDefs, int currentOrder, SortedList<string, List<ulong>> keyValuePairs, int membersToMove, ref string currentOrderKey, ref int currentOrderRecordOrdinalPos)
        {
            OrdinalResponse result = null!;
            var endOfSet = handleWithFieldDefs.LastRecordOrdinal;
            var setMemberOrdinal = ts.Ordinal;
            ulong adjustment = 0;
            bool isBoT = false;
            bool isEoT = false;
            bool readNewRecord = false;            

            #region Determine new setMemberOrdinal
            if (membersToMove < 0)
            {
                throw new NotSupportedException("TableOrderMoveNext() currently only supports moving one record at a time in the forward direction.");
                // Moving backwards...
                //adjustment = Convert.ToUInt64(Math.Abs(membersToMove));
                //if (setMemberOrdinal == 0) // Are we on EoT?
                //{
                //    // We are backing up from EoT so see if we have room
                //    if (adjustment > endOfSet)
                //    {
                //        // Can't go backwards past the first record
                //        // New position is BOT
                //        // %TODO% no support for DELETED reocords...
                //        setMemberOrdinal = (endOfSet == 0 ? 0UL : 1UL);
                //        isBoT = true;
                //    }
                //    else
                //    {
                //        //%TODO%:  This assumes no DELETED records
                //        setMemberOrdinal = endOfSet + 1 - adjustment;
                //    }
                //}
                //else  // Not currently on EoT but on a valid record
                //{
                //    if (adjustment >= setMemberOrdinal)
                //    {
                //        // New position is BOT
                //        setMemberOrdinal = 1;
                //        isBoT = true;
                //    }
                //    else
                //    {
                //        //%TODO%:  This assumes no DELETED records
                //        setMemberOrdinal = setMemberOrdinal - adjustment;
                //    }
                //}
            }
            else
            {
                // Moving forwards
                adjustment = Convert.ToUInt64(membersToMove);
                if( adjustment != 1)
                    throw new NotSupportedException("TableOrderMoveNext() currently only supports moving one record at a time in the forward direction.");
                // start postion
                var recordOrdList = keyValuePairs[currentOrderKey];
                if (currentOrderRecordOrdinalPos + 1 < recordOrdList.Count)
                {
                    setMemberOrdinal = recordOrdList[++currentOrderRecordOrdinalPos];
                }
                else
                {
                    var nextKey = keyValuePairs.Keys.IndexOf(currentOrderKey) + 1;
                    if (nextKey < keyValuePairs.Count)
                    {
                        currentOrderKey = keyValuePairs.Keys[nextKey];
                        currentOrderRecordOrdinalPos = 0;
                        setMemberOrdinal = keyValuePairs[currentOrderKey][currentOrderRecordOrdinalPos];
                    }
                    else
                    {
                        // No more records in the order
                        setMemberOrdinal = 0;
                        isEoT = true;
                    }
                }
            }
            #endregion
            ts.Ordinal = setMemberOrdinal;
            if (setMemberOrdinal > 0)
            {
                // Load the record we need
                readNewRecord = true;
                ts.IsDirty = false;
            }
            //bool isDeleted = false;
            if (readNewRecord)
            {
                result = ReadTableSetMemberAsync(ts, handleWithFieldDefs).Result;
                //// NOTE:  Cannot do this await in the lock() above
                ////blkResponse = await SetManager.ReadTableSetMemberAsync( pSet, recordBuffer, setMemberOrdinal, -1 ); // -1 indicates that all bytes in the block file should be read (i.e.; we don't know how many bytes to actually read so read them all (but must be less than buffer size)
                //blkResponse = await SetManager.ReadTableSetMemberAsync(pSet, recordBuffer, setMemberOrdinal, recordBuffer.Length * -1); // -1 indicates that all bytes in the block file should be read (i.e.; we don't know how many bytes to actually read so read them all (but must be less than buffer size)
                //if (blkResponse.ByteCount < 0)
                //{
                //    // Attempting to load the required record failed and we are in an undefined state 
                //    throw new InvalidOperationException("Record not found.");
                //}
                //isDeleted = blkResponse.IsDeleted;
            }
            else
            {
                ts.ReInitializeRecord();  // simply initialize the empty EoT record - there is nothing to read 
                result = new OrdinalResponse
                {
                    ResponseCode = APIOperationResponseCode.OK,
                    Ordinal = setMemberOrdinal,
                    LastOrdinal = handleWithFieldDefs.LastRecordOrdinal, // FileLength is the number of records in the table
                    IsDeleted = false, // isDeleted,
                    IsBoT = isBoT,
                    IsEoT = isEoT,
                    IsDirty = false
                };
            }
            return result;
            //OrdinalResponse result = await ClientSetManager.MoveNextRecordAsync(handle, recordsToMove);
            //if (result.ResponseCode != APIOperationResponseCode.OK)
            //{
            //    throw APIOperationToException(result.ResponseCode);
            //}

            //return result;
        }


        //static public OrdinalResponse TableOrderGoTop(TypedSetMember ts, HandleWithFieldDefs handleWithFieldDefs, int currentOrder, SortedList<string, List<ulong>> keyValuePairs, ref string currentOrderKey, ref int currentOrderRecordOrdinalPos)
        //{
        //    OrdinalResponse result = null!;
        //    var endOfSet = handleWithFieldDefs.LastRecordOrdinal;
        //    var setMemberOrdinal = ts.Ordinal;
        //    ulong adjustment = 0;
        //    bool isBoT = false;
        //    bool isEoT = false;
        //    bool readNewRecord = false;

        //    #region Determine new setMemberOrdinal
            
                
        //    // start postion
        //    var kvp = keyValuePairs.ElementAt(0);
        //    currentOrderKey = kvp.Key;
        //    currentOrderRecordOrdinalPos = 0 -;
            
        //    #endregion
        //    ts.Ordinal = setMemberOrdinal;
        //    if (setMemberOrdinal > 0)
        //    {
        //        // Load the record we need
        //        readNewRecord = true;
        //        ts.IsDirty = false;
        //    }
        //    //bool isDeleted = false;
        //    if (readNewRecord)
        //    {
        //        result = ReadTableSetMemberAsync(ts, handleWithFieldDefs).Result;
        //        //// NOTE:  Cannot do this await in the lock() above
        //        ////blkResponse = await SetManager.ReadTableSetMemberAsync( pSet, recordBuffer, setMemberOrdinal, -1 ); // -1 indicates that all bytes in the block file should be read (i.e.; we don't know how many bytes to actually read so read them all (but must be less than buffer size)
        //        //blkResponse = await SetManager.ReadTableSetMemberAsync(pSet, recordBuffer, setMemberOrdinal, recordBuffer.Length * -1); // -1 indicates that all bytes in the block file should be read (i.e.; we don't know how many bytes to actually read so read them all (but must be less than buffer size)
        //        //if (blkResponse.ByteCount < 0)
        //        //{
        //        //    // Attempting to load the required record failed and we are in an undefined state 
        //        //    throw new InvalidOperationException("Record not found.");
        //        //}
        //        //isDeleted = blkResponse.IsDeleted;
        //    }
        //    else
        //    {
        //        ts.ReInitializeRecord();  // simply initialize the empty EoT record - there is nothing to read 
        //        result = new OrdinalResponse
        //        {
        //            ResponseCode = APIOperationResponseCode.OK,
        //            Ordinal = setMemberOrdinal,
        //            LastOrdinal = handleWithFieldDefs.LastRecordOrdinal, // FileLength is the number of records in the table
        //            IsDeleted = false, // isDeleted,
        //            IsBoT = isBoT,
        //            IsEoT = isEoT,
        //            IsDirty = false
        //        };
        //    }
        //    return result;
        //    //OrdinalResponse result = await ClientSetManager.MoveNextRecordAsync(handle, recordsToMove);
        //    //if (result.ResponseCode != APIOperationResponseCode.OK)
        //    //{
        //    //    throw APIOperationToException(result.ResponseCode);
        //    //}

        //    //return result;
        //}


        static public async Task<OrdinalResponse> TableMoveNextAsync(TypedSetMember ts, HandleWithFieldDefs handleWithFieldDefs, int membersToMove)
        {
            OrdinalResponse result = null!;
            var endOfSet = handleWithFieldDefs.LastRecordOrdinal; 
            var setMemberOrdinal = ts.Ordinal;
            ulong adjustment = 0;
            bool isBoT = false;
            bool isEoT = false;
            bool readNewRecord = false;
            #region Determine new setMemberOrdinal
            if (membersToMove < 0)
            {
                // Moving backwards...
                adjustment = Convert.ToUInt64(Math.Abs(membersToMove));
                if (setMemberOrdinal == 0) // Are we on EoT?
                {
                    // We are backing up from EoT so see if we have room
                    if (adjustment > endOfSet)
                    {
                        // Can't go backwards past the first record
                        // New position is BOT
                        // %TODO% no support for DELETED reocords...
                        setMemberOrdinal = (endOfSet == 0 ? 0UL : 1UL);
                        isBoT = true;
                    }
                    else
                    {
                        //%TODO%:  This assumes no DELETED records
                        setMemberOrdinal = endOfSet + 1 - adjustment;
                    }
                }
                else  // Not currently on EoT but on a valid record
                {
                    if (adjustment >= setMemberOrdinal)
                    {
                        // New position is BOT
                        setMemberOrdinal = 1;
                        isBoT = true;
                    }
                    else
                    {
                        //%TODO%:  This assumes no DELETED records
                        setMemberOrdinal = setMemberOrdinal - adjustment;
                    }
                }
            }
            else 
            {
                // Moving forwards
                adjustment = Convert.ToUInt64(membersToMove);
                if (adjustment > endOfSet - setMemberOrdinal)
                {
                    // New position is EOT
                    setMemberOrdinal = 0;
                    isEoT = true;
                }
                else
                {
                    //%TODO%:  This assumes no DELETED records
                    setMemberOrdinal = setMemberOrdinal + adjustment;
                }
            }
            #endregion
            ts.Ordinal = setMemberOrdinal;
            if (setMemberOrdinal > 0)
            {
                // Load the record we need
                //recordBuffer = ts.SetMemberBuffer;
                readNewRecord = true;
                ts.IsDirty = false;
            }
            //bool isDeleted = false;
            if (readNewRecord)
            {
                result = await ReadTableSetMemberAsync(ts, handleWithFieldDefs).ConfigureAwait(false);
                //// NOTE:  Cannot do this await in the lock() above
                ////blkResponse = await SetManager.ReadTableSetMemberAsync( pSet, recordBuffer, setMemberOrdinal, -1 ); // -1 indicates that all bytes in the block file should be read (i.e.; we don't know how many bytes to actually read so read them all (but must be less than buffer size)
                //blkResponse = await SetManager.ReadTableSetMemberAsync(pSet, recordBuffer, setMemberOrdinal, recordBuffer.Length * -1); // -1 indicates that all bytes in the block file should be read (i.e.; we don't know how many bytes to actually read so read them all (but must be less than buffer size)
                //if (blkResponse.ByteCount < 0)
                //{
                //    // Attempting to load the required record failed and we are in an undefined state 
                //    throw new InvalidOperationException("Record not found.");
                //}
                //isDeleted = blkResponse.IsDeleted;
            }
            else
            {
                ts.ReInitializeRecord();  // simply initialize the empty EoT record - there is nothing to read 
                result = new OrdinalResponse
                {
                    ResponseCode = APIOperationResponseCode.OK,
                    Ordinal = setMemberOrdinal,
                    LastOrdinal = handleWithFieldDefs.LastRecordOrdinal, // FileLength is the number of records in the table
                    IsDeleted = false, // isDeleted,
                    IsBoT = isBoT,
                    IsEoT = isEoT,
                    IsDirty = false
                };
            }
            return result;
            //OrdinalResponse result = await ClientSetManager.MoveNextRecordAsync(handle, recordsToMove);
            //if (result.ResponseCode != APIOperationResponseCode.OK)
            //{
            //    throw APIOperationToException(result.ResponseCode);
            //}

            //return result;
        }


        static public async Task<object> GetTableFieldAsync( long handle, FieldDef fieldDef )
		{
			FieldValue fieldValue = await ClientSetManager.GetTableFieldAsync( handle, fieldDef );
			if (fieldValue.ResponseCode == APIOperationResponseCode.OK)
			{
				return fieldValue.Value;
			}
			else
			{
				// %TODO%:  - convert APIOperationResponseCode to a suitable System.Exception
				throw new System.InvalidOperationException( "Problem accessing field." );
			}
		}


		static public async Task<object> PutTableFieldAsync( long handle, FieldDef fieldDef, object fieldvalue )
		{
			FieldValue fieldValue =  await ClientSetManager.PutTableFieldAsync( handle, fieldDef, fieldvalue );
			if (fieldValue.ResponseCode == APIOperationResponseCode.OK)
			{
				return fieldValue.Value;
			}
			else
			{
				throw APIOperationToException( fieldValue.ResponseCode );
			}
		}



		static public async Task<OrdinalResponse> GoToAsync( long handle, ulong recordOrdinal, bool isRollback = false )
		{
			OrdinalResponse result = await ClientSetManager.GoToRecordAsync( handle, recordOrdinal, isRollback );
			if (result.ResponseCode != APIOperationResponseCode.OK)
			{
				throw APIOperationToException( result.ResponseCode );
			}

			return result;
		}

		static public async Task<OrdinalResponse> GoTopAsync( long handle )
		{
			OrdinalResponse result = await ClientSetManager.GoTopRecordAsync( handle );
			if (result.ResponseCode != APIOperationResponseCode.OK)
			{
				throw APIOperationToException( result.ResponseCode );
			}

			return result;
		}

		static public async Task<OrdinalResponse> GoBottomAsync( long handle )
		{
			OrdinalResponse result = await ClientSetManager.GoBottomRecordAsync( handle );
			if (result.ResponseCode != APIOperationResponseCode.OK)
			{
				throw APIOperationToException( result.ResponseCode );
			}

			return result;
		}
        #endregion

        static public Exception APIOperationToException( APIOperationResponseCode responseCode )
		{
			Exception ex = null;
			switch( responseCode )
			{
				case APIOperationResponseCode.INVALID_ARGUMENT:
					ex = new InvalidArgument();
					break;

				case APIOperationResponseCode.INVALID_RECORD_ORDINAL:
					ex = new InvalidRecord();
					break;

				case APIOperationResponseCode.INVALID_AT_EOT:
					ex = new InvalidAtEoT();
					break;

				case APIOperationResponseCode.INVALID_HANDLE:
					ex = new InvalidHandle();
					break;

				case APIOperationResponseCode.NAVIGATION_WITH_PENDING_UPDATES:
					ex = new NavigationWithPendingUpdates();
					break;

				case APIOperationResponseCode.RECORD_NOT_FOUND:
					ex = new RecordNotFound();
					break;

				case APIOperationResponseCode.UNKNOWN_FIELD:
					ex = new UnknownField();
					break;

				default:
					throw new UnknownResponseCode();
			}
			return ex;
		}

        #region Helpers
		private static long RegisterFileStreamContextAndSetName( FileStreamContext fsContext, string setName)
		{
			while (true)
			{
				long handle = Convert.ToInt64( BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(sizeof(uint))) );
				lock (handles)
				{
					#region Ensure not already used before returning
					if (!handles.ContainsKey(handle))
					{
						handles.Add(handle, fsContext);
						setNames.Add(setName.ToUpper(), handle);
						return handle;
					}
					#endregion
				}
			}
		}

		private static void UnRegisterFileStreamContextByHandle(long handle)
		{
			lock (handles)
			{
				#region Ensure not already used before returning
				if (handles.ContainsKey(handle))
				{
					var ctx = handles[handle];
					handles.Remove(handle);
					setNames.Remove(ctx.SetName.ToUpper());
				}
				#endregion
			}
		}


		private static FileStreamContext LookUpFileStramContextByHandle(long handle)
		{
			lock (handles)
			{
				if (handles.ContainsKey(handle))
				{
					return handles[handle];
				}
			}
			return null!;
		}


        static private (FileStreamContext, long) LookupFileStreamContextAndHandleBySetName(string setName)
        {
            FileStreamContext Ctx = null!;
            long handle = INVALID_HANDLE;
            if (setNames.ContainsKey(setName.ToUpper()))
            {
                handle = setNames[setName.ToUpper()];
                Ctx = handles[handle];
            }
            return (Ctx, handle);
        }
        #endregion 
    }

    internal class FieldValue
	{
		internal object Value;
		internal APIOperationResponseCode ResponseCode = APIOperationResponseCode.OK;

		internal FieldValue( object value, APIOperationResponseCode result )
		{
			Value = value;
			ResponseCode = result;
		}
	}
}
