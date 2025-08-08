// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  __Error
** 
** <OWNER>Microsoft</OWNER>
**
**
** Purpose: Centralized error methods for the IO package.  
** Mostly useful for translating Win32 HRESULTs into meaningful
** error strings & exceptions.
**
**
===========================================================*/

using System;
using System.Runtime.InteropServices;
//using Win32Native = Microsoft.Win32.Win32Native;
using System.Text;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics.Contracts;

namespace Unosys.SDK
{
	[Pure]
	internal static class __Error
	{
		internal static void EndOfFile()
		{
			throw new System.IO.EndOfStreamException( "IO.EOF_ReadBeyondEOF" );
		}

		internal static void FileNotOpen()
		{
			throw new ObjectDisposedException( null, "ObjectDisposed_FileClosed" );
		}

		internal static void StreamIsClosed()
		{
			throw new ObjectDisposedException( null, "ObjectDisposed_StreamClosed" );
		}

		internal static void MemoryStreamNotExpandable()
		{
			throw new NotSupportedException( "NotSupported_MemStreamNotExpandable" );
		}

		internal static void ReaderClosed()
		{
			throw new ObjectDisposedException( null, "ObjectDisposed_ReaderClosed" );
		}

		internal static void ReadNotSupported()
		{
			throw new NotSupportedException( "NotSupported_UnreadableStream" );
		}

		internal static void SeekNotSupported()
		{
			throw new NotSupportedException( "NotSupported_UnseekableStream" );
		}

		internal static void WrongAsyncResult()
		{
			throw new ArgumentException( "Arg_WrongAsyncResult" );
		}

		internal static void EndReadCalledTwice()
		{
			// Should ideally be InvalidOperationExc but we can't maitain parity with Stream and FileStream without some work
			throw new ArgumentException( "InvalidOperation_EndReadCalledMultiple" );
		}

		internal static void EndWriteCalledTwice()
		{
			// Should ideally be InvalidOperationExc but we can't maintain parity with Stream and FileStream without some work
			throw new ArgumentException( "InvalidOperation_EndWriteCalledMultiple" );
		}

		// Given a possible fully qualified path, ensure that we have path
		// discovery permission to that path.  If we do not, return just the 
		// file name.  If we know it is a directory, then don't return the 
		// directory name.
		[System.Security.SecurityCritical]  // auto-generated
		internal static String GetDisplayablePath( String path, bool isInvalidPath )
		{
			if (String.IsNullOrEmpty( path ))
				return String.Empty;

			if (path.Length < 2)
				return path;

			// Return the path as is if we're relative (not fully qualified) and not a bad path
			//if (PathInternal.IsPartiallyQualified( path ) && !isInvalidPath)
				//return path;
			
			bool safeToReturn = false;
			/*try
			{
				if (!isInvalidPath)
				{
#if !FEATURE_CORECLR
					FileIOPermission.QuickDemand( FileIOPermissionAccess.PathDiscovery, path, false, false );
#endif
					safeToReturn = true;
				}
			}
			catch (SecurityException)
			{
			}
			catch (ArgumentException)
			{
				// ? and * characters cause ArgumentException to be thrown from HasIllegalCharacters
				// inside FileIOPermission.AddPathList
			}
			catch (NotSupportedException)
			{
				// paths like "!Bogus\\dir:with/junk_.in it" can cause NotSupportedException to be thrown
				// from Security.Util.StringExpressionSet.CanonicalizePath when ':' is found in the path
				// beyond string index position 1.  
			}*/

			if (!safeToReturn)
			{
				if (Path.IsDirectorySeparator( path[path.Length - 1] ))
					path = "IO.IO_NoPermissionToDirectoryName";
				else
					path = Path.GetFileName( path );
			}

			return path;
		}
		
		[System.Security.SecuritySafeCritical]  // auto-generated
		internal static void WinIOError()
		{
			int errorCode = Marshal.GetLastWin32Error();
			WinIOError( errorCode, String.Empty );
		}

