using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	// File attributes for use with the FileEnumerator class.
	// These constants correspond to the constants in WinNT.h.
	[Serializable]
	[Flags]
	[System.Runtime.InteropServices.ComVisible( true )]
	public enum FileAttributes
	{
		//ErrorNotFound = -1,	// From File.FillAttributeInfo (for backwards compatability?)
		//None = 0,
		// From WinNT.h (FILE_ATTRIBUTE_XXX)
		ReadOnly = 0x1,
		Hidden = 0x2,
		System = 0x4,
		Directory = 0x10,
		Archive = 0x20,
		Device = 0x40,
		Normal = 0x80,
		Temporary = 0x100,
		SparseFile = 0x200,
		ReparsePoint = 0x400,
		Compressed = 0x800,
		Offline = 0x1000,
		NotContentIndexed = 0x2000,
		Encrypted = 0x4000,

#if !FEATURE_CORECLR
#if FEATURE_COMINTEROP
        [System.Runtime.InteropServices.ComVisible(false)]        
#endif // FEATURE_COMINTEROP
		IntegrityStream = 0x8000,

#if FEATURE_COMINTEROP
        [System.Runtime.InteropServices.ComVisible(false)]        
#endif // FEATURE_COMINTEROP
		NoScrubData = 0x20000,
#endif
	}

	// Summary:
	//     Specifies how the operating system should open a file.
	[Serializable]
	//[ComVisible( true )]
	public enum FileMode
	{
		// Summary:
		//     Specifies that the operating system should create a new file. This requires
		//     System.Security.Permissions.FileIOPermissionAccess.Write permission. If the
		//     file already exists, an System.IO.IOException exception is thrown.
		CreateNew = 1,
		//
		// Summary:
		//     Specifies that the operating system should create a new file. If the file
		//     already exists, it will be overwritten. This requires System.Security.Permissions.FileIOPermissionAccess.Write
		//     permission. FileMode.Create is equivalent to requesting that if the file
		//     does not exist, use System.IO.FileMode.CreateNew; otherwise, use System.IO.FileMode.Truncate.
		//     If the file already exists but is a hidden file, an System.UnauthorizedAccessException
		//     exception is thrown.
		Create = 2,
		//
		// Summary:
		//     Specifies that the operating system should open an existing file. The ability
		//     to open the file is dependent on the value specified by the System.IO.FileAccess
		//     enumeration. A System.IO.FileNotFoundException exception is thrown if the
		//     file does not exist.
		Open = 3,
		//
		// Summary:
		//     Specifies that the operating system should open a file if it exists; otherwise,
		//     a new file should be created. If the file is opened with FileAccess.Read,
		//     System.Security.Permissions.FileIOPermissionAccess.Read permission is required.
		//     If the file access is FileAccess.Write, System.Security.Permissions.FileIOPermissionAccess.Write
		//     permission is required. If the file is opened with FileAccess.ReadWrite,
		//     both System.Security.Permissions.FileIOPermissionAccess.Read and System.Security.Permissions.FileIOPermissionAccess.Write
		//     permissions are required.
		OpenOrCreate = 4,
		//
		// Summary:
		//     Specifies that the operating system should open an existing file. When the
		//     file is opened, it should be truncated so that its size is zero bytes. This
		//     requires System.Security.Permissions.FileIOPermissionAccess.Write permission.
		//     Attempts to read from a file opened with FileMode.Truncate cause an System.ArgumentException
		//     exception.
		Truncate = 5,
		//
		// Summary:
		//     Opens the file if it exists and seeks to the end of the file, or creates
		//     a new file. This requires System.Security.Permissions.FileIOPermissionAccess.Append
		//     permission. FileMode.Append can be used only in conjunction with FileAccess.Write.
		//     Trying to seek to a position before the end of the file throws an System.IO.IOException
		//     exception, and any attempt to read fails and throws a System.NotSupportedException
		//     exception.
		Append = 6,
	}


	// Summary:
	//     Defines constants for read, write, or read/write access to a file.
	[Serializable]
	//[ComVisible( true )]
	[Flags]
	public enum FileAccess
	{
		// Summary:
		//     Read access to the file. Data can be read from the file. Combine with Write
		//     for read/write access.
		Read = 1,
		//
		// Summary:
		//     Write access to the file. Data can be written to the file. Combine with Read
		//     for read/write access.
		Write = 2,
		//
		// Summary:
		//     Read and write access to the file. Data can be written to and read from the
		//     file.
		ReadWrite = 3,
	}


	// Summary:
	//     Contains constants for controlling the kind of access other System.IO.FileStream
	//     objects can have to the same file.
	[Serializable]
	//[ComVisible( true )]
	[Flags]
	public enum FileShare
	{
		// Summary:
		//     Declines sharing of the current file. Any request to open the file (by this
		//     process or another process) will fail until the file is closed.
		None = 0,
		//
		// Summary:
		//     Allows subsequent opening of the file for reading. If this flag is not specified,
		//     any request to open the file for reading (by this process or another process)
		//     will fail until the file is closed. However, even if this flag is specified,
		//     additional permissions might still be needed to access the file.
		Read = 1,
		//
		// Summary:
		//     Allows subsequent opening of the file for writing. If this flag is not specified,
		//     any request to open the file for writing (by this process or another process)
		//     will fail until the file is closed. However, even if this flag is specified,
		//     additional permissions might still be needed to access the file.
		Write = 2,
		//
		// Summary:
		//     Allows subsequent opening of the file for reading or writing. If this flag
		//     is not specified, any request to open the file for reading or writing (by
		//     this process or another process) will fail until the file is closed. However,
		//     even if this flag is specified, additional permissions might still be needed
		//     to access the file.
		ReadWrite = 3,
		//
		// Summary:
		//     Allows subsequent deleting of a file.
		Delete = 4,
		//
		// Summary:
		//     Makes the file handle inheritable by child processes. This is not directly
		//     supported by Win32.
		Inheritable = 16,
	}


	// Summary:
	//     Represents advanced options for creating a System.IO.FileStream object.
	[Serializable]
	//[ComVisible( true )]
	[Flags]
	public enum FileOptions
	{
		// Summary:
		//     Indicates that the system should write through any intermediate cache and
		//     go directly to disk.
		WriteThrough = -2147483648,
		//
		// Summary:
		//     Indicates that no additional options should be used when creating a System.IO.FileStream
		//     object.
		None = 0,
		//
		// Summary:
		//     Indicates that a file is encrypted and can be decrypted only by using the
		//     same user account used for encryption.
		Encrypted = 16384,
		//
		// Summary:
		//     Indicates that a file is automatically deleted when it is no longer in use.
		DeleteOnClose = 67108864,
		//
		// Summary:
		//     Indicates that the file is to be accessed sequentially from beginning to
		//     end. The system can use this as a hint to optimize file caching. If an application
		//     moves the file pointer for random access, optimum caching may not occur;
		//     however, correct operation is still guaranteed.
		SequentialScan = 134217728,
		//
		// Summary:
		//     Indicates that the file is accessed randomly. The system can use this as
		//     a hint to optimize file caching.
		RandomAccess = 268435456,
		//
		// Summary:
		//     Indicates that a file can be used for asynchronous reading and writing.
		Asynchronous = 1073741824,
	}


	//// Summary:
	////     Specifies the position in a stream to use for seeking.
	//[Serializable]
	////[ComVisible( true )]
	//public enum SeekOrigin
	//{
	//	// Summary:
	//	//     Specifies the beginning of a stream.
	//	Begin = 0,
	//	//
	//	// Summary:
	//	//     Specifies the current position within a stream.
	//	Current = 1,
	//	//
	//	// Summary:
	//	//     Specifies the end of a stream.
	//	End = 2,
	//}
}
