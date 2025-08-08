#define UNIMPLEMENTED_MEMBERS	// Comment out to remove unimplemented members from the class
#define FEATURE_ASYNC_IO		// Unosys does async!

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
#if FEATURE_REMOTING
using System.Runtime.Remoting.Messaging;
#endif
using System.Security;
#if FEATURE_MACL
using System.Security.AccessControl;
#endif
using System.Security.Permissions;
using System.Text;
using System.Threading;
#if FEATURE_ASYNC_IO
using System.Threading.Tasks;
#endif
//using Microsoft.Win32;
//using Microsoft.Win32.SafeHandles;

/*
 * FileStream supports different modes of accessing the disk - async mode
 * and sync mode.  They are two completely different codepaths in the
 * sync & async methods (ie, Read/Write vs. BeginRead/BeginWrite).  File
 * handles in NT can be opened in only sync or overlapped (async) mode,
 * and we have to deal with this pain.  Stream has implementations of
 * the sync methods in terms of the async ones, so we'll
 * call through to our base class to get those methods when necessary.
 *
 * Also buffering is added into FileStream as well. Folded in the
 * code from BufferedStream, so all the comments about it being mostly
 * aggressive (and the possible perf improvement) apply to FileStream as 
 * well.  Also added some buffering to the async code paths.
 *
 * Class Invariants:
 * The class has one buffer, shared for reading & writing.  It can only be
 * used for one or the other at any point in time - not both.  The following
 * should be true:
 *   0 <= _readPos <= _readLen < _bufferSize
 *   0 <= _writePos < _bufferSize
 *   _readPos == _readLen && _readPos > 0 implies the read buffer is valid, 
 *     but we're at the end of the buffer.
 *   _readPos == _readLen == 0 means the read buffer contains garbage.
 *   Either _writePos can be greater than 0, or _readLen & _readPos can be
 *     greater than zero, but neither can be greater than zero at the same time.
 *
 */

namespace Unosys.SDK
{
	// This is an internal object implementing IAsyncResult with fields
	// for all of the relevant data necessary to complete the IO operation.
	// This is used by AsyncFSCallback and all of the async methods.
	// We should probably make this a nested type of FileStream. But 
	// I don't know how to define a nested class in mscorlib.h

	// Ideally we shoult make this type windows only (!FEATURE_PAL). But to make that happen
	// we need to do a lot of untangling in the VM code.
	unsafe internal sealed class FileStreamAsyncResult : IAsyncResult
	{
		// README:
		// If you modify the order of these fields, make sure to update 
		// the native VM definition of this class as well!!! 
#if FEATURE_ASYNC_IO
		// User code callback
		private AsyncCallback _userCallback;
#endif
		private Object _userStateObject;
		private ManualResetEvent _waitHandle;
		//[System.Security.SecurityCritical]
		//private SafeFileHandle _handle;      // For cancellation support.
		private long _handle;
#if !FEATURE_PAL
		[SecurityCritical]
		private NativeOverlapped* _overlapped;
		internal NativeOverlapped* OverLapped { [SecurityCritical]get { return _overlapped; } }
		internal bool IsAsync { [SecuritySafeCritical]get { return _overlapped != null; } }
#endif

		internal int _EndXxxCalled;   // Whether we've called EndXxx already.
		private int _numBytes;     // number of bytes read OR written
		internal int NumBytes { get { return _numBytes; } }

		private int _errorCode;
		internal int ErrorCode { get { return _errorCode; } }

		private int _numBufferedBytes;
		internal int NumBufferedBytes { get { return _numBufferedBytes; } }

		internal int NumBytesRead { get { return _numBytes + _numBufferedBytes; } }

		private bool _isWrite;     // Whether this is a read or a write
		internal bool IsWrite { get { return _isWrite; } }

		private bool _isComplete;  // Value for IsCompleted property        
		private bool _completedSynchronously;  // Which thread called callback

		// The NativeOverlapped struct keeps a GCHandle to this IAsyncResult object.
		// So if the user doesn't call EndRead/EndWrite, a finalizer won't help because
		// it'll never get called. 

		// Overlapped class will take care of the async IO operations in progress 
		// when an appdomain unload occurs.

#if FEATURE_ASYNC_IO
		[System.Security.SecurityCritical] // auto-generated
		private unsafe static IOCompletionCallback s_IOCallback;

		[SecuritySafeCritical]
		internal FileStreamAsyncResult(
			int numBufferedBytes,
			byte[] bytes,
			long handle,	//SafeFileHandle handle,
			AsyncCallback userCallback,
			Object userStateObject,
			bool isWrite )
		{
			_userCallback = userCallback;
			_userStateObject = userStateObject;
			_isWrite = isWrite;
			_numBufferedBytes = numBufferedBytes;
			_handle = handle;

			// For Synchronous IO, I could go with either a callback and using
			// the managed Monitor class, or I could create a handle and wait on it.
			ManualResetEvent waitHandle = new ManualResetEvent( false );
			_waitHandle = waitHandle;

			// Create a managed overlapped class
			// We will set the file offsets later
			Overlapped overlapped = new Overlapped( 0, 0, IntPtr.Zero, this );

			// Pack the Overlapped class, and store it in the async result
			if (userCallback != null)
			{
				var ioCallback = s_IOCallback; // cached static delegate; delay initialized due to it being SecurityCritical
				if (ioCallback == null) s_IOCallback = ioCallback = new IOCompletionCallback( AsyncFSCallback );
				_overlapped = overlapped.Pack( ioCallback, bytes );
			}
			else
			{
				_overlapped = overlapped.UnsafePack( null, bytes );
			}

			Contract.Assert( _overlapped != null, "Did Overlapped.Pack or Overlapped.UnsafePack just return a null?" );
		}

		internal static FileStreamAsyncResult CreateBufferedReadResult( int numBufferedBytes, AsyncCallback userCallback, Object userStateObject, bool isWrite )
		{
			FileStreamAsyncResult asyncResult = new FileStreamAsyncResult( numBufferedBytes, userCallback, userStateObject, isWrite );
			asyncResult.CallUserCallback();
			return asyncResult;
		}

		// This creates a synchronous Async Result. We should consider making this a separate class and maybe merge it with 
		// System.IO.Stream.SynchronousAsyncResult
		private FileStreamAsyncResult( int numBufferedBytes, AsyncCallback userCallback, Object userStateObject, bool isWrite )
		{
			_userCallback = userCallback;
			_userStateObject = userStateObject;
			_isWrite = isWrite;
			_numBufferedBytes = numBufferedBytes;
		}
#endif // FEATURE_ASYNC_IO

		public Object AsyncState
		{
			get { return _userStateObject; }
		}

		public bool IsCompleted
		{
			get { return _isComplete; }
		}

		public WaitHandle AsyncWaitHandle
		{
#if !FEATURE_PAL
			[System.Security.SecuritySafeCritical]  // auto-generated
			//[ResourceExposure( ResourceScope.None )]
			//[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
			get
			{
				// Consider uncommenting this someday soon - the EventHandle 
				// in the Overlapped struct is really useless half of the 
				// time today since the OS doesn't signal it.  If users call
				// EndXxx after the OS call happened to complete, there's no
				// reason to create a synchronization primitive here.  Fixing
				// this will save us some perf, assuming we can correctly
				// initialize the ManualResetEvent.  
				if (_waitHandle == null)
				{
					ManualResetEvent mre = new ManualResetEvent( false );
					if (_overlapped != null && _overlapped->EventHandle != IntPtr.Zero)
					{
						//mre.SafeWaitHandle = new SafeWaitHandle( _overlapped->EventHandle, true );
					}

					// make sure only one thread sets _waitHandle
					if (Interlocked.CompareExchange<ManualResetEvent>( ref _waitHandle, mre, null ) == null)
					{
						if (_isComplete)
							_waitHandle.Set();
					}
					else
					{
						// There's a slight but acceptable ---- if we weren't
						// the thread that set _waitHandle and this code path
						// returns before the code in the if statement 
						// executes (on the other thread). However, the 
						// caller is waiting for the wait handle to be set, 
						// which will still happen.
						mre.Close();
					}
				}
				return _waitHandle;
			}
#else
            get { return null; }
#endif //!FEATURE_PAL
		}

		// Returns true iff the user callback was called by the thread that 
		// called BeginRead or BeginWrite.  If we use an async delegate or
		// threadpool thread internally, this will be false.  This is used
		// by code to determine whether a successive call to BeginRead needs 
		// to be done on their main thread or in their callback to avoid a
		// stack overflow on many reads or writes.
		public bool CompletedSynchronously
		{
			get { return _completedSynchronously; }
		}

#if FEATURE_ASYNC_IO
		private void CallUserCallbackWorker()
		{
			_isComplete = true;

			// ensure _isComplete is set before reading _waitHandle
			Thread.MemoryBarrier();
			if (_waitHandle != null)
				_waitHandle.Set();

			_userCallback( this );
		}

		internal void CallUserCallback()
		{
			// Convenience method for me, since I have to do this in a number 
			// of places in the buffering code for fake IAsyncResults.   
			// AsyncFSCallback intentionally does not use this method.

			if (_userCallback != null)
			{
				// Call user's callback on a threadpool thread.  
				// Set completedSynchronously to false, since it's on another 
				// thread, not the main thread.
				_completedSynchronously = false;
				ThreadPool.QueueUserWorkItem( state => ((FileStreamAsyncResult) state).CallUserCallbackWorker(), this );
			}
			else
			{
				_isComplete = true;

				// ensure _isComplete is set before reading _waitHandle
				Thread.MemoryBarrier();
				if (_waitHandle != null)
					_waitHandle.Set();
			}
		}

		[SecurityCritical]
		internal void ReleaseNativeResource()
		{
			// Free memory & GC handles.
			if (this._overlapped != null)
				Overlapped.Free( _overlapped );
		}

		internal void Wait()
		{
			if (_waitHandle != null)
			{
				// We must block to ensure that AsyncFSCallback has completed,
				// and we should close the WaitHandle in here.  AsyncFSCallback
				// and the hand-ported imitation version in COMThreadPool.cpp 
				// are the only places that set this event.
				try
				{
					_waitHandle.WaitOne();
					Contract.Assert( _isComplete == true, "FileStreamAsyncResult::Wait - AsyncFSCallback  didn't set _isComplete to true!" );
				}
				finally
				{
					_waitHandle.Close();
				}
			}
		}

		// When doing IO asynchronously (ie, _isAsync==true), this callback is 
		// called by a free thread in the threadpool when the IO operation 
		// completes.  
		[System.Security.SecurityCritical]  // auto-generated
		//[ResourceExposure( ResourceScope.None )]
		//[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		unsafe private static void AsyncFSCallback( uint errorCode, uint numBytes, NativeOverlapped* pOverlapped )
		{
			//BCLDebug.Log( string.Format( "AsyncFSCallback called.  errorCode: " + errorCode + "  numBytes: " + numBytes ) );

			// Unpack overlapped
			Overlapped overlapped = Overlapped.Unpack( pOverlapped );
			// Free the overlapped struct in EndRead/EndWrite.

			// Extract async result from overlapped 
			FileStreamAsyncResult asyncResult =
				(FileStreamAsyncResult) overlapped.AsyncResult;
			asyncResult._numBytes = (int) numBytes;

			//if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled( EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer ))
			//	FrameworkEventSource.Log.ThreadTransferReceive( (long) (asyncResult.OverLapped), 2, string.Empty );

			// Handle reading from & writing to closed pipes.  While I'm not sure
			// this is entirely necessary anymore, maybe it's possible for 
			// an async read on a pipe to be issued and then the pipe is closed, 
			// returning this error.  This may very well be necessary.
			if (errorCode == FileStream.ERROR_BROKEN_PIPE || errorCode == FileStream.ERROR_NO_DATA)
				errorCode = 0;

			asyncResult._errorCode = (int) errorCode;

			// Call the user-provided callback.  It can and often should
			// call EndRead or EndWrite.  There's no reason to use an async 
			// delegate here - we're already on a threadpool thread.  
			// IAsyncResult's completedSynchronously property must return
			// false here, saying the user callback was called on another thread.
			asyncResult._completedSynchronously = false;
			asyncResult._isComplete = true;

			// ensure _isComplete is set before reading _waitHandle
			Thread.MemoryBarrier();

			// The OS does not signal this event.  We must do it ourselves.
			ManualResetEvent wh = asyncResult._waitHandle;
			if (wh != null)
			{
				Contract.Assert( !wh.SafeWaitHandle.IsClosed, "ManualResetEvent already closed!" );
				bool r = wh.Set();
				Contract.Assert( r, "ManualResetEvent::Set failed!" );
				if (!r) __Error.WinIOError();
			}

			AsyncCallback userCallback = asyncResult._userCallback;
			if (userCallback != null)
				userCallback( asyncResult );
		}