		// After calling GetLastWin32Error(), it clears the last error field,
		// so you must save the HResult and pass it to this method.  This method
		// will determine the appropriate exception to throw dependent on your 
		// error, and depending on the error, insert a string into the message 
		// gotten from the ResourceManager.
		[System.Security.SecurityCritical]  // auto-generated
		internal static void WinIOError( int errorCode, String maybeFullPath )
		{
			// This doesn't have to be perfect, but is a perf optimization.
			bool isInvalidPath = errorCode == Win32Native.ERROR_INVALID_NAME || errorCode == Win32Native.ERROR_BAD_PATHNAME;
			String str = GetDisplayablePath( maybeFullPath, isInvalidPath );

			switch (errorCode)
			{
				case Win32Native.ERROR_FILE_NOT_FOUND:
					if (str.Length == 0)
						throw new System.IO.FileNotFoundException( "IO.FileNotFound" );
					else
						throw new System.IO.FileNotFoundException( "IO.FileNotFound_FileName", str );

				case Win32Native.ERROR_PATH_NOT_FOUND:
					if (str.Length == 0)
						throw new System.IO.DirectoryNotFoundException( "IO.PathNotFound_NoPathName" );
					else
						throw new System.IO.DirectoryNotFoundException( "IO.PathNotFound_Path" );//, str );

				case Win32Native.ERROR_ACCESS_DENIED:
					if (str.Length == 0)
						throw new UnauthorizedAccessException( "UnauthorizedAccess_IODenied_NoPathName" );
					else
						throw new UnauthorizedAccessException( "UnauthorizedAccess_IODenied_Path" );//, str );

				case Win32Native.ERROR_ALREADY_EXISTS:
					if (str.Length == 0)
						goto default;
					//throw new IOException( "IO.IO_AlreadyExists_Name", str, Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );
					throw new IOException( "IO.IO_AlreadyExists_Name", Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );

				case Win32Native.ERROR_FILENAME_EXCED_RANGE:
					throw new PathTooLongException( "IO.PathTooLong" );

				case Win32Native.ERROR_INVALID_DRIVE:
					throw new System.IO.DriveNotFoundException( "IO.DriveNotFound_Drive" );//, str );

				case Win32Native.ERROR_INVALID_PARAMETER:
					throw new IOException( Win32Native.GetMessage( errorCode ), Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );

				case Win32Native.ERROR_SHARING_VIOLATION:
					if (str.Length == 0)
						throw new IOException( "IO.IO_SharingViolation_NoFileName", Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );
					else
						//throw new IOException( "IO.IO_SharingViolation_File", str, Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );
						throw new IOException( "IO.IO_SharingViolation_File", Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );

				case Win32Native.ERROR_FILE_EXISTS:
					if (str.Length == 0)
						goto default;
					//throw new IOException( "IO.IO_FileExists_Name", str, Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );
					throw new IOException( "IO.IO_FileExists_Name", Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );

				case Win32Native.ERROR_OPERATION_ABORTED:
					throw new OperationCanceledException();

				default:
					throw new IOException( Win32Native.GetMessage( errorCode ), Win32Native.MakeHRFromErrorCode( errorCode ), maybeFullPath );
			}
		}

		// An alternative to WinIOError with friendlier messages for drives
		[System.Security.SecuritySafeCritical]  // auto-generated
		internal static void WinIODriveError( String driveName )
		{
			int errorCode = Marshal.GetLastWin32Error();
			WinIODriveError( driveName, errorCode );
		}

		[System.Security.SecurityCritical]  // auto-generated
		internal static void WinIODriveError( String driveName, int errorCode )
		{
			switch (errorCode)
			{
				case Win32Native.ERROR_PATH_NOT_FOUND:
				case Win32Native.ERROR_INVALID_DRIVE:
					throw new System.IO.DriveNotFoundException( "IO.DriveNotFound_Drive" );//, driveName ) );

				default:
					WinIOError( errorCode, driveName );
					break;
			}
		}

		internal static void WriteNotSupported()
		{
			throw new NotSupportedException( "NotSupported_UnwritableStream" );
		}

		internal static void WriterClosed()
		{
			throw new ObjectDisposedException( null, "ObjectDisposed_WriterClosed" );
		}

		// From WinError.h
		internal const int ERROR_FILE_NOT_FOUND = Win32Native.ERROR_FILE_NOT_FOUND;
		internal const int ERROR_PATH_NOT_FOUND = Win32Native.ERROR_PATH_NOT_FOUND;
		internal const int ERROR_ACCESS_DENIED = Win32Native.ERROR_ACCESS_DENIED;
		internal const int ERROR_INVALID_PARAMETER = Win32Native.ERROR_INVALID_PARAMETER;
	}
}