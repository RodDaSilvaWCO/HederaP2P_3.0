#define FEATURE_MACL			// Comment out to remove the GetAccessControl and SetAccessControl methods

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
#if FEATURE_MACL
using System.Security.AccessControl;
#endif
using System.Security.Permissions;
//using Microsoft.Win32;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace Unosys.SDK
{
	[Serializable]
	[ComVisible( true )]
	public sealed class DirectoryInfo : FileSystemInfo
	{
		//private string[] demandDir;

#if FEATURE_CORECLR
         // Migrating InheritanceDemands requires this default ctor, so we can annotate it.
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#else
        [System.Security.SecuritySafeCritical]
#endif //FEATURE_CORESYSTEM
        private DirectoryInfo(){}
 
        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static DirectoryInfo UnsafeCreateDirectoryInfo(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
 
            DirectoryInfo di = new DirectoryInfo();
            di.Init(path, false);
            return di;
        }
#endif

		[System.Security.SecuritySafeCritical]
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public DirectoryInfo( string path )
		{
			if (path == null)
				throw new ArgumentNullException( "path" );
			Contract.EndContractBlock();

			Init( path, true );
		}

		[System.Security.SecurityCritical]
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		private void Init( string path, bool checkHost )
		{
			// Special case "<DriveLetter>:" to point to "<CurrentDirectory>" instead
			if ((path.Length == 2) && (path[1] == ':'))
			{
				OriginalPath = ".";
			}
			else
			{
				OriginalPath = path;
			}

			// Must fully qualify the path for the security check
			string fullPath = Path.GetFullPathInternal( path );

			//demandDir = new string[] { Directory.GetDemandDir( fullPath, true ) };
#if FEATURE_CORECLR
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, OriginalPath, fullPath);
                state.EnsureState();
            }
#else
			// TODO: Security
			//FileIOPermission.QuickDemand( FileIOPermissionAccess.Read, demandDir, false, false );
#endif
			FullPath = fullPath;
			DisplayPath = GetDisplayName( OriginalPath, FullPath );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
#if FEATURE_CORESYSTEM
        [System.Security.SecuritySafeCritical]
#endif //FEATURE_CORESYSTEM
		internal DirectoryInfo( string fullPath, bool junk )
		{
			Contract.Assert( Path.GetRootLength( fullPath ) > 0, "fullPath must be fully qualified!" );
			// Fast path when we know a DirectoryInfo exists.
			OriginalPath = Path.GetFileName( fullPath );

			FullPath = fullPath;
			DisplayPath = GetDisplayName( OriginalPath, FullPath );
			//demandDir = new string[] { Directory.GetDemandDir( fullPath, true ) };
		}

		[System.Security.SecurityCritical]  // auto-generated
		private DirectoryInfo( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
#if !FEATURE_CORECLR
			// TODO: Security
			//demandDir = new string[] { Directory.GetDemandDir( FullPath, true ) };
			//FileIOPermission.QuickDemand( FileIOPermissionAccess.Read, demandDir, false, false );
#endif
			DisplayPath = GetDisplayName( OriginalPath, FullPath );
		}

		public override string Name
		{
			[ResourceExposure( ResourceScope.Machine )]
			[ResourceConsumption( ResourceScope.Machine )]
			get
			{
#if FEATURE_CORECLR
                // DisplayPath is dir name for coreclr
                return DisplayPath;
#else
				// Return just dir name
				return GetDirName( FullPath );
#endif
			}
		}

		public DirectoryInfo Parent
		{
			[System.Security.SecuritySafeCritical]
			[ResourceExposure( ResourceScope.Machine )]
			[ResourceConsumption( ResourceScope.Machine )]
			get
			{
				string parentName;
				// FullPath might be either "c:\bar" or "c:\bar\".  Handle 
				// those cases, as well as avoiding mangling "c:\".
				string s = FullPath;
				if (s.Length > 3 && s.EndsWith( Convert.ToString( Path.DirectorySeparatorChar ) ))
					s = FullPath.Substring( 0, FullPath.Length - 1 );
				parentName = Path.GetDirectoryName( s );
				if (parentName == null)
					return null;
				DirectoryInfo dir = new DirectoryInfo( parentName, false );
#if FEATURE_CORECLR
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery | FileSecurityStateAccess.Read, string.Empty, dir.demandDir[0]);
                state.EnsureState();
#else
				// TODO: Security
				//FileIOPermission.QuickDemand( FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, dir.demandDir, false, false );
#endif
				return dir;
			}
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]
#endif
		public DirectoryInfo CreateSubdirectory( string path )
		{
			if (path == null)
				throw new ArgumentNullException( "path" );
			Contract.EndContractBlock();

			return CreateSubdirectory( path, null );
		}