		[SecuritySafeCritical]
		//////[HostProtection( ExternalThreading = true )]  // %LEGACY Unosys SDK%
		internal void Cancel()
		{
			//Contract.Assert( _handle != null, "_handle should not be null." );
			Contract.Assert( _overlapped != null, "Cancel should only be called on true asynchronous FileStreamAsyncResult, i.e. _overlapped is not null" );

			if (IsCompleted)
				return;

			//if (_handle.IsInvalid)
			if (_handle < 1)
				return;

			/*bool r = Win32Native.CancelIoEx( _handle, _overlapped );
			if (!r)
			{
				int errorCode = Marshal.GetLastWin32Error();

				// ERROR_NOT_FOUND is returned if CancelIoEx cannot find the request to cancel.
				// This probably means that the IO operation has completed.
				if (errorCode != Win32Native.ERROR_NOT_FOUND)
					__Error.WinIOError( errorCode, string.Empty );
			}*/
		}
#endif //FEATURE_ASYNC_IO
	}

	[ComVisible( true )]
	public class FileStream : System.IO.Stream
	{
		#region Field Members
		internal const int DefaultBufferSize = 4096;
		private static readonly bool _canUseAsync = true;
		//private unsafe static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(FileStream.AsyncFSCallback);

		private byte[] _buffer;   // Shared read/write buffer.  Alloc on first use. 
		private string _fileName; // Fully qualified file name. 
		private bool _isAsync;    // Whether we opened the handle for overlapped IO
		private bool _canRead;
		private bool _canWrite;
		private bool _canSeek;
		private bool _exposedHandle; // Could other code be using this handle?
		private bool _isPipe;     // Whether to disable async buffering code. 
		private int _readPos;     // Read pointer within shared buffer.
		private int _readLen;     // Number of bytes read in buffer from file. 
		private int _writePos;    // Write pointer within shared buffer. 
		private int _bufferSize;  // Length of internal buffer, if it's allocated.
		//[System.Security.SecurityCritical] // auto-generated
		//private SafeFileHandle _handle;
		private long _pos;        // Cache current location in the file.
		private long _appendStart;// When appending, prevent overwriting file.
#if FEATURE_ASYNC_IO
		// Unosys doesn't need hacks to do async Read/Write
		//private static AsyncCallback s_endReadTask;
		//private static AsyncCallback s_endWriteTask;
		//private static Action<object> s_cancelReadHandler;
		//private static Action<object> s_cancelWriteHandler;        
#endif
		private long len;		// Cached file length, purely for debugging (Length property doesn't resolve at run-time)
		private long _handle;

		// Windows API definitions, from winbase.h and others 
		private const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
		private const int FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
		private const int FILE_FLAG_OVERLAPPED = 0x40000000;
		internal const int GENERIC_READ = unchecked( (int) 0x80000000 );
		private const int GENERIC_WRITE = 0x40000000;

		private const int FILE_BEGIN = 0;
		private const int FILE_CURRENT = 1;
		private const int FILE_END = 2;

		// Error codes (not HRESULTS), from winerror.h
		internal const int ERROR_BROKEN_PIPE = 109;
		internal const int ERROR_NO_DATA = 232;
		internal const int ERROR_HANDLE_EOF = 38;
		internal const int ERROR_INVALID_PARAMETER = 87;
		internal const int ERROR_IO_PENDING = 997;
		#endregion

		#region Constructors & Destructor
		static FileStream()
		{
		}

		//This exists only to support IsolatedStorageFileStream.
		//Any changes to FileStream must include the corresponding changes in IsolatedStorage.
		internal FileStream()
		{
		}

		public FileStream( string path, FileMode mode )
			: this( path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous, Path.GetFileName( path ), false )
		{
		}

		public FileStream( string path, FileMode mode, FileAccess access )
			: this( path, mode, access, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous, Path.GetFileName( path ), false )
		{
		}

		public FileStream( string path, FileMode mode, FileAccess access, FileShare share )
			: this( path, mode, access, share, DefaultBufferSize, FileOptions.Asynchronous, Path.GetFileName( path ), false )
		{
		}

		public FileStream( string path, FileMode mode, FileAccess access, FileShare share, int bufferSize )
			: this( path, mode, access, share, bufferSize, FileOptions.Asynchronous, Path.GetFileName( path ), false )
		{
		}

		public FileStream( string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options )
			: this( path, mode, access, share, bufferSize, options, Path.GetFileName( path ), false )
		{
		}

		public FileStream( string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync )
			: this( path, mode, access, share, bufferSize, (useAsync ? FileOptions.Asynchronous : FileOptions.None), Path.GetFileName( path ), false )
		{
		}

#if !FEATURE_PAL && FEATURE_MACL 
        // This constructor is done differently to avoid loading a few more 
        // classes, and more importantly, to build correctly on Rotor.
        public FileStream(string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity fileSecurity)
        { 
            Object pinningHandle;
            Win32Native.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share, fileSecurity, out pinningHandle); 
            try { 
                Init(path, mode, (FileAccess)0, (int)rights, true, share, bufferSize, options, secAttrs, Path.GetFileName(path), false, false);
            } 
            finally {
                if (pinningHandle != null) {
                    GCHandle pinHandle = (GCHandle) pinningHandle;
                    pinHandle.Free(); 
                }
            } 
        } 
 
        public FileStream(string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options)
        { 
            Win32Native.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);
            Init(path, mode, (FileAccess)0, (int)rights, true, share, bufferSize, options, secAttrs, Path.GetFileName(path), false, false); 
        } 
#endif

		internal FileStream( string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, string msgPath, bool bFromProxy )
		{
			//Win32Native.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs( share );
			Init( path, mode, access, 0, false, share, bufferSize, options, msgPath, bFromProxy, false );
		}

		~FileStream()
		{
			if (_handle != -1)
			{
				// BCLDebug.Correctness(_handle.IsClosed, "You didn't close a FileStream & it got finalized.  Name: \""+_fileName+"\""); 
				Dispose( false );
			}
		}
		#endregion

		internal void Init( string path, FileMode mode, FileAccess access, int rights, bool useRights, FileShare share, int bufferSize, FileOptions options, string msgPath, bool bFromProxy, bool useLongPath )
		{
			#region Validate Parameters
			if (path == null)
				throw new ArgumentNullException( "ArgumentNull_Path" );
			if (path.Length == 0)
				throw new ArgumentException( "Argument_EmptyPath" );
			_fileName = msgPath;  // To handle odd cases of finalizing partially constructed objects. 
			_exposedHandle = false;

			// don't include inheritable in our bounds check for share 
			FileShare tempshare = share & ~FileShare.Inheritable;
			string badArg = null;

			if (mode < FileMode.CreateNew || mode > FileMode.Append)
				badArg = "mode";
			else if (!useRights && (access < FileAccess.Read || access > FileAccess.ReadWrite))
				badArg = "access";
#if !FEATURE_PAL && FEATURE_MACL 
            else if (useRights && (fileSystemRights < FileSystemRights.ReadData || fileSystemRights > FileSystemRights.FullControl)) 
                badArg = "rights";
#endif
			else if (tempshare < FileShare.None || tempshare > (FileShare.ReadWrite | FileShare.Delete))
				badArg = "share";

			if (badArg != null)
				throw new ArgumentOutOfRangeException( badArg, "ArgumentOutOfRange_Enum" );

			// NOTE: any change to FileOptions enum needs to be matched here in the error validation 
			if (options != FileOptions.None && (options & ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Encrypted | (FileOptions) 0x20000000 /* NoBuffering */)) != 0)
				throw new ArgumentOutOfRangeException( "ArgumentOutOfRange_Enum" );

			if (bufferSize <= 0)
				throw new ArgumentOutOfRangeException( "ArgumentOutOfRange_NeedPosNum" );

			// Write access validation
			if (!useRights && (access & FileAccess.Write) == 0)
			{
				if (mode == FileMode.Truncate || mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.Append)
				{
					// No write access
					if (!useRights)
						throw new ArgumentException( "Argument_InvalidFileMode&AccessCombo" );
				}
			}

#if !FEATURE_PAL && FEATURE_MACL 
            // FileMode.Truncate only works with GENERIC_WRITE (FileAccess.Write), source:MSDN 
            // For backcomp use FileAccess.Write when FileSystemRights.Write is specified
            if (useRights && (mode == FileMode.Truncate)) { 
                if (fileSystemRights == FileSystemRights.Write) {
                    useRights = false;
                    access = FileAccess.Write;
                } 
                else {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFileModeTruncate&RightsCombo", mode, fileSystemRights)); 
                } 
            }
#endif

			int fAccess;
			if (!useRights)
			{
				fAccess = access == FileAccess.Read ? GENERIC_READ :
				access == FileAccess.Write ? GENERIC_WRITE :
				GENERIC_READ | GENERIC_WRITE;
			}
			else
			{
				fAccess = rights;
			}

			// Get absolute path - Security needs this to prevent something
			// like trying to create a file in c:\tmp with the name 
			// "..\WinNT\System32\ntoskrnl.exe".  Store it for user convenience.
			int maxPath = useLongPath ? Path.MaxLongPath : Path.MaxPath;
			//int maxPath = Path.MaxLongPath;
			string filePath = Path.NormalizePath( path, true, maxPath ); 

			_fileName = filePath;

			// Prevent access to your disk drives as raw block devices.
			if (filePath.StartsWith( "\\\\.\\", StringComparison.Ordinal ))
				throw new ArgumentException( "Arg_DevicesNotSupported" );

			bool read = false;

#if FEATURE_MACL
            if ((!useRights && (access & FileAccess.Read) != 0) || (useRights && (fileSystemRights & FileSystemRights.ReadAndExecute) != 0))
#else
			if (!useRights && (access & FileAccess.Read) != 0)
#endif //FEATURE_MACL
			{
				if (mode == FileMode.Append)
					throw new ArgumentException( "Argument_InvalidAppendMode" );
				else
					read = true;
			}

			// All demands in full trust domains are no-ops, so skip 
#if FEATURE_CAS_POLICY
            if (CodeAccessSecurityEngine.QuickCheckForAllDemands())
            {
                // Need to throw the same exceptions that are thrown if we actually called QuickDemand() below.
                FileIOPermission.EmulateFileIOPermissionChecks(filePath);
            }
            else
#endif // FEATURE_CAS_POLICY

			// Build up security permissions required, as well as validate we 
			// have a sensible set of parameters.  IE, creating a brand new file 
			// for reading doesn't make much sense.
			//FileIOPermissionAccess secAccess = FileIOPermissionAccess.NoAccess;  // %LEGACY Unosys SDK%

			if (read)
			{
				Contract.Assert( mode != FileMode.Append );
                //secAccess = secAccess | FileIOPermissionAccess.Read; // %LEGACY Unosys SDK%
            }

            // I can't think of any combos of FileMode we should disallow if we
            // don't have read access.  Writing would pretty much always be valid 
            // in those cases.

            // For any FileSystemRights other than ReadAndExecute, demand Write permission 
            // This is probably bit overkill for TakeOwnership etc but we don't have any
            // matching FileIOPermissionAccess to demand. It is better that we ask for Write permission. 

#if !FEATURE_PAL && FEATURE_MACL
            // FileMode.OpenOrCreate & FileSystemRights.Synchronize can create 0-byte file; demand write
            if ((!useRights && (access & FileAccess.Write) != 0) 
                || (useRights && (fileSystemRights & (FileSystemRights.Write | FileSystemRights.Delete
                                            | FileSystemRights.DeleteSubdirectoriesAndFiles 
                                            | FileSystemRights.ChangePermissions 
                                            | FileSystemRights.TakeOwnership)) != 0)
                || (useRights && ((fileSystemRights & FileSystemRights.Synchronize) != 0) 
                                            && mode==FileMode.OpenOrCreate)
               )
#else
            if (!useRights && (access & FileAccess.Write) != 0)
#endif //!FEATURE_PAL && FEATURE_MACL
			{
                //if (mode == FileMode.Append)									// %LEGACY Unosys SDK%
                //	secAccess = secAccess | FileIOPermissionAccess.Append;		// %LEGACY Unosys SDK%
                //else															// %LEGACY Unosys SDK%
                //	secAccess = secAccess | FileIOPermissionAccess.Write;		// %LEGACY Unosys SDK%
            }

#if FEATURE_MACL
                bool specifiedAcl;
                unsafe {
                    specifiedAcl = secAttrs != null && secAttrs.pSecurityDescriptor != null;
                }
                
                AccessControlActions control = specifiedAcl ? AccessControlActions.Change : AccessControlActions.None;
                FileIOPermission.QuickDemand(secAccess, control, new string[] { filePath }, false, false);
#else
#if FEATURE_CORECLR
                if (checkHost) {
                    FileSecurityState state = new FileSecurityState(FileSecurityState.ToFileSecurityState(secAccess), path, filePath);
                    state.EnsureState();
                }
#else
            //FileIOPermission.QuickDemand( secAccess, filePath, false, false );
            //	new FileIOPermission( secAccess, new string[] { filePath } ).Demand();
#endif // FEATURE_CORECLR
#endif

            // Our Inheritable bit was stolen from Windows, but should be set in 
            // the security attributes class.  Don't leave this bit set. 
            share &= ~FileShare.Inheritable;

			bool seekToEnd = (mode == FileMode.Append);
			// Must use a valid Win32 constant here...
			if (mode == FileMode.Append)
				mode = FileMode.OpenOrCreate;

			// WRT async IO, do the right thing for whatever platform we're on. 
			// This way, someone can easily write code that opens a file 
			// asynchronously no matter what their platform is.
			if (_canUseAsync && (options & FileOptions.Asynchronous) != 0)
				_isAsync = true;
			else
				options &= ~FileOptions.Asynchronous;

			int flagsAndAttributes = (int) options;
			#endregion

			#region %TODO%
			// Need to validate we aren't trying to open an invalid (i.e.; non) file device  (i.e.; com1: lpt1: , etc.,)

            // Disallow access to all non-file devices from the FileStream
            // constructors that take a string.  Everyone else can call 
            // CreateFile themselves then use the constructor that takes an 
            // IntPtr.  Disallows "con:", "com1:", "lpt1:", etc.
			int fileType = Win32Native.FILE_TYPE_DISK;//Win32Native.GetFileType(_handle);
            if (fileType != Win32Native.FILE_TYPE_DISK) {
                //_handle.Close();
                throw new NotSupportedException("NotSupported_FileStreamOnNonFiles");
            }
			#endregion

#if !FEATURE_PAL
			// For mitigating local elevation of privilege attack through named pipes
			// make sure we always call CreateFile with SECURITY_ANONYMOUS so that the
			// named pipe server can't impersonate a high privileged client security context
			flagsAndAttributes |= (Win32Native.SECURITY_SQOS_PRESENT | Win32Native.SECURITY_ANONYMOUS);
#endif
			// Don't pop up a dialog for reading from an emtpy floppy drive
			//int oldMode = Win32Native.SetErrorMode( Win32Native.SEM_FAILCRITICALERRORS );
			try
			{
				//string tempPath = filePath;
				#region Open File via API call
				//Debug.Print( "SDK: FileStream.Init() - path={0}, FileMode={1}, FileAccess={2}, FileShare={3}, FileOptions={4}", new object[] { path, mode, access, share, options } );
				switch (mode)
				{
					case FileMode.Create:
						_handle = API.CreateSetAsync( filePath, (int) mode, (int) access, (int) options, (int) share ).Result;
						break;

					case FileMode.Open:
						_handle = API.OpenSetAsync( filePath, (uint) mode, (uint) access, (uint) options, (uint) share ).Result;
						break;

					default:
						throw new IOException( "Unsupported File.Mode" );
				}
				//if (useLongPath)
				//	tempPath = Path.AddLongPathPrefix(tempPath); 
				//_handle = Win32Native.SafeCreateFile(tempPath, fAccess, share, secAttrs, mode, flagsAndAttributes, Win32Native.NULL);

				//if (_handle.IsInvalid) {
				//	// Return a meaningful exception, using the RELATIVE path to 
				//	// the file to avoid returning extra information to the caller
				//	// unless they have path discovery permission, in which case 
				//	// the full path is fine & useful. 

				//	// NT5 oddity - when trying to open "C:\" as a FileStream, 
				//	// we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
				//	// probably be consistent w/ every other directory.
				//	int errorCode = Marshal.GetLastWin32Error();
				//	//if (errorCode==//__Error.ERROR_PATH_NOT_FOUND && filePath.Equals(Directory.GetDirectoryRoot(filePath))) 
				//	//	errorCode = //__Error.ERROR_ACCESS_DENIED;

				//	// We need to give an exception, and preferably it would include 
				//	// the fully qualified path name.  Do security check here.  If
				//	// we fail, give back the msgPath, which should not reveal much. 
				//	// While this logic is largely duplicated in
				//	// //__Error.WinIOError, we need this for
				//	// IsolatedStorageFileStream.
				//   // bool canGiveFullPath = false; 

				//	if (!bFromProxy) 
				//	{ 
				//		try {
				//		  //  new FileIOPermission(FileIOPermissionAccess.PathDiscovery, new string[] { _fileName }, false, false ).Demand(); 
				//		  //  canGiveFullPath = true;
				//		}
				//		catch(SecurityException) {}
				//	} 

				//	//if (canGiveFullPath) 
				//	//	//__Error.WinIOError(errorCode, _fileName); 
				//	//else
				//	//	//__Error.WinIOError(errorCode, msgPath); 
				//}

				var testGetFileLength = this.Length;	// Just for debugging - Remove this any time
				#endregion
			}
			catch (Exception ex)
			{
				Debug.Print( "!*!*!*!*!*!*!*!*!*!*!***  ERROR -  Unosys.SKD.FileStream.Init() - {0}  ***!*!*!*!*!*!*!*!*!*!*!*!", ex );
				throw;
			}

#if FEATURE_ASYNC_IO
			// This is necessary for async IO using IO Completion ports via our 
			// managed Threadpool API's.  This (theoretically) calls the OS's 
			// BindIoCompletionCallback method, and passes in a stub for the 
			// LPOVERLAPPED_COMPLETION_ROUTINE.  This stub looks at the Overlapped
			// struct for this request and gets a delegate to a managed callback 
			// from there, which it then calls on a threadpool thread.  (We allocate
			// our native OVERLAPPED structs 2 pointers too large and store EE state
			// & GC handles there, one to an IAsyncResult, the other to a delegate.)
			/*if (_isAsync)
			{
				bool b = false;
				// BindHandle requires UnmanagedCode permission
				new SecurityPermission( SecurityPermissionFlag.UnmanagedCode ).Assert();
				try
				{
					b = ThreadPool.BindHandle( _handle );
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
					if (!b)
					{
						// We should close the handle so that the handle is not open until SafeFileHandle GC
						Contract.Assert( !_exposedHandle, "Are we closing handle that we exposed/not own, how?" );
						_handle.Close();
					}
				}
				if (!b)
					throw new IOException( "IO.IO_BindHandleFailed" );
			}*/
			// In Unosys we don't have to "bind" handles, handles are completely divorced from the current machine!
#endif // FEATURE_ASYNC_IO

			if (!useRights)
			{
				_canRead = (access & FileAccess.Read) != 0;
				_canWrite = (access & FileAccess.Write) != 0;
			}
#if !FEATURE_PAL && FEATURE_MACL 
            else { 
                _canRead = (fileSystemRights & FileSystemRights.ReadData) != 0;
                _canWrite = ((fileSystemRights & FileSystemRights.WriteData) != 0) 
                            || ((fileSystemRights & FileSystemRights.AppendData) != 0);
            }
#endif //!FEATURE_PAL && FEATURE_MACL

			_canSeek = true;
			_isPipe = false;
			_pos = 0;
			_bufferSize = bufferSize;
			_readPos = 0;
			_readLen = 0;
			_writePos = 0;

			// For Append mode... 
			if (seekToEnd)
			{
				_appendStart = SeekCore( 0, System.IO.SeekOrigin.End );
			}
			else
			{
				_appendStart = -1;
			}
		}

#if FEATURE_MACL
        // If pinningHandle is not null, caller must free it AFTER the call to
        // CreateFile has returned.
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static Win32Native.SECURITY_ATTRIBUTES GetSecAttrs(FileShare share, FileSecurity fileSecurity, out Object pinningHandle)
        {
            pinningHandle = null;
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
            if ((share & FileShare.Inheritable) != 0 || fileSecurity != null) {
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);
 
                if ((share & FileShare.Inheritable) != 0) {
                    secAttrs.bInheritHandle = 1;
                }
 
                // For ACL's, get the security descriptor from the FileSecurity.
                if (fileSecurity != null) {
                    byte[] sd = fileSecurity.GetSecurityDescriptorBinaryForm();
                    pinningHandle = GCHandle.Alloc(sd, GCHandleType.Pinned);
                    fixed(byte* pSecDescriptor = sd)
                        secAttrs.pSecurityDescriptor = pSecDescriptor;
                }
            }

            return secAttrs;
        }