#if FEATURE_MACL
		[System.Security.SecuritySafeCritical]  // auto-generated
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public DirectoryInfo CreateSubdirectory( string path, DirectorySecurity directorySecurity )
		{
			return CreateSubdirectoryHelper( path, directorySecurity );
		}
#else  // FEATURE_MACL
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public DirectoryInfo CreateSubdirectory( string path, Object directorySecurity )
		{
			if (path == null)
				throw new ArgumentNullException( "path" );
			Contract.EndContractBlock();

			return CreateSubdirectoryHelper( path, directorySecurity );
		}
#endif // FEATURE_MACL

		[System.Security.SecurityCritical]  // auto-generated
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		private DirectoryInfo CreateSubdirectoryHelper( string path, Object directorySecurity )
		{
			Contract.Requires( path != null );

			string newDirs = Path.InternalCombine( FullPath, path );
			string fullPath = Path.GetFullPathInternal( newDirs );

			if (0 != string.Compare( FullPath, 0, fullPath, 0, FullPath.Length, StringComparison.OrdinalIgnoreCase ))
			{
				string displayPath = __Error.GetDisplayablePath( DisplayPath, false );
				throw new ArgumentException( string.Format( "Argument_InvalidSubPath: {0} {1}", path, displayPath ) );
			}

			// TODO: Security
			// Ensure we have permission to create this subdirectory.
			//string demandDirForCreation = Directory.GetDemandDir( fullPath, true );
#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, OriginalPath, demandDirForCreation);
            state.EnsureState();
#else
			//FileIOPermission.QuickDemand( FileIOPermissionAccess.Write, demandDirForCreation, false, false );
#endif
			Directory.InternalCreateDirectory( fullPath, path, directorySecurity );

			// Check for read permission to directory we hand back by calling this constructor.
			return new DirectoryInfo( fullPath );
		}

		[ResourceExposure( ResourceScope.None )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public void Create()
		{
			Directory.InternalCreateDirectory( FullPath, OriginalPath, null, true );
		}

#if FEATURE_MACL
		[ResourceExposure( ResourceScope.None )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public void Create( DirectorySecurity directorySecurity )
		{
			Directory.InternalCreateDirectory( FullPath, OriginalPath, directorySecurity, true );
		}
#endif

		// Tests if the given path refers to an existing DirectoryInfo on disk.
		// 
		// Your application must have Read permission to the directory's
		// contents.
		public override bool Exists
		{
			[System.Security.SecuritySafeCritical]  // auto-generated
			get
			{
				try
				{
					if (_dataInitialised == -1)
						Refresh();
					if (_dataInitialised != 0) // Refresh was unable to initialise the data
						return false;

					//return _data.fileAttributes != -1 && (_data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) != 0;
					return (_data.Attributes.Value & (uint) FileAttributes.Directory) > 0;
				}
				catch
				{
					return false;
				}
			}
		}

#if FEATURE_MACL
		[ResourceExposure( ResourceScope.None )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public DirectorySecurity GetAccessControl()
		{
			return Directory.GetAccessControl( FullPath, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group );
		}

		[ResourceExposure( ResourceScope.None )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public DirectorySecurity GetAccessControl( AccessControlSections includeSections )
		{
			return Directory.GetAccessControl( FullPath, includeSections );
		}

		[ResourceExposure( ResourceScope.None )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public void SetAccessControl( DirectorySecurity directorySecurity )
		{
			Directory.SetAccessControl( FullPath, directorySecurity );
		}
#endif

		// Returns an array of Files in the current DirectoryInfo matching the 
		// given search criteria (ie, "*.txt").
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public FileInfo[] GetFiles( string searchPattern )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			Contract.EndContractBlock();

			return InternalGetFiles( searchPattern, SearchOption.TopDirectoryOnly );
		}

		// Returns an array of Files in the current DirectoryInfo matching the 
		// given search criteria (ie, "*.txt").
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public FileInfo[] GetFiles( string searchPattern, SearchOption searchOption )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
				throw new ArgumentOutOfRangeException( "searchOption", "ArgumentOutOfRange_Enum" );
			Contract.EndContractBlock();

			return InternalGetFiles( searchPattern, searchOption );
		}

		// Returns an array of Files in the current DirectoryInfo matching the 
		// given search criteria (ie, "*.txt").
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		private FileInfo[] InternalGetFiles( string searchPattern, SearchOption searchOption )
		{
			Contract.Requires( searchPattern != null );
			Contract.Requires( searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly );

			IEnumerable<FileInfo> enble = FileSystemEnumerableFactory.CreateFileInfoIterator( FullPath, OriginalPath, searchPattern, searchOption );
			List<FileInfo> fileList = new List<FileInfo>( enble );
			return fileList.ToArray();
		}

		// Returns an array of Files in the DirectoryInfo specified by path
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public FileInfo[] GetFiles()
		{
			return InternalGetFiles( "*", SearchOption.TopDirectoryOnly );
		}

		// Returns an array of Directories in the current directory.
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public DirectoryInfo[] GetDirectories()
		{
			return InternalGetDirectories( "*", SearchOption.TopDirectoryOnly );
		}

		// Returns an array of strongly typed FileSystemInfo entries in the path with the
		// given search criteria (ie, "*.txt").
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public FileSystemInfo[] GetFileSystemInfos( string searchPattern )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			Contract.EndContractBlock();

			return InternalGetFileSystemInfos( searchPattern, SearchOption.TopDirectoryOnly );
		}

		// Returns an array of strongly typed FileSystemInfo entries in the path with the
		// given search criteria (ie, "*.txt").
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public FileSystemInfo[] GetFileSystemInfos( string searchPattern, SearchOption searchOption )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
				throw new ArgumentOutOfRangeException( "searchOption", "ArgumentOutOfRange_Enum" );
			Contract.EndContractBlock();

			return InternalGetFileSystemInfos( searchPattern, searchOption );
		}

		// Returns an array of strongly typed FileSystemInfo entries in the path with the
		// given search criteria (ie, "*.txt").
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		private FileSystemInfo[] InternalGetFileSystemInfos( string searchPattern, SearchOption searchOption )
		{
			Contract.Requires( searchPattern != null );
			Contract.Requires( searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly );

			IEnumerable<FileSystemInfo> enble = FileSystemEnumerableFactory.CreateFileSystemInfoIterator( FullPath, OriginalPath, searchPattern, searchOption );
			List<FileSystemInfo> fileList = new List<FileSystemInfo>( enble );
			return fileList.ToArray();
		}

		// Returns an array of strongly typed FileSystemInfo entries which will contain a listing
		// of all the files and directories.
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public FileSystemInfo[] GetFileSystemInfos()
		{
			return InternalGetFileSystemInfos( "*", SearchOption.TopDirectoryOnly );
		}

		// Returns an array of Directories in the current DirectoryInfo matching the 
		// given search criteria (ie, "System*" could match the System & System32
		// directories).
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public DirectoryInfo[] GetDirectories( string searchPattern )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			Contract.EndContractBlock();

			return InternalGetDirectories( searchPattern, SearchOption.TopDirectoryOnly );
		}

		// Returns an array of Directories in the current DirectoryInfo matching the 
		// given search criteria (ie, "System*" could match the System & System32
		// directories).
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public DirectoryInfo[] GetDirectories( string searchPattern, SearchOption searchOption )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
				throw new ArgumentOutOfRangeException( "searchOption", "ArgumentOutOfRange_Enum" );
			Contract.EndContractBlock();

			return InternalGetDirectories( searchPattern, searchOption );
		}

		// Returns an array of Directories in the current DirectoryInfo matching the 
		// given search criteria (ie, "System*" could match the System & System32
		// directories).
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		private DirectoryInfo[] InternalGetDirectories( string searchPattern, SearchOption searchOption )
		{
			Contract.Requires( searchPattern != null );
			Contract.Requires( searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly );

			IEnumerable<DirectoryInfo> enble = FileSystemEnumerableFactory.CreateDirectoryInfoIterator( FullPath, OriginalPath, searchPattern, searchOption );
			List<DirectoryInfo> fileList = new List<DirectoryInfo>( enble );
			return fileList.ToArray();
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<DirectoryInfo> EnumerateDirectories()
		{
			return InternalEnumerateDirectories( "*", SearchOption.TopDirectoryOnly );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<DirectoryInfo> EnumerateDirectories( string searchPattern )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			Contract.EndContractBlock();

			return InternalEnumerateDirectories( searchPattern, SearchOption.TopDirectoryOnly );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<DirectoryInfo> EnumerateDirectories( string searchPattern, SearchOption searchOption )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
				throw new ArgumentOutOfRangeException( "searchOption", "ArgumentOutOfRange_Enum" );
			Contract.EndContractBlock();

			return InternalEnumerateDirectories( searchPattern, searchOption );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		private IEnumerable<DirectoryInfo> InternalEnumerateDirectories( string searchPattern, SearchOption searchOption )
		{
			Contract.Requires( searchPattern != null );
			Contract.Requires( searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly );

			return FileSystemEnumerableFactory.CreateDirectoryInfoIterator( FullPath, OriginalPath, searchPattern, searchOption );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<FileInfo> EnumerateFiles()
		{
			return InternalEnumerateFiles( "*", SearchOption.TopDirectoryOnly );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<FileInfo> EnumerateFiles( string searchPattern )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			Contract.EndContractBlock();

			return InternalEnumerateFiles( searchPattern, SearchOption.TopDirectoryOnly );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<FileInfo> EnumerateFiles( string searchPattern, SearchOption searchOption )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
				throw new ArgumentOutOfRangeException( "searchOption", "ArgumentOutOfRange_Enum" );
			Contract.EndContractBlock();

			return InternalEnumerateFiles( searchPattern, searchOption );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		private IEnumerable<FileInfo> InternalEnumerateFiles( string searchPattern, SearchOption searchOption )
		{
			Contract.Requires( searchPattern != null );
			Contract.Requires( searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly );

			return FileSystemEnumerableFactory.CreateFileInfoIterator( FullPath, OriginalPath, searchPattern, searchOption );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
		{
			return InternalEnumerateFileSystemInfos( "*", SearchOption.TopDirectoryOnly );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos( string searchPattern )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			Contract.EndContractBlock();

			return InternalEnumerateFileSystemInfos( searchPattern, SearchOption.TopDirectoryOnly );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos( string searchPattern, SearchOption searchOption )
		{
			if (searchPattern == null)
				throw new ArgumentNullException( "searchPattern" );
			if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
				throw new ArgumentOutOfRangeException( "searchOption", "ArgumentOutOfRange_Enum" );
			Contract.EndContractBlock();

			return InternalEnumerateFileSystemInfos( searchPattern, searchOption );
		}

		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		private IEnumerable<FileSystemInfo> InternalEnumerateFileSystemInfos( string searchPattern, SearchOption searchOption )
		{
			Contract.Requires( searchPattern != null );
			Contract.Requires( searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly );

			return FileSystemEnumerableFactory.CreateFileSystemInfoIterator( FullPath, OriginalPath, searchPattern, searchOption );
		}

		// Returns the root portion of the given path. The resulting string
		// consists of those rightmost characters of the path that constitute the
		// root of the path. Possible patterns for the resulting string are: An
		// empty string (a relative path on the current drive), "\" (an absolute
		// path on the current drive), "X:" (a relative path on a given drive,
		// where X is the drive letter), "X:\" (an absolute path on a given drive),
		// and "\\server\share" (a UNC path for a given server and share name).
		// The resulting string is null if path is null.
		public DirectoryInfo Root
		{
			[System.Security.SecuritySafeCritical]
			[ResourceExposure( ResourceScope.None )]
			[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
			get
			{
				int rootLength = Path.GetRootLength( FullPath );
				string rootPath = FullPath.Substring( 0, rootLength );
				//string demandPath;
				//demandPath = Directory.GetDemandDir( rootPath, true );
#if FEATURE_CORECLR
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, string.Empty, demandPath);
                sourceState.EnsureState();
#else
				// TODO: Security
				//FileIOPermission.QuickDemand( FileIOPermissionAccess.PathDiscovery, demandPath, false, false );
#endif
				return new DirectoryInfo( rootPath );
			}
		}

		[System.Security.SecuritySafeCritical]
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public void MoveTo( string destDirName )
		{
			if (destDirName == null)
				throw new ArgumentNullException( "destDirName" );
			if (destDirName.Length == 0)
				throw new ArgumentException( "Argument_EmptyFileName", "destDirName" );
			Contract.EndContractBlock();

#if FEATURE_CORECLR
            FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, DisplayPath, Directory.GetDemandDir(FullPath, true));
            sourceState.EnsureState();
#else
			// TODO: Security
			//FileIOPermission.QuickDemand( FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, demandDir, false, false );
#endif
			string fullDestDirName = Path.GetFullPathInternal( destDirName );
			string demandPath;
			if (!fullDestDirName.EndsWith( Convert.ToString( Path.DirectorySeparatorChar ) ))
				fullDestDirName = fullDestDirName + Path.DirectorySeparatorChar;

			demandPath = fullDestDirName + '.';

			// Demand read & write permission to destination.  The reason is
			// we hand back a DirectoryInfo to the destination that would allow
			// you to read a directory listing from that directory.  Sure, you 
			// had the ability to read the file contents in the old location,
			// but you technically also need read permissions to the new 
			// location as well, and write is not a true superset of read.
#if FEATURE_CORECLR
            FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destDirName, demandPath);
            destState.EnsureState();
#else
			// TODO: Security
			//FileIOPermission.QuickDemand( FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, demandPath );
#endif
			string fullSourcePath;
			if (FullPath.EndsWith( Convert.ToString( Path.DirectorySeparatorChar ) ))
				fullSourcePath = FullPath;
			else
				fullSourcePath = FullPath + Path.DirectorySeparatorChar;

			if (string.Compare( fullSourcePath, fullDestDirName, StringComparison.OrdinalIgnoreCase ) == 0)
				throw new IOException( "IO.IO_SourceDestMustBeDifferent" );

			string sourceRoot = Path.GetPathRoot( fullSourcePath );
			string destinationRoot = Path.GetPathRoot( fullDestDirName );

			if (string.Compare( sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase ) != 0)
				throw new IOException( "IO.IO_SourceDestMustHaveSameRoot" );

			/*if (!Win32Native.MoveFile( FullPath, destDirName ))
			{
				int hr = Marshal.GetLastWin32Error();
				if (hr == Win32Native.ERROR_FILE_NOT_FOUND) // A dubious error code
				{
					hr = Win32Native.ERROR_PATH_NOT_FOUND;
					__Error.WinIOError( hr, DisplayPath );
				}

				if (hr == Win32Native.ERROR_ACCESS_DENIED) // We did this for Win9x. We can't change it for backcomp. 
					throw new IOException( Environment.GetResourceString( "UnauthorizedAccess_IODenied_Path", DisplayPath ) );

				__Error.WinIOError( hr, string.Empty );
			}*/
			Directory.Move( FullPath, destDirName );
			FullPath = fullDestDirName;
			OriginalPath = destDirName;
			DisplayPath = GetDisplayName( OriginalPath, FullPath );
			//demandDir = new string[] { Directory.GetDemandDir( FullPath, true ) };

			// Flush any cached information about the directory.
			_dataInitialised = -1;
		}

		[System.Security.SecuritySafeCritical]
		[ResourceExposure( ResourceScope.None )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public override void Delete()
		{
			Directory.Delete( FullPath, OriginalPath, false, true );
		}

		[System.Security.SecuritySafeCritical]
		[ResourceExposure( ResourceScope.None )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public void Delete( bool recursive )
		{
			Directory.Delete( FullPath, OriginalPath, recursive, true );
		}

		// Returns the fully qualified path
		public override string ToString()
		{
			return DisplayPath;
		}

		private static string GetDisplayName( string originalPath, string fullPath )
		{
			Contract.Assert( originalPath != null );
			Contract.Assert( fullPath != null );

			string displayName = "";

			// Special case "<DriveLetter>:" to point to "<CurrentDirectory>" instead
			if ((originalPath.Length == 2) && (originalPath[1] == ':'))
			{
				displayName = ".";
			}
			else
			{
#if FEATURE_CORECLR
                displayName = GetDirName(fullPath);
#else
				displayName = originalPath;
#endif
			}
			return displayName;
		}

		private static string GetDirName( string fullPath )
		{
			Contract.Assert( fullPath != null );

			string dirName = null;
			if (fullPath.Length > 3)
			{
				string s = fullPath;
				if (fullPath.EndsWith( Convert.ToString( Path.DirectorySeparatorChar ) ))
				{
					s = fullPath.Substring( 0, fullPath.Length - 1 );
				}
				dirName = Path.GetFileName( s );
			}
			else
			{
				dirName = fullPath;  // For rooted paths, like "c:\"
			}
			return dirName;
		}
	}
}