#endif

		// Verifies that this handle supports synchronous IO operations (unless you
		// didn't open it for either reading or writing).
		[System.Security.SecuritySafeCritical]  // auto-generated
		private unsafe void VerifyHandleIsSync()
		{
			// Do NOT use this method on pipes.  Reading or writing to a pipe may
			// cause an app to block incorrectly, introducing a deadlock (depending
			// on whether a write will wake up an already-blocked thread or this
			// FileStream's thread).

			// Do NOT change this to use a byte[] of length 0, or test test won't
			// work.  Our ReadFile & WriteFile methods are special cased to return
			// for arrays of length 0, since we'd get an IndexOutOfRangeException 
			// while using C#'s fixed syntax.
			byte[] bytes = new byte[1];
			int hr = 0;
			int r = 0;

			// If the handle is a pipe, ReadFile will block until there
			// has been a write on the other end.  We'll just have to deal with it,
			// For the read end of a pipe, you can mess up and 
			// accidentally read synchronously from an async pipe.
			if (CanRead)
			{
				//r = ReadFileNative( _handle, bytes, 0, 0, null, out hr );
				r = Read( bytes, 0, 0 );
			}
			else if (CanWrite)
			{
				//r = WriteFileNative( _handle, bytes, 0, 0, null, out hr );
				Write( bytes, 0, 0 );
			}

			if (hr == ERROR_INVALID_PARAMETER)
				throw new ArgumentException( "Arg_HandleNotSync" );
			if (hr == Win32Native.ERROR_INVALID_HANDLE)
				__Error.WinIOError( hr, "<OS handle>" );
		}
 
		#region Original Public Members
		public override bool CanRead
		{
			get { return _canRead; }
		}

		public override bool CanSeek
		{
			get { return _canSeek; }
		}

		public override bool CanWrite
		{
			get { return _canWrite; }
		}

		public virtual IntPtr Handle
		{
			[System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
			[SecurityPermissionAttribute( SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode )]
#endif
			//[ResourceExposure( ResourceScope.Machine )]
			//[ResourceConsumption( ResourceScope.Machine )]
			get
			{
				Flush();
				// Explicitly dump any buffered data, since the user could move our
				// position or write to the file.
				_readPos = 0;
				_readLen = 0;
				_writePos = 0;
				_exposedHandle = true;

				//return _handle.DangerousGetHandle();
				return new IntPtr( _handle );
			}
		}

		public virtual bool IsAsync
		{
			get { return _isAsync; }
		}

		public override long Length
		{
			get
			{
				////if (_handle.IsClosed) //__Error.FileNotOpen();
				////if (!CanSeek) //__Error.SeekNotSupported(); 
				//int hi = 0, lo = 0;

				//lo = Win32Native.GetFileSize(_handle, out hi);
				len = Convert.ToInt64( API.GetSetLengthAsync( _handle ).Result );

				//if (lo==-1) {  // Check for either an error or a 4GB - 1 byte file.
				//	int hr = Marshal.GetLastWin32Error(); 
				//	//if (hr != 0) 
				//	//	//__Error.WinIOError(hr, string.Empty);
				//} 
				//long len = (((long)hi) << 32) | ((uint) lo);
				// If we're writing near the end of the file, we must include our
				// internal buffer in our Length calculation.  Don't flush because
				// we use the length of the file in our async write method. 
				if (_writePos > 0 && _pos + _writePos > len)
					len = _writePos + _pos;

				return len;
			}
		}

		public string Name
		{
			get
			{
				if (_fileName == null)
					return "IO_UnknownFileName";
				//new FileIOPermission(FileIOPermissionAccess.PathDiscovery, new string[] { _fileName }, false, false ).Demand(); 
#if FEATURE_CORECLR
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, string.Empty, _fileName);
                sourceState.EnsureState();
#else
                //FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, _fileName, false, false);
#endif
				return _fileName;
			}
		}

		public override long Position
		{
			get
			{
				// if (_handle.IsClosed) //__Error.FileNotOpen();
				//  if (!CanSeek) //__Error.SeekNotSupported(); 

				Contract.Assert( (_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both." );

				// Verify that internal position is in [....] with the handle
				if (_exposedHandle)
					VerifyOSHandlePosition();

				// Compensate for buffer that we read from the handle (_readLen) Vs what the user
				// read so far from the internel buffer (_readPos). Of course add any unwrittern 
				// buffered data 
				return _pos + (_readPos - _readLen + _writePos);
			}
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException( "ArgumentOutOfRange_NeedNonNegNum" );
				Contract.EndContractBlock();
				if (_writePos > 0) FlushWrite( false );
				_readPos = 0;
				_readLen = 0;
				Seek( value, System.IO.SeekOrigin.Begin );
			}
		}

		protected override void Dispose( bool disposing )
		{
			// Nothing will be done differently based on whether we are
			// disposing vs. finalizing.  This is taking advantage of the 
			// weak ordering between normal finalizable objects & critical 
			// finalizable objects, which I included in the SafeHandle
			// design for FileStream, which would often "just work" when 
			// finalized.
			try
			{
				//if (_handle != null && !_handle.IsClosed) {
				//	// Flush data to disk iff we were writing.  After 
				//	// thinking about this, we also don't need to flush
				//	// our read position, regardless of whether the handle 
				//	// was exposed to the user.  They probably would NOT 
				//	// want us to do this.
				//	if (_writePos > 0) { 
				//		FlushWrite(!disposing);
				//	}
				//}
			}
			finally
			{
				//if (_handle != null && !_handle.IsClosed) 
				//	_handle.Dispose(); 

				_canRead = false;
				_canWrite = false;
				_canSeek = false;
				// Don't set the buffer to null, to avoid a NullReferenceException
				// when users have a race condition in their code (ie, they call 
				// Close when calling another method on Stream like Read).
				//_buffer = null; 
				base.Dispose( disposing );
			}
		}

		public override void Flush()
		{
			Flush( false );
		}

		[System.Security.SecuritySafeCritical]
		public virtual void Flush( bool flushToDisk )
		{
			// This code is duplicated in Dispose
			//if (_handle.IsClosed) __Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

			FlushInternalBuffer();

			/*if (flushToDisk && CanWrite)
			{
				FlushOSBuffer();
			}*/
		}

		private void FlushInternalBuffer()
		{
			if (_writePos > 0)
			{
				FlushWrite( false );
			}
			else if (_readPos < _readLen && CanSeek)
			{
				FlushRead();
			}
		}

#if FEATURE_ASYNC_IO
		// Unlike Flush(), FlushAsync() always flushes to disk. This is intentional.
		// Legend is that we chose not to flush the OS file buffers in Flush() in fear of 
		// perf problems with frequent, long running FlushFileBuffers() calls. But we don't 
		// have that problem with FlushAsync() because we will call FlushFileBuffers() in the background.
		////[HostProtection( ExternalThreading = true )]
		[ComVisible( false )]
		[System.Security.SecuritySafeCritical]
		public override Task FlushAsync( CancellationToken cancellationToken )
		{
			// If we have been inherited into a subclass, the following implementation could be incorrect
			// since it does not call through to Flush() which a subclass might have overriden.  To be safe 
			// we will only use this implementation in cases where we know it is safe to do so,
			// and delegate to our base class (which will call into Flush) when we are not sure.
			if (this.GetType() != typeof( FileStream ))
				return base.FlushAsync( cancellationToken );

			cancellationToken.ThrowIfCancellationRequested();
			//if (cancellationToken.IsCancellationRequested)
			//	return Task.FromCancellation( cancellationToken );

			//if (_handle.IsClosed)
			if (_handle < 1)
				__Error.FileNotOpen();

			// The always synchronous data transfer between the OS and the internal buffer is intentional 
			// because this is needed to allow concurrent async IO requests. Concurrent data transfer
			// between the OS and the internal buffer will result in race conditions. Since FlushWrite and
			// FlushRead modify internal state of the stream and transfer data between the OS and the 
			// internal buffer, they cannot be truly async. We will, however, flush the OS file buffers
			// asynchronously because it doesn't modify any internal state of the stream and is potentially 
			// a long running process.
			FlushInternalBuffer();
			/*try
			{
				FlushInternalBuffer();
			}
			catch (Exception e)
			{
				return Task.FromException( e );
			}*/

			/*if (CanWrite)
				return Task.Factory.StartNew(
					state => ((FileStream) state).FlushOSBuffer(),
					this,
					cancellationToken,
					TaskCreationOptions.DenyChildAttach,
					TaskScheduler.Default );
			else*/
			return Task.FromResult( 0 );
			//	return Task.CompletedTask;
		}
#endif //FEATURE_ASYNC_IO

		public override void SetLength( long value )
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException( "ArgumentOutOfRange_NeedNonNegNum" );
			Contract.EndContractBlock();

			//if (_handle.IsClosed) //__Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();
			if (!CanWrite) __Error.WriteNotSupported();
			if (!CanSeek) __Error.SeekNotSupported();

			// Handle buffering updates. 
			if (_writePos > 0)
			{
				FlushWrite( false );
			}
			else if (_readPos < _readLen)
			{
				FlushRead();
			}

			_readPos = 0;
			_readLen = 0;
			if (_appendStart != -1 && value < _appendStart)
				throw new IOException( "IO.IO_SetLengthAppendTruncate" );
			SetLengthCore( value );
		}

		public override int Read( [In, Out] byte[] array, int offset, int count )
		{
			if (array == null)
				throw new ArgumentNullException( "ArgumentNull_Buffer" );
			if (offset < 0)
				throw new ArgumentOutOfRangeException( "ArgumentOutOfRange_NeedNonNegNum" );
			if (count < 0)
				throw new ArgumentOutOfRangeException( "ArgumentOutOfRange_NeedNonNegNum" );
			if (array.Length - offset < count)
				throw new ArgumentException( "Argument_InvalidOffLen" );
			//  Contract.EndContractBlock();

			// if (_handle.IsClosed) //__Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

			Contract.Assert( (_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both." );

			bool isBlocked = false;
			int n = _readLen - _readPos;
			// if the read buffer is empty, read into either user's array or our 
			// buffer, depending on number of bytes user asked for and buffer size.
			if (n == 0)
			{
				if (!CanRead) __Error.ReadNotSupported();
				if (_writePos > 0) FlushWrite( false );
				if (!CanSeek || (count >= _bufferSize))
				{
					var devicePos1 = GetDevicePosition();
					n = ReadCore( array, offset, count, devicePos1 ).Result;
					// Throw away read buffer.
					_readPos = 0;
					_readLen = 0;
					return n;
				}

				if (_buffer == null) _buffer = new byte[_bufferSize];
				var devicePos2 = GetDevicePosition();
				n = ReadCore( _buffer, 0, _bufferSize, devicePos2 ).Result;
				if (n == 0) return 0;
				isBlocked = n < _bufferSize;
				_readPos = 0;
				_readLen = n;
			}

			// Now copy min of count or numBytesAvailable (ie, near EOF) to array.
			if (n > count) n = count;
			Buffer.BlockCopy( _buffer, _readPos, array, offset, n );
			_readPos += n;

			// We may have read less than the number of bytes the user asked 
			// for, but that is part of the Stream contract.  Reading again for
			// more data may cause us to block if we're using a device with 
			// no clear end of file, such as a serial port or pipe.  If we 
			// blocked here & this code was used with redirected pipes for a
			// process's standard output, this can lead to deadlocks involving 
			// two processes. But leave this here for files to avoid what would
			// probably be a breaking change.         --

			// If we are reading from a device with no clear EOF like a 
			// serial port or a pipe, this will cause us to block incorrectly.
			if (!_isPipe)
			{
				// If we hit the end of the buffer and didn't have enough bytes, we must 
				// read some more from the underlying stream.  However, if we got
				// fewer bytes from the underlying stream than we asked for (ie, we're 
				// probably blocked), don't ask for more bytes.
				if (n < count && !isBlocked)
				{
					Contract.Assert( _readPos == _readLen, "Read buffer should be empty!" );
					var devicePos1 = GetDevicePosition();
					int moreBytesRead = ReadCore( array, offset + n, count - n, devicePos1 ).Result;
					n += moreBytesRead;
					// We've just made our buffer inconsistent with our position 
					// pointer.  We must throw away the read buffer. 
					_readPos = 0;
					_readLen = 0;
				}
			}

			return n;
		}

		private ulong GetDevicePosition()
		{
			//var result = _pos + (_readPos - _readLen + _writePos);
			//Debug.WriteLine( "Currently reading from device position: {0} + {1} - {2} + {3} = {4}", _pos, _readPos, _readLen, _writePos, result );
			return (ulong) this.Position;
		}

		public override long Seek( long offset, System.IO.SeekOrigin origin )
		{
			//if (origin<seekorigin.begin ||="" origin="">SeekOrigin.End) 
			//	throw new ArgumentException("Argument_InvalidSeekOrigin"); 
			Contract.EndContractBlock();
			//if (_handle.IsClosed) //__Error.FileNotOpen(); 
			if (_handle < 1) __Error.FileNotOpen();
			if (!CanSeek) __Error.SeekNotSupported();

			//Contract.Assert((_readPos==0 && _readLen==0 && _writePos >= 0) || (_writePos==0 && _readPos <= _readLen), "We're either reading or writing, but not both.");

			// If we've got bytes in our buffer to write, write them out.
			// If we've read in and consumed some bytes, we'll have to adjust 
			// our seek positions ONLY IF we're seeking relative to the current 
			// position in the stream.  This simulates doing a seek to the new
			// position, then a read for the number of bytes we have in our buffer. 
			if (_writePos > 0)
			{
				FlushWrite( false );
			}
			else if (origin == System.IO.SeekOrigin.Current)
			{
				// Don't call FlushRead here, which would have caused an infinite
				// loop.  Simply adjust the seek origin.  This isn't necessary 
				// if we're seeking relative to the beginning or end of the stream. 
				offset -= (_readLen - _readPos);
			}

			// Verify that internal position is in [....] with the handle
			if (_exposedHandle)
				VerifyOSHandlePosition();

			long oldPos = _pos + (_readPos - _readLen);
			long pos = SeekCore( offset, origin );

			// Prevent users from overwriting data in a file that was opened in 
			// append mode.
			if (_appendStart != -1 && pos < _appendStart)
			{
				SeekCore( oldPos, System.IO.SeekOrigin.Begin );
				throw new IOException( "IO.IO_SeekAppendOverwrite" );
			}

			// We now must update the read buffer.  We can in some cases simply 
			// update _readPos within the buffer, copy around the buffer so our
			// Position property is still correct, and avoid having to do more 
			// reads from the disk.  Otherwise, discard the buffer's contents.
			if (_readLen > 0)
			{
				// We can optimize the following condition:
				// oldPos - _readPos <= pos < oldPos + _readLen - _readPos 
				if (oldPos == pos)
				{
					if (_readPos > 0)
					{
						//Console.WriteLine("Seek: seeked for 0, adjusting buffer back by: "+_readPos+"  _readLen: "+_readLen); 
						Buffer.BlockCopy( _buffer, _readPos, _buffer, 0, _readLen - _readPos );
						_readLen -= _readPos;
						_readPos = 0;
					}
					// If we still have buffered data, we must update the stream's
					// position so our Position property is correct. 
					if (_readLen > 0)
						SeekCore( _readLen, System.IO.SeekOrigin.Current );
				}
				else if (oldPos - _readPos < pos && pos < oldPos + _readLen - _readPos)
				{
					int diff = (int) (pos - oldPos);
					//Console.WriteLine("Seek: diff was "+diff+", readpos was "+_readPos+"  adjusting buffer - shrinking by "+ (_readPos + diff));
					Buffer.BlockCopy( _buffer, _readPos + diff, _buffer, 0, _readLen - (_readPos + diff) );
					_readLen -= (_readPos + diff);
					_readPos = 0;
					if (_readLen > 0)
						SeekCore( _readLen, System.IO.SeekOrigin.Current );
				}
				else
				{
					// Lose the read buffer. 
					_readPos = 0;
					_readLen = 0;
				}
				Contract.Assert( _readLen >= 0 && _readPos <= _readLen, "_readLen should be nonnegative, and _readPos should be less than or equal _readLen" );
				Contract.Assert( pos == Position, "Seek optimization: pos != Position!  Buffer math was mangled." );
			}

			return pos;
		}

		// Checks the position of the OS's handle equals what we expect it to. 
		// This will fail if someone else moved the FileStream's handle or if
		// we've hit a bug in FileStream's position updating code. 

		public override void Write( byte[] array, int offset, int count )
		{
			if (array == null)
				throw new ArgumentNullException( "ArgumentNull_Buffer" );
			if (offset < 0)
				throw new ArgumentOutOfRangeException( "ArgumentOutOfRange_NeedNonNegNum" );
			if (count < 0)
				throw new ArgumentOutOfRangeException( "ArgumentOutOfRange_NeedNonNegNum" );
			if (array.Length - offset < count)
				throw new ArgumentException( "Argument_InvalidOffLen" );
			Contract.EndContractBlock();

			// if (_handle.IsClosed) //__Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

			if (_writePos == 0)
			{
				// Ensure we can write to the stream, and ready buffer for writing.
				if (!CanWrite) //__Error.WriteNotSupported(); 
					if (_readPos < _readLen) FlushRead();
				_readPos = 0;
				_readLen = 0;
			}

			// If our buffer has data in it, copy data from the user's array into
			// the buffer, and if we can fit it all there, return.  Otherwise, write
			// the buffer to disk and copy any remaining data into our buffer.
			// The assumption here is memcpy is cheaper than disk (or net) IO. 
			// (10 milliseconds to disk vs. ~20-30 microseconds for a 4K memcpy)
			// So the extra copying will reduce the total number of writes, in 
			// non-pathological cases (ie, write 1 byte, then write for the buffer 
			// size repeatedly)
			if (_writePos > 0)
			{
				int numBytes = _bufferSize - _writePos;   // space left in buffer
				if (numBytes > 0)
				{
					if (numBytes > count)
						numBytes = count;
					Buffer.BlockCopy( array, offset, _buffer, _writePos, numBytes );
					_writePos += numBytes;
					if (count == numBytes) return;
					offset += numBytes;
					count -= numBytes;
				}

				// Reset our buffer.  We essentially want to call FlushWrite
				// without calling Flush on the underlying Stream.
				int written = WriteCore( _buffer, 0, _writePos ).Result;
				_writePos = 0;
			}

			// If the buffer would slow writes down, avoid buffer completely. 
			if (count >= _bufferSize)
			{
				Contract.Assert( _writePos == 0, "FileStream cannot have buffered data to write here!  Your stream will be corrupted." );
				int written = WriteCore( array, offset, count ).Result;
				return;
			}
			else if (count == 0)
				return;  // Don't allocate a buffer then call memcpy for 0 bytes.

			if (_buffer == null) _buffer = new byte[_bufferSize];
			// Copy remaining bytes into buffer, to write at a later date. 
			Buffer.BlockCopy( array, offset, _buffer, _writePos, count );
			_writePos = count;
			return;
		}

		[System.Security.SecuritySafeCritical]  // auto-generated
		////[HostProtection( ExternalThreading = true )]
		public override IAsyncResult BeginRead( byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject )
		{
			if (array == null)
				throw new ArgumentNullException( "array" );
			if (offset < 0)
				throw new ArgumentOutOfRangeException( "offset", "ArgumentOutOfRange_NeedNonNegNum" );
			if (numBytes < 0)
				throw new ArgumentOutOfRangeException( "numBytes", "ArgumentOutOfRange_NeedNonNegNum" );
			if (array.Length - offset < numBytes)
				throw new ArgumentException( "Argument_InvalidOffLen" );
			Contract.EndContractBlock();

			//if (_handle.IsClosed) __Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

#if FEATURE_ASYNC_IO
			if (!_isAsync)
				return base.BeginRead( array, offset, numBytes, userCallback, stateObject );
			else
				return BeginReadAsync( array, offset, numBytes, userCallback, stateObject );
#else
            return base.BeginRead(array, offset, numBytes, userCallback, stateObject);
#endif // FEATURE_ASYNC_IO
		}

#if FEATURE_ASYNC_IO
		[System.Security.SecuritySafeCritical]  // auto-generated
		////[HostProtection( ExternalThreading = true )]
		private FileStreamAsyncResult BeginReadAsync( byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject )
		{
			Contract.Assert( _isAsync );

			if (!CanRead) __Error.ReadNotSupported();

			Contract.Assert( (_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both." );

			if (_isPipe)
			{
				// Pipes are ----ed up, at least when you have 2 different pipes
				// that you want to use simultaneously.  When redirecting stdout
				// & stderr with the Process class, it's easy to deadlock your
				// parent & child processes when doing writes 4K at a time.  The
				// OS appears to use a 4K buffer internally.  If you write to a
				// pipe that is full, you will block until someone read from 
				// that pipe.  If you try reading from an empty pipe and 
				// FileStream's BeginRead blocks waiting for data to fill it's 
				// internal buffer, you will be blocked.  In a case where a child
				// process writes to stdout & stderr while a parent process tries
				// reading from both, you can easily get into a deadlock here.
				// To avoid this deadlock, don't buffer when doing async IO on
				// pipes.  But don't completely ignore buffered data either.  
				if (_readPos < _readLen)
				{
					int n = _readLen - _readPos;
					if (n > numBytes) n = numBytes;
					//Buffer.InternalBlockCopy( _buffer, _readPos, array, offset, n );
					Buffer.BlockCopy( _buffer, _readPos, array, offset, n );
					_readPos += n;

					// Return a synchronous FileStreamAsyncResult
					return FileStreamAsyncResult.CreateBufferedReadResult( n, userCallback, stateObject, false );
				}
				else
				{
					Contract.Assert( _writePos == 0, "FileStream must not have buffered write data here!  Pipes should be unidirectional." );
					return BeginReadCore( array, offset, numBytes, userCallback, stateObject, 0 );
				}
			}

			Contract.Assert( !_isPipe, "Should not be a pipe." );

			// Handle buffering.
			if (_writePos > 0) FlushWrite( false );
			if (_readPos == _readLen)
			{
				// I can't see how to handle buffering of async requests when 
				// filling the buffer asynchronously, without a lot of complexity.
				// The problems I see are issuing an async read, we do an async 
				// read to fill the buffer, then someone issues another read 
				// (either synchronously or asynchronously) before the first one 
				// returns.  This would involve some sort of complex buffer locking
				// that we probably don't want to get into, at least not in V1.
				// If we did a sync read to fill the buffer, we could avoid the
				// problem, and any async read less than 64K gets turned into a
				// synchronous read by NT anyways...       -- 

				if (numBytes < _bufferSize)
				{
					if (_buffer == null) _buffer = new byte[_bufferSize];
					IAsyncResult bufferRead = BeginReadCore( _buffer, 0, _bufferSize, null, null, 0 );
					_readLen = EndRead( bufferRead );
					int n = _readLen;
					if (n > numBytes) n = numBytes;
					//Buffer.InternalBlockCopy( _buffer, 0, array, offset, n );
					Buffer.BlockCopy( _buffer, 0, array, offset, n );
					_readPos = n;

					// Return a synchronous FileStreamAsyncResult
					return FileStreamAsyncResult.CreateBufferedReadResult( n, userCallback, stateObject, false );
				}
				else
				{
					// Here we're making our position pointer inconsistent
					// with our read buffer.  Throw away the read buffer's contents.
					_readPos = 0;
					_readLen = 0;
					return BeginReadCore( array, offset, numBytes, userCallback, stateObject, 0 );
				}
			}
			else
			{
				int n = _readLen - _readPos;
				if (n > numBytes) n = numBytes;
				//Buffer.InternalBlockCopy( _buffer, _readPos, array, offset, n );
				Buffer.BlockCopy( _buffer, _readPos, array, offset, n );
				_readPos += n;

				if (n >= numBytes)
				{
					// Return a synchronous FileStreamAsyncResult
					return FileStreamAsyncResult.CreateBufferedReadResult( n, userCallback, stateObject, false );
				}
				else
				{
					// For streams with no clear EOF like serial ports or pipes
					// we cannot read more data without causing an app to block
					// incorrectly.  Pipes don't go down this path 
					// though.  This code needs to be fixed.
					// Throw away read buffer.
					_readPos = 0;
					_readLen = 0;
					return BeginReadCore( array, offset + n, numBytes - n, userCallback, stateObject, n );
				}
				// WARNING: all state on asyncResult objects must be set before
				// we call ReadFile in BeginReadCore, since the OS can run our
				// callback & the user's callback before ReadFile returns.
			}
		}
#endif // FEATURE_ASYNC_IO

#if FEATURE_ASYNC_IO
		[System.Security.SecuritySafeCritical]  // auto-generated
		//[ResourceExposure( ResourceScope.None )]
		//[ResourceConsumption( ResourceScope.AppDomain, ResourceScope.AppDomain )]
		unsafe private FileStreamAsyncResult BeginReadCore( byte[] bytes, int offset, int numBytes, AsyncCallback userCallback, Object stateObject, int numBufferedBytesRead )
		{
			//Contract.Assert( !_handle.IsClosed, "!_handle.IsClosed" );
			Contract.Assert( CanRead, "CanRead" );
			Contract.Assert( bytes != null, "bytes != null" );
			Contract.Assert( _writePos == 0, "_writePos == 0" );
			Contract.Assert( _isAsync, "BeginReadCore doesn't work on synchronous file streams!" );
			Contract.Assert( offset >= 0, "offset is negative" );
			Contract.Assert( numBytes >= 0, "numBytes is negative" );

			// Create and store async stream class library specific data in the async result

			// Must pass in _numBufferedBytes here to ensure all the state on the IAsyncResult 
			// object is set before we call ReadFile, which gives the OS an
			// opportunity to run our callback (including the user callback &
			// the call to EndRead) before ReadFile has returned.   
			FileStreamAsyncResult asyncResult = new FileStreamAsyncResult( numBufferedBytesRead, bytes, _handle, userCallback, stateObject, false );
			NativeOverlapped* intOverlapped = asyncResult.OverLapped;

			// Calculate position in the file we should be at after the read is done
			if (CanSeek)
			{
				long len = Length;

				// Make sure we are reading from the position that we think we are
				if (_exposedHandle)
					VerifyOSHandlePosition();

				if (_pos + numBytes > len)
				{
					if (_pos <= len)
						numBytes = (int) (len - _pos);
					else
						numBytes = 0;
				}

				// Now set the position to read from in the NativeOverlapped struct
				// For pipes, we should leave the offset fields set to 0.
				intOverlapped->OffsetLow = unchecked( (int) _pos );
				intOverlapped->OffsetHigh = (int) (_pos >> 32);

				// When using overlapped IO, the OS is not supposed to 
				// touch the file pointer location at all.  We will adjust it 
				// ourselves. This isn't threadsafe.

				// WriteFile should not update the file pointer when writing
				// in overlapped mode, according to MSDN.  But it does update 
				// the file pointer when writing to a UNC path!   
				// So changed the code below to seek to an absolute 
				// location, not a relative one.  ReadFile seems consistent though.
				SeekCore( numBytes, System.IO.SeekOrigin.Current );
			}

			//if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled( EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer ))
			//	FrameworkEventSource.Log.ThreadTransferSend( (long) (asyncResult.OverLapped), 2, string.Empty, false );

			// queue an async ReadFile operation and pass in a packed overlapped
			int hr = 0;
			//int r = ReadFileNative( _handle, bytes, offset, numBytes, intOverlapped, out hr );
			int r = Read( bytes, offset, numBytes );
			// ReadFile, the OS version, will return 0 on failure.  But
			// my ReadFileNative wrapper returns -1.  My wrapper will return
			// the following:
			// On error, r==-1.
			// On async requests that are still pending, r==-1 w/ hr==ERROR_IO_PENDING
			// on async requests that completed sequentially, r==0
			// You will NEVER RELIABLY be able to get the number of bytes
			// read back from this call when using overlapped structures!  You must
			// not pass in a non-null lpNumBytesRead to ReadFile when using 
			// overlapped structures!  This is by design NT behavior.
			if (r == -1 && numBytes != -1)
			{
				// For pipes, when they hit EOF, they will come here.
				if (hr == ERROR_BROKEN_PIPE)
				{
					// Not an error, but EOF.  AsyncFSCallback will NOT be 
					// called.  Call the user callback here.

					// We clear the overlapped status bit for this special case.
					// Failure to do so looks like we are freeing a pending overlapped later.
					intOverlapped->InternalLow = IntPtr.Zero;
					asyncResult.CallUserCallback();
					// EndRead will free the Overlapped struct correctly.
				}
				else if (hr != ERROR_IO_PENDING)
				{
					//if (!_handle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
					if (_handle > 0 && CanSeek)  // Update Position - It could be anywhere.
						SeekCore( 0, System.IO.SeekOrigin.Current );

					if (hr == ERROR_HANDLE_EOF)
						__Error.EndOfFile();
					else
						__Error.WinIOError( hr, string.Empty );
				}
			}
			else
			{
				// Due to a workaround for a race condition in NT's ReadFile & 
				// WriteFile routines, we will always be returning 0 from ReadFileNative
				// when we do async IO instead of the number of bytes read, 
				// irregardless of whether the operation completed 
				// synchronously or asynchronously.  We absolutely must not
				// set asyncResult._numBytes here, since will never have correct
				// results.  
				//Console.WriteLine("ReadFile returned: "+r+" (0x"+Int32.Format(r, "x")+")  The IO completed synchronously, but the user callback was called on a separate thread");
			}

			return asyncResult;
		}
#endif //FEATURE_ASYNC_IO

		[System.Security.SecuritySafeCritical]  // Although the unsafe code is only required in PAL, the block is wide scoped. Leave it here for desktop to ensure it's reviewed.
		public unsafe override int EndRead( IAsyncResult asyncResult )
		{
			// There are 3 significantly different IAsyncResults we'll accept
			// here.  One is from Stream::BeginRead.  The other two are variations
			// on our FileStreamAsyncResult.  One is from BeginReadCore,
			// while the other is from the BeginRead buffering wrapper.
			if (asyncResult == null)
				throw new ArgumentNullException( "asyncResult" );
			Contract.EndContractBlock();

#if FEATURE_ASYNC_IO
			if (!_isAsync)
				return base.EndRead( asyncResult );

			FileStreamAsyncResult afsar = asyncResult as FileStreamAsyncResult;
			if (afsar == null || afsar.IsWrite)
				__Error.WrongAsyncResult();

			// Ensure we can't get into any ----s by doing an interlocked
			// CompareExchange here.  Avoids corrupting memory via freeing the
			// NativeOverlapped class or GCHandle twice.  -- 
			if (1 == Interlocked.CompareExchange( ref afsar._EndXxxCalled, 1, 0 ))
				__Error.EndReadCalledTwice();

			// Obtain the WaitHandle, but don't use public property in case we
			// delay initialize the manual reset event in the future.
			afsar.Wait();

			// Free memory & GC handles.
			afsar.ReleaseNativeResource();

			// Now check for any error during the read.
			if (afsar.ErrorCode != 0)
				__Error.WinIOError( afsar.ErrorCode, string.Empty );

			return afsar.NumBytesRead;
#else
            return base.EndRead(asyncResult);
#endif // FEATURE_ASYNC_IO
		}
 
		// Reads a byte from the file stream.  Returns the byte cast to an int
		// or -1 if reading from the end of the stream. 
		public override int ReadByte()
		{
			//if (_handle.IsClosed) //__Error.FileNotOpen(); 
			if (_handle < 1) __Error.FileNotOpen();

			//if (_readLen==0 && !CanRead) //__Error.ReadNotSupported();
			Contract.Assert( (_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both." );
			if (_readPos == _readLen)
			{
				if (_writePos > 0) FlushWrite( false );
				Contract.Assert( _bufferSize > 0, "_bufferSize > 0" );
				if (_buffer == null) _buffer = new byte[_bufferSize];
				var devicePos1 = GetDevicePosition();
				_readLen = ReadCore( _buffer, 0, _bufferSize, devicePos1 ).Result;
				_readPos = 0;
			}
			if (_readPos == _readLen)
				return -1;

			int result = _buffer[_readPos];
			_readPos++;
			return result;
		}

		[System.Security.SecuritySafeCritical]  // auto-generated
		////[HostProtection( ExternalThreading = true )]
		public override IAsyncResult BeginWrite( byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject )
		{
			if (array == null)
				throw new ArgumentNullException( "array" );
			if (offset < 0)
				throw new ArgumentOutOfRangeException( "offset", "ArgumentOutOfRange_NeedNonNegNum" );
			if (numBytes < 0)
				throw new ArgumentOutOfRangeException( "numBytes", "ArgumentOutOfRange_NeedNonNegNum" );
			if (array.Length - offset < numBytes)
				throw new ArgumentException( "Argument_InvalidOffLen" );
			Contract.EndContractBlock();

			//if (_handle.IsClosed) __Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

#if FEATURE_ASYNC_IO
			if (!_isAsync)
				return base.BeginWrite( array, offset, numBytes, userCallback, stateObject );
			else
				return BeginWriteAsync( array, offset, numBytes, userCallback, stateObject );
#else
            return base.BeginWrite(array, offset, numBytes, userCallback, stateObject);
#endif // FEATURE_ASYNC_IO
		}

#if FEATURE_ASYNC_IO
		[System.Security.SecuritySafeCritical]  // auto-generated
		////[HostProtection( ExternalThreading = true )]
		private FileStreamAsyncResult BeginWriteAsync( byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject )
		{
			Contract.Assert( _isAsync );

			if (!CanWrite) __Error.WriteNotSupported();

			Contract.Assert( (_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both." );

			if (_isPipe)
			{
				// Pipes are ----ed up, at least when you have 2 different pipes
				// that you want to use simultaneously.  When redirecting stdout
				// & stderr with the Process class, it's easy to deadlock your
				// parent & child processes when doing writes 4K at a time.  The
				// OS appears to use a 4K buffer internally.  If you write to a
				// pipe that is full, you will block until someone read from 
				// that pipe.  If you try reading from an empty pipe and 
				// FileStream's BeginRead blocks waiting for data to fill it's 
				// internal buffer, you will be blocked.  In a case where a child
				// process writes to stdout & stderr while a parent process tries
				// reading from both, you can easily get into a deadlock here.
				// To avoid this deadlock, don't buffer when doing async IO on
				// pipes.   
				Contract.Assert( _readPos == 0 && _readLen == 0, "FileStream must not have buffered data here!  Pipes should be unidirectional." );

				if (_writePos > 0)
					FlushWrite( false );

				return BeginWriteCore( array, offset, numBytes, userCallback, stateObject );
			}

			// Handle buffering.
			if (_writePos == 0)
			{
				if (_readPos < _readLen) FlushRead();
				_readPos = 0;
				_readLen = 0;
			}

			int n = _bufferSize - _writePos;
			if (numBytes <= n)
			{
				if (_writePos == 0) _buffer = new byte[_bufferSize];
				//Buffer.InternalBlockCopy( array, offset, _buffer, _writePos, numBytes );
				Buffer.BlockCopy( array, offset, _buffer, _writePos, numBytes );
				_writePos += numBytes;

				// Return a synchronous FileStreamAsyncResult
				return FileStreamAsyncResult.CreateBufferedReadResult( numBytes, userCallback, stateObject, true );
			}

			if (_writePos > 0)
				FlushWrite( false );

			return BeginWriteCore( array, offset, numBytes, userCallback, stateObject );
		}
#endif // FEATURE_ASYNC_IO

#if FEATURE_ASYNC_IO
		[System.Security.SecuritySafeCritical]  // auto-generated
		//[ResourceExposure( ResourceScope.None )]
		//[ResourceConsumption( ResourceScope.AppDomain, ResourceScope.AppDomain )]
		unsafe private FileStreamAsyncResult BeginWriteCore( byte[] bytes, int offset, int numBytes, AsyncCallback userCallback, Object stateObject )
		{
			//Contract.Assert( !_handle.IsClosed, "!_handle.IsClosed" );
			Contract.Assert( CanWrite, "CanWrite" );
			Contract.Assert( bytes != null, "bytes != null" );
			Contract.Assert( _readPos == _readLen, "_readPos == _readLen" );
			Contract.Assert( _isAsync, "BeginWriteCore doesn't work on synchronous file streams!" );
			Contract.Assert( offset >= 0, "offset is negative" );
			Contract.Assert( numBytes >= 0, "numBytes is negative" );

			// Create and store async stream class library specific data in the async result
			FileStreamAsyncResult asyncResult = new FileStreamAsyncResult( 0, bytes, _handle, userCallback, stateObject, true );
			NativeOverlapped* intOverlapped = asyncResult.OverLapped;

			if (CanSeek)
			{
				// Make sure we set the length of the file appropriately.
				long len = Length;
				//Console.WriteLine("BeginWrite - Calculating end pos.  pos: "+pos+"  len: "+len+"  numBytes: "+numBytes);

				// Make sure we are writing to the position that we think we are
				if (_exposedHandle)
					VerifyOSHandlePosition();

				if (_pos + numBytes > len)
				{
					//Console.WriteLine("BeginWrite - Setting length to: "+(pos + numBytes));
					SetLengthCore( _pos + numBytes );
				}

				// Now set the position to read from in the NativeOverlapped struct
				// For pipes, we should leave the offset fields set to 0.
				intOverlapped->OffsetLow = (int) _pos;
				intOverlapped->OffsetHigh = (int) (_pos >> 32);

				// When using overlapped IO, the OS is not supposed to 
				// touch the file pointer location at all.  We will adjust it 
				// ourselves.  This isn't threadsafe.

				// 







				SeekCore( numBytes, System.IO.SeekOrigin.Current );
			}

			//Console.WriteLine("BeginWrite finishing.  pos: "+pos+"  numBytes: "+numBytes+"  _pos: "+_pos+"  Position: "+Position);
			//if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled( EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer ))
			//	FrameworkEventSource.Log.ThreadTransferSend( (long) (asyncResult.OverLapped), 2, string.Empty, false );

			int hr = 0;
			// queue an async WriteFile operation and pass in a packed overlapped
			//int r = WriteFileNative( _handle, bytes, offset, numBytes, intOverlapped, out hr );
			int r = 0; Write( bytes, offset, numBytes );

			// WriteFile, the OS version, will return 0 on failure.  But
			// my WriteFileNative wrapper returns -1.  My wrapper will return
			// the following:
			// On error, r==-1.
			// On async requests that are still pending, r==-1 w/ hr==ERROR_IO_PENDING
			// On async requests that completed sequentially, r==0
			// You will NEVER RELIABLY be able to get the number of bytes
			// written back from this call when using overlapped IO!  You must
			// not pass in a non-null lpNumBytesWritten to WriteFile when using 
			// overlapped structures!  This is ByDesign NT behavior.
			if (r == -1 && numBytes != -1)
			{
				//Console.WriteLine("WriteFile returned 0;  Write will complete asynchronously (if hr==3e5)  hr: 0x{0:x}", hr);

				// For pipes, when they are closed on the other side, they will come here.
				if (hr == ERROR_NO_DATA)
				{
					// Not an error, but EOF.  AsyncFSCallback will NOT be 
					// called.  Call the user callback here.
					asyncResult.CallUserCallback();
					// EndWrite will free the Overlapped struct correctly.
				}
				else if (hr != ERROR_IO_PENDING)
				{
					//if (!_handle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
					if (_handle > 0 && CanSeek)  // Update Position - It could be anywhere.
						SeekCore( 0, System.IO.SeekOrigin.Current );

					if (hr == ERROR_HANDLE_EOF)
						__Error.EndOfFile();
					else
						__Error.WinIOError( hr, string.Empty );
				}
			}
			else
			{
				// Due to a workaround for a race condition in NT's ReadFile & 
				// WriteFile routines, we will always be returning 0 from WriteFileNative
				// when we do async IO instead of the number of bytes written, 
				// irregardless of whether the operation completed 
				// synchronously or asynchronously.  We absolutely must not
				// set asyncResult._numBytes here, since will never have correct
				// results.  
				//Console.WriteLine("WriteFile returned: "+r+" (0x"+Int32.Format(r, "x")+")  The IO completed synchronously, but the user callback was called on another thread.");
			}

			return asyncResult;
		}
#endif //FEATURE_ASYNC_IO

		[System.Security.SecuritySafeCritical]  // Although the unsafe code is only required in PAL, the block is wide scoped. Leave it here for desktop to ensure it's reviewed.
		public unsafe override void EndWrite( IAsyncResult asyncResult )
		{
			if (asyncResult == null)
				throw new ArgumentNullException( "asyncResult" );
			Contract.EndContractBlock();

#if FEATURE_ASYNC_IO
			if (!_isAsync)
			{
				base.EndWrite( asyncResult );
				return;
			}

			FileStreamAsyncResult afsar = asyncResult as FileStreamAsyncResult;
			if (afsar == null || !afsar.IsWrite)
				__Error.WrongAsyncResult();

			// Ensure we can't get into any ----s by doing an interlocked
			// CompareExchange here.  Avoids corrupting memory via freeing the
			// NativeOverlapped class or GCHandle twice.  -- 
			if (1 == Interlocked.CompareExchange( ref afsar._EndXxxCalled, 1, 0 ))
				__Error.EndWriteCalledTwice();

			// Obtain the WaitHandle, but don't use public property in case we
			// delay initialize the manual reset event in the future.
			afsar.Wait();

			// Free memory & GC handles.
			afsar.ReleaseNativeResource();

			// Now check for any error during the write.
			if (afsar.ErrorCode != 0)
				__Error.WinIOError( afsar.ErrorCode, string.Empty );

			// Number of bytes written is afsar._numBytes + afsar._numBufferedBytes.
			return;
#else
            base.EndWrite(asyncResult);
#endif // FEATURE_ASYNC_IO
		}
 
		// Writes a single byte as efficiently as possible
		[System.Security.SecuritySafeCritical]  // auto-generated
		public override void WriteByte( byte value )
		{
			//if (_handle.IsClosed) __Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();
			if (_writePos == 0)
			{
				if (!CanWrite) __Error.WriteNotSupported();
				if (_readPos < _readLen) FlushRead();
				_readPos = 0;
				_readLen = 0;
				Contract.Assert( _bufferSize > 0, "_bufferSize > 0" );
				if (_buffer == null) _buffer = new byte[_bufferSize];
			}
			if (_writePos == _bufferSize)
				FlushWrite( false );

			_buffer[_writePos] = value;
			_writePos++;
		}

#if FEATURE_ASYNC_IO
		////[HostProtection( ExternalThreading = true )]
		[ComVisible( false )]
		[SecuritySafeCritical]
		public override Task<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
		{
			if (buffer == null)
				throw new ArgumentNullException( "buffer" );
			if (offset < 0)
				throw new ArgumentOutOfRangeException( "offset", "ArgumentOutOfRange_NeedNonNegNum" );
			if (count < 0)
				throw new ArgumentOutOfRangeException( "count", "ArgumentOutOfRange_NeedNonNegNum" );
			if (buffer.Length - offset < count)
				throw new ArgumentException( "Argument_InvalidOffLen" );
			Contract.EndContractBlock();

			// If we have been inherited into a subclass, the following implementation could be incorrect
			// since it does not call through to Read() or BeginRead() which a subclass might have overriden.  
			// To be safe we will only use this implementation in cases where we know it is safe to do so,
			// and delegate to our base class (which will call into Read/BeginRead) when we are not sure.
			if (this.GetType() != typeof( FileStream ))
				return base.ReadAsync( buffer, offset, count, cancellationToken );

			cancellationToken.ThrowIfCancellationRequested();
			//if (cancellationToken.IsCancellationRequested)
			//	return Task.FromCancellation<int>( cancellationToken );

			//if (_handle.IsClosed) __Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

			// If async IO is not supported on this platform or 
			// if this FileStream was not opened with FileOptions.Asynchronous.
			if (!_isAsync)
				return base.ReadAsync( buffer, offset, count, cancellationToken );

			// At core, Unosys is asynchronous, so we can just use our base Read method here:
			//return this.ReadCore( buffer, offset, count );
			// Not quite, we still need to handle the FileStream class implementation just like Read does:
			// if (_handle.IsClosed) //__Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

			Contract.Assert( (_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both." );

			bool isBlocked = false;
			int n = _readLen - _readPos;
			// if the read buffer is empty, read into either user's array or our 
			// buffer, depending on number of bytes user asked for and buffer size.
			if (n == 0)
			{
				if (!CanRead) __Error.ReadNotSupported();
				if (_writePos > 0) FlushWrite( false );
				if (!CanSeek || (count >= _bufferSize))
				{
					var result = ReadCore( buffer, offset, count, (ulong) this.Position );
					// Throw away read buffer.
					_readPos = 0;
					_readLen = 0;
					return result;
				}

				if (_buffer == null) _buffer = new byte[_bufferSize];
				var devicePos1 = GetDevicePosition();
				n = ReadCore( _buffer, 0, _bufferSize, devicePos1 ).Result;
				if (n == 0) return Task.FromResult<int>( 0 );
				isBlocked = n < _bufferSize;
				_readPos = 0;
				_readLen = n;
			}

			// Now copy min of count or numBytesAvailable (ie, near EOF) to array.
			if (n > count) n = count;
			Buffer.BlockCopy( _buffer, _readPos, buffer, offset, n );
			_readPos += n;

			// We may have read less than the number of bytes the user asked 
			// for, but that is part of the Stream contract.  Reading again for
			// more data may cause us to block if we're using a device with 
			// no clear end of file, such as a serial port or pipe.  If we 
			// blocked here & this code was used with redirected pipes for a
			// process's standard output, this can lead to deadlocks involving 
			// two processes. But leave this here for files to avoid what would
			// probably be a breaking change.         --

			// If we are reading from a device with no clear EOF like a 
			// serial port or a pipe, this will cause us to block incorrectly.
			if (!_isPipe)
			{
				// If we hit the end of the buffer and didn't have enough bytes, we must 
				// read some more from the underlying stream.  However, if we got
				// fewer bytes from the underlying stream than we asked for (ie, we're 
				// probably blocked), don't ask for more bytes.
				if (n < count && !isBlocked)
				{
					Contract.Assert( _readPos == _readLen, "Read buffer should be empty!" );
					var readMoreBytes = ReadCore( buffer, offset + n, count - n, n, (ulong) this.Position );
					// We've just made our buffer inconsistent with our position 
					// pointer.  We must throw away the read buffer. 
					_readPos = 0;
					_readLen = 0;
					return readMoreBytes;
				}
			}

			return Task.FromResult<int>( n );
		}

		////[HostProtection( ExternalThreading = true )]
		[ComVisible( false )]
		[SecuritySafeCritical]
		public override Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
		{
			if (buffer == null)
				throw new ArgumentNullException( "buffer" );
			if (offset < 0)
				throw new ArgumentOutOfRangeException( "offset", "ArgumentOutOfRange_NeedNonNegNum" );
			if (count < 0)
				throw new ArgumentOutOfRangeException( "count", "ArgumentOutOfRange_NeedNonNegNum" );
			if (buffer.Length - offset < count)
				throw new ArgumentException( "Argument_InvalidOffLen" );
			Contract.EndContractBlock();

			// If we have been inherited into a subclass, the following implementation could be incorrect
			// since it does not call through to Write() or BeginWrite() which a subclass might have overriden.  
			// To be safe we will only use this implementation in cases where we know it is safe to do so,
			// and delegate to our base class (which will call into Write/BeginWrite) when we are not sure.
			if (this.GetType() != typeof( FileStream ))
				return base.WriteAsync( buffer, offset, count, cancellationToken );

			//if (cancellationToken.IsCancellationRequested)
			//	return Task.FromCancellation(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();

			//if (_handle.IsClosed) __Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

			// If async IO is not supported on this platform or 
			// if this FileStream was not opened with FileOptions.Asynchronous.
			if (!_isAsync)
				return base.WriteAsync( buffer, offset, count, cancellationToken );

			// At core, Unosys is asynchronous, so we can just use our base Write method here:
			//return this.WriteCore( buffer, offset, count );
			// Not quite, we still need to handle the FileStream class implementation just like Write does:
			if (_writePos == 0)
			{
				// Ensure we can write to the stream, and ready buffer for writing.
				if (!CanWrite) //__Error.WriteNotSupported(); 
					if (_readPos < _readLen) FlushRead();
				_readPos = 0;
				_readLen = 0;
			}

			// If our buffer has data in it, copy data from the user's array into
			// the buffer, and if we can fit it all there, return.  Otherwise, write
			// the buffer to disk and copy any remaining data into our buffer.
			// The assumption here is memcpy is cheaper than disk (or net) IO. 
			// (10 milliseconds to disk vs. ~20-30 microseconds for a 4K memcpy)
			// So the extra copying will reduce the total number of writes, in 
			// non-pathological cases (ie, write 1 byte, then write for the buffer 
			// size repeatedly)
			if (_writePos > 0)
			{
				int numBytes = _bufferSize - _writePos;   // space left in buffer
				if (numBytes > 0)
				{
					if (numBytes > count)
						numBytes = count;
					Buffer.BlockCopy( buffer, offset, _buffer, _writePos, numBytes );
					_writePos += numBytes;
					if (count == numBytes) return Task.FromResult<int>( numBytes );
					offset += numBytes;
					count -= numBytes;
				}

				// Reset our buffer.  We essentially want to call FlushWrite
				// without calling Flush on the underlying Stream.
				int written = WriteCore( _buffer, 0, _writePos ).Result;
				_writePos = 0;
			}

			// If the buffer would slow writes down, avoid buffer completely. 
			if (count >= _bufferSize)
			{
				Contract.Assert( _writePos == 0, "FileStream cannot have buffered data to write here!  Your stream will be corrupted." );
				return WriteCore( buffer, offset, count );
			}
			else if (count == 0)
				return Task.FromResult<int>( 0 );  // Don't allocate a buffer then call memcpy for 0 bytes.

			if (_buffer == null) _buffer = new byte[_bufferSize];
			// Copy remaining bytes into buffer, to write at a later date. 
			Buffer.BlockCopy( buffer, offset, _buffer, _writePos, count );
			_writePos = count;
			return Task.FromResult<int>( count );
		}

		/* Unneeded WriteAsync cruft
		private int Writing()
		{
			return 0;
		}

		private void CancellationRoutine( object obj )
		{
		}

		// The task instance returned from ReadAsync and WriteAsync.
		// Also stores all of the state necessary for those calls to avoid closures and extraneous delegate allocations.
		private sealed class FileStreamReadWriteTask<T> : Task<T>
		{
			internal CancellationToken _cancellationToken;
			internal CancellationTokenRegistration _registration;
			internal FileStreamAsyncResult _asyncResult; // initialized after Begin call completes

			// NOTE: This implementation relied heavily upon this:
			// internal Task() : base() { } // Construct a promise-style task without any options
			// Which is obviously not visible from here
			// I've added Func<T> function to the signature here to fix that, maybe it'll even work
			internal FileStreamReadWriteTask( Func<T> function, CancellationToken cancellationToken )
				: base( function )
			{
				_cancellationToken = cancellationToken;
			}
		}

		// Completion callback for WriteAsync
		[SecuritySafeCritical]
		private static void EndWriteTask( IAsyncResult iar )
		{
			var asyncResult = iar as FileStreamAsyncResult;
			Contract.Assert( asyncResult != null );
			Contract.Assert( asyncResult.IsCompleted, "How can we end up in the completion callback if the IAsyncResult is not completed?" );

			//var writeTask = iar.AsyncState as FileStreamReadWriteTask<VoidTaskResult>;
			var writeTask = iar.AsyncState as FileStreamReadWriteTask<int>;
			Contract.Assert( writeTask != null );

			try
			{
			if (asyncResult.IsAsync)
			{
				asyncResult.ReleaseNativeResource();

				// release the resource held by CancellationTokenRegistration
				writeTask._registration.Dispose();
			}

			if (asyncResult.ErrorCode == Win32Native.ERROR_OPERATION_ABORTED)
			{
				var cancellationToken = writeTask._cancellationToken;
				Contract.Assert(cancellationToken.IsCancellationRequested, "How can the IO operation be aborted if cancellation was not requested?");
				writeTask.TrySetCanceled(cancellationToken);
			}
			else
				writeTask.TrySetResult(default(VoidTaskResult));
			}
			catch (Exception ex)
			{
				writeTask.TrySetException(ex);
			}
		}

		// Cancellation callback for both ReadAsync and WriteAsync.
		[SecuritySafeCritical]
		private static void CancelTask<T>( object state )
		{
			var task = state as FileStreamReadWriteTask<T>;
			Contract.Assert( task != null );
			FileStreamAsyncResult asyncResult = task._asyncResult;

			// This method is used as both the completion callback and the cancellation callback.
			// We should try to cancel the operation if this is running as the completion callback
			// or if cancellation is not applicable:
			// 1. asyncResult is not a FileStreamAsyncResult
			// 2. asyncResult.IsAsync is false: asyncResult is a "synchronous" FileStreamAsyncResult.
			// 3. The asyncResult is completed: this should never happen.
			//Contract.Assert((!asyncResult.IsWrite && typeof(T) == typeof(int)) ||
			//				(asyncResult.IsWrite && typeof(T) == typeof(VoidTaskResult)));
			Contract.Assert( asyncResult != null );
			Contract.Assert( asyncResult.IsAsync );

			//try
			//{
			// Cancel the overlapped read and set the task to cancelled state.
			if (!asyncResult.IsCompleted)
				asyncResult.Cancel();
            }
            catch (Exception ex)
            {
                task.TrySetException(ex);
            }
		}*/

		/* Unneeded ReadAsync cruft
		private int Reading()
		{
			return 0;
		}
		
		// Completion callback for ReadAsync
		[SecuritySafeCritical]
		private static void EndReadTask( IAsyncResult iar )
		{
			FileStreamAsyncResult asyncResult = iar as FileStreamAsyncResult;
			Contract.Assert( asyncResult != null );
			Contract.Assert( asyncResult.IsCompleted, "How can we end up in the completion callback if the IAsyncResult is not completed?" );

			var readTask = asyncResult.AsyncState as FileStreamReadWriteTask<int>;
			Contract.Assert( readTask != null );

			try
			{
			if (asyncResult.IsAsync)
			{
				asyncResult.ReleaseNativeResource();

				// release the resource held by CancellationTokenRegistration
				readTask._registration.Dispose();
			}

			if (asyncResult.ErrorCode == Win32Native.ERROR_OPERATION_ABORTED)
			{
				var cancellationToken = readTask._cancellationToken;
				Contract.Assert( cancellationToken.IsCancellationRequested, "How can the IO operation be aborted if cancellation was not requested?" );
				//readTask.TrySetCanceled(cancellationToken);
				throw new Exception( "Read Task aborted - Dunno how this happened" );
			}
			//else
			//	readTask.TrySetResult( asyncResult.NumBytesRead );
			}
			catch (Exception ex)
			{
				readTask.TrySetException(ex);
			}
		}*/
#endif //FEATURE_ASYNC_IO
		#endregion

		#region Unimplemented Public Members
		/// <summary>
		/// Methods of FileStream that were not given a working concrete implementation for Unosys
		/// </summary>
#if UNIMPLEMENTED_MEMBERS
		public virtual Microsoft.Win32.SafeHandles.SafeFileHandle SafeFileHandle
		{
			[SecurityCriticalAttribute]
			[SecurityPermissionAttribute( SecurityAction.InheritanceDemand,
				Flags = SecurityPermissionFlag.UnmanagedCode )]
			get
			{
				throw new NotImplementedException( "Unosys does not provide FileStream.SafeFileHandle" );
				/*Flush();
				// Explicitly dump any buffered data, since the user could move our
				// position or write to the file.
				_readPos = 0;
				_readLen = 0;
				_writePos = 0;
				_exposedHandle = true;

				return _handle;*/
			}
		}

#if FEATURE_MACL
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public FileSecurity GetAccessControl()
        {
            if (_handle.IsClosed) __Error.FileNotOpen();
            return new FileSecurity(_handle, _fileName, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }
 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetAccessControl(FileSecurity fileSecurity)
        {
            if (fileSecurity == null)
                throw new ArgumentNullException("fileSecurity");
            Contract.EndContractBlock();
 
            if (_handle.IsClosed) __Error.FileNotOpen();
 
            fileSecurity.Persist(_handle, _fileName);
        }
#else
		public System.Security.AccessControl.FileSecurity GetAccessControl()
		{
			throw new NotImplementedException( "Unosys does not provide FileStream.GetAccessControl" );
		}

		public void SetAccessControl( System.Security.AccessControl.FileSecurity fileSecurity )
		{
			throw new NotImplementedException( "Unosys does not provide FileStream.SetAccessControl" );
		}
#endif

		[System.Security.SecuritySafeCritical]  // auto-generated
		public virtual void Lock( long position, long length )
		{
			if (position < 0 || length < 0)
				throw new ArgumentOutOfRangeException( (position < 0 ? "position" : "length"), "ArgumentOutOfRange_NeedNonNegNum" );
			Contract.EndContractBlock();
			//if (_handle.IsClosed) __Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

			int positionLow = unchecked( (int) (position) );
			int positionHigh = unchecked( (int) (position >> 32) );
			int lengthLow = unchecked( (int) (length) );
			int lengthHigh = unchecked( (int) (length >> 32) );

			//if (!Win32Native.LockFile( _handle, positionLow, positionHigh, lengthLow, lengthHigh ))
				//__Error.WinIOError();
			throw new NotImplementedException( "Unosys does not provide FileStream.Lock" );
		}

		[System.Security.SecuritySafeCritical]  // auto-generated
		public virtual void Unlock( long position, long length )
		{
			if (position < 0 || length < 0)
				throw new ArgumentOutOfRangeException( (position < 0 ? "position" : "length"),  "ArgumentOutOfRange_NeedNonNegNum" );
			Contract.EndContractBlock();
			//if (_handle.IsClosed) __Error.FileNotOpen();
			if (_handle < 1) __Error.FileNotOpen();

			int positionLow = unchecked( (int) (position) );
			int positionHigh = unchecked( (int) (position >> 32) );
			int lengthLow = unchecked( (int) (length) );
			int lengthHigh = unchecked( (int) (length >> 32) );

			//if (!Win32Native.UnlockFile( _handle, positionLow, positionHigh, lengthLow, lengthHigh ))
				//__Error.WinIOError();
			throw new NotImplementedException( "Unosys does not provide FileStream.Unlock" );
		}
#endif
		#endregion

		#region Newly Added Public Members
		public override void Close()
		{
			//base.Close();  // not using base class Stream at all
			if (_handle != -1)
			{
				bool result = API.CloseSetAsync( _handle ).Result;
				// Either way, we want to stop using this handle: It was invalid on call, or it's closed and now invalid
				_handle = -1;
				if (!result )
				{
					throw new ArgumentException( "Invalid handle" );
				}
			}
		}
		#endregion

		#region Helpers
		// Reading is done by blocks from the file, but someone could read
		// 1 byte from the buffer then write.  At that point, the OS's file
		// pointer is out of [....] with the stream's position.  All write 
		// functions should call this function to preserve the position in the file.
		private void FlushRead()
		{
			Contract.Assert( _writePos == 0, "FileStream: Write buffer must be empty in FlushRead!" );
			if (_readPos - _readLen != 0)
			{
				Contract.Assert( CanSeek, "FileStream will lose buffered read data now." );
				SeekCore( _readPos - _readLen, System.IO.SeekOrigin.Current );
			}
			_readPos = 0;
			_readLen = 0;
		}

		// Writes are buffered.  Anytime the buffer fills up 
		// (_writePos + delta > _bufferSize) or the buffer switches to reading
		// and there is left over data (_writePos > 0), this function must be called. 
		private void FlushWrite( bool calledFromFinalizer )
		{
			Contract.Assert( _readPos == 0 && _readLen == 0, "FileStream: Read buffer must be empty in FlushWrite!" );

			int written = WriteCore( _buffer, 0, _writePos ).Result;
			_writePos = 0;
		}

		// We absolutely need this method broken out so that BeginWriteCore can call 
		// a method without having to go through buffering code that might call 
		// FlushWrite.
		private void SetLengthCore( long value )
		{
			Contract.Assert( value >= 0, "value >= 0" );
			long origPos = _pos;

			if (_exposedHandle)
				VerifyOSHandlePosition();
			if (_pos != value)
				SeekCore( value, System.IO.SeekOrigin.Begin );
			//if (!Win32Native.SetEndOfFile(_handle)) {
			//	int hr = Marshal.GetLastWin32Error();
			//	//if (hr==//__Error.ERROR_INVALID_PARAMETER)
			//	  //  throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_FileLengthTooBig")); 
			//	//__Error.WinIOError(hr, string.Empty);
			//} 
			// Return file pointer to where it was before setting length 
			if (origPos != value)
			{
				if (origPos < value)
					SeekCore( origPos, System.IO.SeekOrigin.Begin );
				else
					SeekCore( 0, System.IO.SeekOrigin.End );
			}
		}

		// An additional version of ReadCore that understands that it's working on a multiple read process
		private async Task<int> ReadCore( byte[] buffer, int offset, int count, int alreadyRead, ulong deviceOffsetToReadFrom )
		{
			int r = await API.ReadSetAsync( _handle, buffer, offset, count, deviceOffsetToReadFrom );
			_pos += r;
			return r + alreadyRead;
		}

		private async Task<int> ReadCore( byte[] buffer, int offset, int count, ulong deviceOffsetToReadFrom )
		{
			//Contract.Assert(!_handle.IsClosed, "!_handle.IsClosed"); 
			//Contract.Assert(CanRead, "CanRead");

			//Contract.Assert(buffer != null, "buffer != null");
			//Contract.Assert(_writePos == 0, "_writePos == 0"); 
			//Contract.Assert(offset >= 0, "offset is negative");
			//Contract.Assert(count >= 0, "count is negative"); 
			int r = await API.ReadSetAsync( _handle, buffer, offset, count, deviceOffsetToReadFrom );

			//// Make sure we are reading from the right spot 
			//if (_exposedHandle)
			//	VerifyOSHandlePosition(); 

			//int hr = 0;
			//int r = ReadFileNative(_handle, buffer, offset, count, null, out hr);
			//if (r == -1) { 
			//	// For pipes, ERROR_BROKEN_PIPE is the normal end of the pipe.
			//	if (hr == ERROR_BROKEN_PIPE) { 
			//		r = 0; 
			//	}
			//	else { 
			//		if (hr == ERROR_INVALID_PARAMETER)
			//			throw new ArgumentException(Environment.GetResourceString("Arg_HandleNotSync"));

			//		//__Error.WinIOError(hr, string.Empty); 
			//	}
			//} 
			//Contract.Assert(r >= 0, "FileStream's ReadCore is likely broken."); 
			_pos += r;

			return r;
		}

		// This doesn't do argument checking.  Necessary for SetLength, which must
		// set the file pointer beyond the end of the file. This will update the
		// internal position
		private long SeekCore( long offset, System.IO.SeekOrigin origin )
		{
			//Contract.Assert(!_handle.IsClosed && CanSeek, "!_handle.IsClosed && CanSeek"); 
			//Contract.Assert(origin>=SeekOrigin.Begin && origin<=SeekOrigin.End, "origin>=SeekOrigin.Begin && origin<=SeekOrigin.End"); 
			// int hr = 0;
			long ret = 0;

			//ret = Win32Native.SetFilePointer(_handle, offset, origin, out hr);
			var curFileLength = this.Length;
			if ((origin == System.IO.SeekOrigin.Begin && offset < 0) ||
				(origin == System.IO.SeekOrigin.Current && offset < 0 && Math.Abs( offset ) > _pos) ||
				(origin == System.IO.SeekOrigin.End && offset < 0 && Math.Abs( offset ) > curFileLength))
				throw new ArgumentException( "Seeking before the beginning of the stream is not supported", "offset" );

			if (origin == System.IO.SeekOrigin.Begin)
				ret = offset;
			else if (origin == System.IO.SeekOrigin.Current)
				ret = _pos + offset;
			else
				ret = curFileLength + offset;

			// Here's where we make the file longer if needed
			/*if (ret > curFileLength)
			{
				// TODO: Determine if anything is needed here, perhaps a Write operation?
				// Or maybe that's already handled by writing to a file location that doesn't exist?
			}*/

			//if (ret == -1) {
			//	// For invalid handles, detect the error and mark our handle 
			//	// as closed to give slightly better error messages.  Also
			//	// help ensure we avoid handle recycling bugs. 
			//	if (hr == Win32Native.ERROR_INVALID_HANDLE) 
			//		_handle.SetHandleAsInvalid();
			//	//__Error.WinIOError(hr, string.Empty); 
			//}

			_pos = ret;
			return ret;
		}

		internal string NameInternal
		{
			get
			{
				if (_fileName == null)
					return "<unknownfilename>";
				return _fileName;
			}
		}

		private async Task<int> WriteCore( byte[] buffer, int offset, int count )
		{
			ulong deviceOffsetToWriteTo = (ulong) _pos;	// Current location of the file pointer
			int w = await API.WriteSetAsync( _handle, buffer, offset, count, deviceOffsetToWriteTo );
			_pos += w;  // advance the current position in the stream  - RdS
			return w;
		}

		private void VerifyOSHandlePosition()
		{
			if (!CanSeek)
				return;

			// SeekCore will override the current _pos, so save it now 
			long oldPos = _pos;
			long curPos = SeekCore( 0, System.IO.SeekOrigin.Current );

			if (curPos != oldPos)
			{
				// For reads, this is non-fatal but we still could have returned corrupted
				// data in some cases. So discard the internal buffer. Potential MDA
				_readPos = 0;
				_readLen = 0;
				if (_writePos > 0)
				{
					// Discard the buffer and let the user know! 
					_writePos = 0;
					throw new IOException( "IO.IO_FileStreamHandlePosition" );
				}
			}
		}
		#endregion
	}
}