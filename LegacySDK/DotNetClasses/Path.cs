
using System;
using System.Security.Permissions;
//using Win32Native = Microsoft.Win32.Win32Native;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Unosys.SDK
{
	// Provides methods for processing directory strings in an ideally
	// cross-platform manner.  Most of the methods don't do a complete 
	// full parsing (such as examining a UNC hostname), but they will
	// handle most string operations. 
	// 
	// File names cannot contain backslash (\), slash (/), colon (:),
	// asterick (*), question mark (?), quote ("), less than (<;), 
	// greater than (>;), or pipe (|).  The first three are used as directory
	// separators on various platforms.  Asterick and question mark are treated
	// as wild cards.  Less than, Greater than, and pipe all redirect input
	// or output from a program to a file or some combination thereof.  Quotes 
	// are special.
	// 
	// We are guaranteeing that Path.SeparatorChar is the correct 
	// directory separator on all platforms, and we will support
	// Path.AltSeparatorChar as well.  To write cross platform 
	// code with minimal pain, you can use slash (/) as a directory separator in
	// your strings.
	// Class contains only static data, no need to serialize
	[ComVisible( true )]
	public static class Path
	{
		// Platform specific directory separator character.  This is backslash 
		// ('\') on Windows, slash ('/') on Unix, and colon (':') on Mac.
#if !PLATFORM_UNIX
		public static readonly char DirectorySeparatorChar = '\\';
#else
        public static readonly char DirectorySeparatorChar = '/'; 
#endif // !PLATFORM_UNIX

		// Platform specific alternate directory separator character. 
		// This is backslash ('\') on Unix, and slash ('/') on Windows
		// and MacOS. 
		//
#if !PLATFORM_UNIX
		public static readonly char AltDirectorySeparatorChar = '/';
#else 
        public static readonly char AltDirectorySeparatorChar = '\\';
#endif // !PLATFORM_UNIX

		// Platform specific volume separator character.  This is colon (':')
		// on Windows and MacOS, and slash ('/') on Unix.  This is mostly 
		// useful for parsing paths like "c:\windows" or "MacVolume:System Folder".
		//
#if !PLATFORM_UNIX
		public static readonly char VolumeSeparatorChar = ':';
#else
        public static readonly char VolumeSeparatorChar = '/'; 
#endif // !PLATFORM_UNIX

		// Platform specific invalid list of characters in a path. 
		// See the "Naming a File" MSDN conceptual docs for more details on
		// what is valid in a file name (which is slightly different from what
		// is legal in a path name).
		// Note: This list is duplicated in CheckInvalidPathChars 
		[Obsolete( "Please use GetInvalidPathChars or GetInvalidFileNameChars instead." )]
		public static readonly char[] InvalidPathChars = { '\"', '<', '>', '|', '\0', (Char) 1, (Char) 2, (Char) 3, (Char) 4, (Char) 5, (Char) 6, (Char) 7, (Char) 8, (Char) 9, (Char) 10, (Char) 11, (Char) 12, (Char) 13, (Char) 14, (Char) 15, (Char) 16, (Char) 17, (Char) 18, (Char) 19, (Char) 20, (Char) 21, (Char) 22, (Char) 23, (Char) 24, (Char) 25, (Char) 26, (Char) 27, (Char) 28, (Char) 29, (Char) 30, (Char) 31 };

		// Trim trailing white spaces, tabs etc but don't be aggressive in removing everything that has UnicodeCategory of trailing space.
		// string.WhitespaceChars will trim aggressively than what the underlying FS does (for ex, NTFS, FAT). 
		internal static readonly char[] TrimEndChars = { (char) 0x9, (char) 0xA, (char) 0xB, (char) 0xC, (char) 0xD, (char) 0x20, (char) 0x85, (char) 0xA0 };

		private static readonly char[] RealInvalidPathChars = { '\"', '<', '>', '|', '\0', (Char) 1, (Char) 2, (Char) 3, (Char) 4, (Char) 5, (Char) 6, (Char) 7, (Char) 8, (Char) 9, (Char) 10, (Char) 11, (Char) 12, (Char) 13, (Char) 14, (Char) 15, (Char) 16, (Char) 17, (Char) 18, (Char) 19, (Char) 20, (Char) 21, (Char) 22, (Char) 23, (Char) 24, (Char) 25, (Char) 26, (Char) 27, (Char) 28, (Char) 29, (Char) 30, (Char) 31 };

		private static readonly char[] InvalidFileNameChars = { '\"', '<', '>', '|', '\0', (Char) 1, (Char) 2, (Char) 3, (Char) 4, (Char) 5, (Char) 6, (Char) 7, (Char) 8, (Char) 9, (Char) 10, (Char) 11, (Char) 12, (Char) 13, (Char) 14, (Char) 15, (Char) 16, (Char) 17, (Char) 18, (Char) 19, (Char) 20, (Char) 21, (Char) 22, (Char) 23, (Char) 24, (Char) 25, (Char) 26, (Char) 27, (Char) 28, (Char) 29, (Char) 30, (Char) 31, ':', '*', '?', '\\', '/' };

#if !PLATFORM_UNIX
		public static readonly char PathSeparator = ';';
#else
        public static readonly char PathSeparator = ':';
#endif // !PLATFORM_UNIX

		// Make this public sometime.
		// The max total path is 260, and the max individual component length is 255. 
		// For example, D:\<256 char file name> isn't legal, even though it's under 260 chars. 
		internal static readonly int MaxPath = 260;
		//private static readonly int MaxDirectoryLength = 255;

		// Changes the extension of a file path. The path parameter
		// specifies a file path, and the extension parameter
		// specifies a file extension (with a leading period, such as 
		// ".exe" or ".cs").
		// 
		// The function returns a file path with the same root, directory, and base 
		// name parts as path, but with the file extension changed to
		// the specified extension. If path is null, the function 
		// returns null. If path does not contain a file extension,
		// the new file extension is appended to the path. If extension
		// is null, any exsiting extension is removed from path.
		public static string ChangeExtension( string path, string extension )
		{
			if (path != null)
			{
				CheckInvalidPathChars( path );

				string s = path;
				for (int i = path.Length; --i >= 0; )
				{
					char ch = path[i];
					if (ch == '.')
					{
						s = path.Substring( 0, i );
						break;
					}

					if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar) break;
				}

				if (extension != null && path.Length != 0)
				{
					if (extension.Length == 0 || extension[0] != '.')
					{
						s = s + ".";
					}

					s = s + extension;
				}

				return s;
			}

			return null;
		}

		// Returns the directory path of a file path. This method effectively
		// removes the last element of the given file path, i.e. it returns a 
		// string consisting of all characters up to but not including the last
		// backslash ("\") in the file path. The returned value is null if the file 
		// path is null or if the file path denotes a root (such as "\", "C:", or 
		// "\\server\share").
		[System.Security.SecuritySafeCritical]  // auto-generated
		[ResourceExposure( ResourceScope.None )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public static string GetDirectoryName( string path )
		{
			if (path != null)
			{
				CheckInvalidPathChars( path );
				path = NormalizePath( path, false );
				int root = GetRootLength( path );
				int i = path.Length;
				if (i > root)
				{
					i = path.Length;
					if (i == root) return null;
					while (i > root && path[--i] != DirectorySeparatorChar && path[i] != AltDirectorySeparatorChar) ;
					return path.Substring( 0, i );
				}
			}

			return null;
		}

		// Gets the length of the root DirectoryInfo or whatever DirectoryInfo markers
		// are specified for the first part of the DirectoryInfo name.
		internal static int GetRootLength( string path )
		{
			CheckInvalidPathChars( path );

			int i = 0;
			int length = path.Length;

#if !PLATFORM_UNIX
			if (length >= 1 && (IsDirectorySeparator( path[0] )))
			{
				// handles UNC names and directories off current drive's root. 
				i = 1;
				if (length >= 2 && (IsDirectorySeparator( path[1] )))
				{
					i = 2;
					int n = 2;
					while (i < length && ((path[i] != DirectorySeparatorChar && path[i] != AltDirectorySeparatorChar) || --n > 0)) i++;
				}
			}
			else if (length >= 2 && path[1] == VolumeSeparatorChar)
			{
				// handles A:\foo. 
				i = 2;
				if (length >= 3 && (IsDirectorySeparator( path[2] ))) i++;
			}
			return i;
#else 
            if (length >= 1 && (IsDirectorySeparator(path[0]))) {
                i = 1;
            }
            return i; 
#endif // !PLATFORM_UNIX
		}

		internal static bool IsDirectorySeparator( char c )
		{
			return (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar);
		}

		public static char[] GetInvalidPathChars()
		{
			return (char[]) RealInvalidPathChars.Clone();
		}

		public static char[] GetInvalidFileNameChars()
		{
			return (char[]) InvalidFileNameChars.Clone();
		}

		// Returns the extension of the given path. The returned value includes the
		// period (".") character of the extension except when you have a terminal period when you get string.Empty, such as ".exe" or 
		// ".cpp". The returned value is null if the given path is 
		// null or if the given path does not include an extension.
		public static string GetExtension( string path )
		{
			if (path == null)
				return null;

			CheckInvalidPathChars( path );
			int length = path.Length;
			for (int i = length; --i >= 0; )
			{
				char ch = path[i];
				if (ch == '.')
				{
					if (i != length - 1)
						return path.Substring( i, length - i );
					else
						return string.Empty;
				}
				if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
					break;
			}
			return string.Empty;
		}

		// Expands the given path to a fully qualified path. The resulting string 
		// consists of a drive letter, a colon, and a root relative path. This
		// function does not verify that the resulting path 
		// refers to an existing file or directory on the associated volume. 
		[System.Security.SecuritySafeCritical]  // auto-generated
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public static string GetFullPath( string path )
		{
			string fullPath = GetFullPathInternal( path );
			// %TODO%  Perform equivalent check...
			//  new FileIOPermission( FileIOPermissionAccess.PathDiscovery, new string[] { fullPath }, false, false ).Demand(); 
			return fullPath;
		}

		// This method is package access to let us quickly get a string name
		// while avoiding a security check.  This also serves a slightly 
		// different purpose - when we open a file, we need to resolve the
		// path into a fully qualified, non-relative path name.  This
		// method does that, finding the current drive &; directory.  But
		// as long as we don't return this info to the user, we're good.  However, 
		// the public GetFullPath does need to do a security check.
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		internal static string GetFullPathInternal( string path )
		{
			if (path == null)
				throw new ArgumentNullException( "path" );
			Contract.EndContractBlock();

			//string newPath = NormalizePath( path, true );
			string newPath = NormalizePath( path, false );

			return newPath;
		}

		[System.Security.SecuritySafeCritical]  // auto-generated 
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		internal unsafe static string NormalizePath( string path, bool fullCheck )
		{
			return NormalizePath( path, fullCheck, MaxPath );
		}

		[System.Security.SecurityCritical]  // auto-generated 
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		internal unsafe static string NormalizePath( string path, bool fullCheck, int maxPathLength )
		{
			Contract.Requires( path != null, "path can't be null" );
			// If we're doing a full path check, trim whitespace and look for 
			// illegal path characters.
			if (fullCheck)
			{
				// Trim whitespace off the end of the string. 
				// Win32 normalization trims only U+0020.
				path = path.TrimEnd( TrimEndChars );

				// Look for illegal path characters.
				CheckInvalidPathChars( path );
			}

			// We have one way of fetching an absolute file path, so we use it:
			string returnVal = path;
			try
			{
				// TODO: Get a better way of validating a path than requiring that there be a file there
				//var handle = API.OpenSetAsync( path, (uint) FileMode.Open, (uint) FileAccess.Read, 0, (uint) FileShare.Read ).Result;
				//returnVal = API.GetFullPathNameAsync( handle ).Result;
				//var result = API.CloseSetAsync( handle ).Result;
			}
			catch
			{
				// File does not exist, so we can't validate the path
				Debug.WriteLine( "Error in File.NormalizePath: {0} does not reference an existing file, so cannot be normalized.  As such, {1} is the best we can do.", path, returnVal );
			}

			/* From here onwards a bunch of Windows grognards get it on with char pointers.
			 * We don't need this BS, so we cut it all!
			int index = 0;
			// We prefer to allocate on the stack for workingset/perf gain. If the 
			// starting path is less than MaxPath then we can stackalloc; otherwise we'll
			// use a StringBuilder (PathHelper does this under the hood). The latter may 
			// happen in 2 cases:
			// 1. Starting path is greater than MaxPath but it normalizes down to MaxPath.
			// This is relevant for paths containing escape sequences. In this case, we
			// attempt to normalize down to MaxPath, but the caller pays a perf penalty 
			// since StringBuilder is used.
			// 2. IsolatedStorage, which supports paths longer than MaxPath (value given 
			// by maxPathLength. 
			PathHelper newBuffer = default( PathHelper );
			if (path.Length <= MaxPath)
			{
				char* m_arrayPtr = stackalloc char[MaxPath];
				newBuffer = new PathHelper( m_arrayPtr, MaxPath );
			}
			else
			{
				newBuffer = new PathHelper( path.Length + Path.MaxPath, maxPathLength );
			}

			uint numSpaces = 0;
			uint numDots = 0;
			bool fixupDirectorySeparator = false;
			// Number of significant chars other than potentially suppressible
			// dots and spaces since the last directory or volume separator char
			uint numSigChars = 0;
			int lastSigChar = -1; // Index of last significant character. 
			// Whether this segment of the path (not the complete path) started
			// with a volume separator char.  Reject "c:...". 
			bool startedWithVolumeSeparator = false;
			bool firstSegment = true;
			bool mightBeShortFileName = false;
			int lastDirectorySeparatorPos = 0;

#if !PLATFORM_UNIX
			// LEGACY: This code is here for backwards compatibility reasons. It 
			// ensures that \\foo.cs\bar.cs stays \\foo.cs\bar.cs instead of being
			// turned into \foo.cs\bar.cs. 
			if (path.Length > 0 && (path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar))
			{
				newBuffer.Append( '\\' );
				index++;
				lastSigChar = 0;
			}
#endif

			// Normalize the string, stripping out redundant dots, spaces, and
			// slashes. 
			while (index < path.Length)
			{
				char currentChar = path[index];

				// We handle both directory separators and dots specially.  For
				// directory separators, we consume consecutive appearances.
				// For dots, we consume all dots beyond the second in
				// succession.  All other characters are added as is.  In 
				// addition we consume all spaces after the last other char
				// in a directory name up until the directory separator. 

				if (currentChar == DirectorySeparatorChar || currentChar == AltDirectorySeparatorChar)
				{
					// If we have a path like "123.../foo", remove the trailing dots. 
					// However, if we found "c:\temp\..\bar" or "c:\temp\...\bar", don't.
					// Also remove trailing spaces from both files & directory names.
					// This was agreed on with the OS team to fix undeletable directory
					// names ending in spaces. 

					// If we saw a '\' as the previous last significant character and 
					// are simply going to write out dots, suppress them. 
					// If we only contain dots and slashes though, only allow
					// a string like [dot]+ [space]*.  Ignore everything else. 
					// Legal: "\.. \", "\...\", "\. \"
					// Illegal: "\.. .\", "\. .\", "\ .\"
					if (numSigChars == 0)
					{
						// Dot and space handling 
						if (numDots > 0)
						{
							// Look for ".[space]*" or "..[space]*" 
							int start = lastSigChar + 1;
							if (path[start] != '.')
								throw new ArgumentException( "Arg_PathIllegal" );

							// Only allow "[dot]+[space]*", and normalize the
							// legal ones to "." or ".."
							if (numDots >= 2)
							{
								// Reject "C:..."
								if (startedWithVolumeSeparator && numDots > 2)

									throw new ArgumentException( "Arg_PathIllegal" );

								if (path[start + 1] == '.')
								{
									// Search for a space in the middle of the
									// dots and throw
									for (int i = start + 2; i < start + numDots; i++)
									{
										if (path[i] != '.')
											throw new ArgumentException( "Arg_PathIllegal" );
									}

									numDots = 2;
								}
								else
								{
									if (numDots > 1)
										throw new ArgumentException( "Arg_PathIllegal" );
									numDots = 1;
								}
							}

							if (numDots == 2)
							{
								newBuffer.Append( '.' );
							}

							newBuffer.Append( '.' );
							fixupDirectorySeparator = false;

							// Continue in this case, potentially writing out '\'. 
						}

						if (numSpaces > 0 && firstSegment)
						{
							// Handle strings like " \\server\share".
							if (index + 1 < path.Length &&
								(path[index + 1] == DirectorySeparatorChar || path[index + 1] == AltDirectorySeparatorChar))
							{
								newBuffer.Append( DirectorySeparatorChar );
							}
						}
					}
					numDots = 0;
					numSpaces = 0;  // Suppress trailing spaces

					if (!fixupDirectorySeparator)
					{
						fixupDirectorySeparator = true;
						newBuffer.Append( DirectorySeparatorChar );
					}
					numSigChars = 0;
					lastSigChar = index;
					startedWithVolumeSeparator = false;
					firstSegment = false;

#if !PLATFORM_UNIX
					// For short file names, we must try to expand each of them as
					// soon as possible.  We need to allow people to specify a file 
					// name that doesn't exist using a path with short file names 
					// in it, such as this for a temp file we're trying to create:
					// C:\DOCUME~1\USERNA~1.RED\LOCALS~1\Temp\bg3ylpzp 
					// We could try doing this afterwards piece by piece, but it's
					// probably a lot simpler to do it here.
					if (mightBeShortFileName)
					{
						newBuffer.TryExpandShortFileName();
						mightBeShortFileName = false;
					}
#endif
					int thisPos = newBuffer.Length - 1;
					if (thisPos - lastDirectorySeparatorPos > MaxDirectoryLength)
					{
						throw new PathTooLongException( "IO.PathTooLong" );
					}
					lastDirectorySeparatorPos = thisPos;
				} // if (Found directory separator)
				else if (currentChar == '.')
				{
					// Reduce only multiple .'s only after slash to 2 dots. For 
					// instance a...b is a valid file name.
					numDots++;
					// Don't flush out non-terminal spaces here, because they may in
					// the end not be significant.  Turn "c:\ . .\foo" -> "c:\foo"
					// which is the conclusion of removing trailing dots & spaces,
					// as well as folding multiple '\' characters. 
				}
				else if (currentChar == ' ')
				{
					numSpaces++;
				}
				else
				{  // Normal character logic 
#if !PLATFORM_UNIX
					if (currentChar == '~')
						mightBeShortFileName = true;
#endif
					fixupDirectorySeparator = false;

#if !PLATFORM_UNIX
					// To reject strings like "C:...\foo" and "C  :\foo" 
					if (firstSegment && currentChar == VolumeSeparatorChar)
					{
						// Only accept "C:", not "c :" or ":"
						// Get a drive letter or ' ' if index is 0.
						char driveLetter = (index > 0) ? path[index - 1] : ' ';
						bool validPath = ((numDots == 0) && (numSigChars >= 1) && (driveLetter != ' '));
						if (!validPath)
							throw new ArgumentException( "Arg_PathIllegal" );

						startedWithVolumeSeparator = true;
						// We need special logic to make " c:" work, we should not fix paths like "  foo::$DATA"
						if (numSigChars > 1)
						{ // Common case, simply do nothing
							int spaceCount = 0; // How many spaces did we write out, numSpaces has already been reset.
							while ((spaceCount < newBuffer.Length) && newBuffer[spaceCount] == ' ')
								spaceCount++;
							if (numSigChars - spaceCount == 1)
							{
								//Safe to update stack ptr directly 
								newBuffer.Length = 0;
								newBuffer.Append( driveLetter ); // Overwrite spaces, we need a special case to not break "  foo" as a relative path. 
							}
						}
						numSigChars = 0;
					}
					else
#endif // !PLATFORM_UNIX
					{
						numSigChars += 1 + numDots + numSpaces;
					}

					// Copy any spaces & dots since the last significant character
					// to here.  Note we only counted the number of dots & spaces,
					// and don't know what order they're in.  Hence the copy.
					if (numDots > 0 || numSpaces > 0)
					{
						int numCharsToCopy = (lastSigChar >= 0) ? index - lastSigChar - 1 : index;
						if (numCharsToCopy > 0)
						{
							for (int i = 0; i < numCharsToCopy; i++)
							{
								newBuffer.Append( path[lastSigChar + 1 + i] );
							}
						}
						numDots = 0;
						numSpaces = 0;
					}

					newBuffer.Append( currentChar );
					lastSigChar = index;
				}

				index++;
			} // end while

			if (newBuffer.Length - 1 - lastDirectorySeparatorPos > MaxDirectoryLength)
			{
				throw new PathTooLongException( "IO.PathTooLong" );
			}

			// Drop any trailing dots and spaces from file & directory names, EXCEPT
			// we MUST make sure that "C:\foo\.." is correctly handled.
			// Also handle "C:\foo\." -> "C:\foo", while "C:\." -> "C:\"
			if (numSigChars == 0)
			{
				if (numDots > 0)
				{
					// Look for ".[space]*" or "..[space]*" 
					int start = lastSigChar + 1;
					if (path[start] != '.')
						throw new ArgumentException( "Arg_PathIllegal" );

					// Only allow "[dot]+[space]*", and normalize the
					// legal ones to "." or ".."
					if (numDots >= 2)
					{
						// Reject "C:..."
						if (startedWithVolumeSeparator && numDots > 2)
							throw new ArgumentException( "Arg_PathIllegal" );

						if (path[start + 1] == '.')
						{
							// Search for a space in the middle of the
							// dots and throw
							for (int i = start + 2; i < start + numDots; i++)
							{
								if (path[i] != '.')
									throw new ArgumentException( "Arg_PathIllegal" );
							}

							numDots = 2;
						}
						else
						{
							if (numDots > 1)
								throw new ArgumentException( "Arg_PathIllegal" );
							numDots = 1;
						}
					}

					if (numDots == 2)
					{
						newBuffer.Append( '.' );
					}

					newBuffer.Append( '.' );
				}
			} // if (numSigChars == 0)

			// If we ended up eating all the characters, bail out. 
			if (newBuffer.Length == 0)
				throw new ArgumentException( "Arg_PathIllegal" );

			// Disallow URL's here.  Some of our other Win32 API calls will reject
			// them later, so we might be better off rejecting them here.
			// Note we've probably turned them into "file:\D:\foo.tmp" by now. 
			// But for compatibility, ensure that callers that aren't doing a
			// full check aren't rejected here. 
			if (fullCheck)
			{
				if (newBuffer.OrdinalStartsWith( "http:", false ) ||
					 newBuffer.OrdinalStartsWith( "file:", false ))
				{
					throw new ArgumentException( "Argument_PathUriFormatNotSupported" );
				}
			}

#if !PLATFORM_UNIX
			// If the last part of the path (file or directory name) had a tilde, 
			// expand that too.
			if (mightBeShortFileName)
			{
				newBuffer.TryExpandShortFileName();
			}
#endif

			// Call the Win32 API to do the final canonicalization step.
			int result = 1;

			if (fullCheck)
			{
				// NOTE: Win32 GetFullPathName requires the input buffer to be big enough to fit the initial 
				// path which is a concat of CWD and the relative path, this can be of an arbitrary
				// size and could be > MAX_PATH (which becomes an artificial limit at this point),
				// even though the final normalized path after fixing up the relative path syntax
				// might be well within the MAX_PATH restriction. For ex, 
				// "c:\SomeReallyLongDirName(thinkGreaterThan_MAXPATH)\..\foo.txt" which actually requires a
				// buffer well with in the MAX_PATH as the normalized path is just "c:\foo.txt" 
				// This buffer requirement seems wrong, it could be a bug or a perf optimization 
				// like returning required buffer length quickly or avoid stratch buffer etc.
				// Either way we need to workaround it here... 

				// Ideally we would get the required buffer length first by calling GetFullPathName
				// once without the buffer and use that in the later call but this doesn't always work
				// due to Win32 GetFullPathName bug. For instance, in Win2k, when the path we are trying to 
				// fully qualify is a single letter name (such as "a", "1", ",") GetFullPathName
				// fails to return the right buffer size (i.e, resulting in insufficient buffer). 
				// To workaround this bug we will start with MAX_PATH buffer and grow it once if the 
				// return value is > MAX_PATH.

				result = newBuffer.GetFullPathName();

#if !PLATFORM_UNIX
				// If we called GetFullPathName with something like "foo" and our 
				// command window was in short file name mode (ie, by running edlin or
				// DOS versions of grep, etc), we might have gotten back a short file 
				// name.  So, check to see if we need to expand it. 
				mightBeShortFileName = false;
				for (int i = 0; i < newBuffer.Length && !mightBeShortFileName; i++)
				{
					if (newBuffer[i] == '~')
						mightBeShortFileName = true;
				}

				if (mightBeShortFileName)
				{
					bool r = newBuffer.TryExpandShortFileName();
					// Consider how the path "Doesn'tExist" would expand.  If 
					// we add in the current directory, it too will need to be
					// fully expanded, which doesn't happen if we use a file 
					// name that doesn't exist.
					if (!r)
					{
						int lastSlash = -1;

						for (int i = newBuffer.Length - 1; i >= 0; i--)
						{
							if (newBuffer[i] == DirectorySeparatorChar)
							{
								lastSlash = i;
								break;
							}
						}

						if (lastSlash >= 0)
						{

							// This bounds check is for safe memcpy but we should never get this far
							if (newBuffer.Length >= maxPathLength)
								throw new PathTooLongException( "IO.PathTooLong" );

							int lenSavedName = newBuffer.Length - lastSlash - 1;
							Contract.Assert( lastSlash < newBuffer.Length, "path unexpectedly ended in a '\'" );

							newBuffer.Fixup( lenSavedName, lastSlash );
						}
					}
				}
#endif
			}

			if (result != 0)
			{
				// Throw an ArgumentException for paths like \\, \\server, \\server\
				// This check can only be properly done after normalizing, so
				// \\foo\.. will be properly rejected.  Also, reject \\?\GLOBALROOT\ 
				// (an internal kernel path) because it provides aliases for drives.
				if (newBuffer.Length > 1 && newBuffer[0] == '\\' && newBuffer[1] == '\\')
				{
					int startIndex = 2;
					while (startIndex < result)
					{
						if (newBuffer[startIndex] == '\\')
						{
							startIndex++;
							break;
						}
						else
						{
							startIndex++;
						}
					}
					if (startIndex == result)
						throw new ArgumentException( "Arg_PathIllegalUNC" );

					// Check for \\?\Globalroot, an internal mechanism to the kernel
					// that provides aliases for drives and other undocumented stuff.
					// The kernel team won't even describe the full set of what 
					// is available here - we don't want managed apps mucking
					// with this for security reasons. 
					if (newBuffer.OrdinalStartsWith( "\\\\?\\globalroot", true ))
						throw new ArgumentException( "Arg_PathGlobalRoot" );
				}
			}

			// Check our result and form the managed string as necessary.
			if (newBuffer.Length >= maxPathLength)
				throw new PathTooLongException( "IO.PathTooLong" );

			if (result == 0)
			{
				//int errorCode = Marshal.GetLastWin32Error();
				//if (errorCode == 0) 
				//	errorCode = Win32Native.ERROR_BAD_PATHNAME;
				//__Error.WinIOError(errorCode, path);
				return null;  // Unreachable - silence a compiler error.
			}

			string returnVal = newBuffer.ToString();
			if (string.Equals( returnVal, path, StringComparison.Ordinal ))
			{
				returnVal = path;
			}*/

			return returnVal;
		}

		internal static readonly int MaxLongPath = 32000;

		private static readonly string Prefix = @"\\?\";

		internal unsafe static bool HasLongPathPrefix( string path )
		{
			return path.StartsWith( Prefix, StringComparison.Ordinal );
		}

		internal unsafe static string AddLongPathPrefix( string path )
		{
			if (path.StartsWith( Prefix, StringComparison.Ordinal ))
				return path;
			else
				return Prefix + path;
		}

		internal unsafe static string RemoveLongPathPrefix( string path )
		{
			if (!path.StartsWith( Prefix, StringComparison.Ordinal ))
				return path;
			else
				return path.Substring( 4 );
		}

		internal unsafe static StringBuilder RemoveLongPathPrefix( StringBuilder path )
		{
			if (!path.ToString().StartsWith( Prefix, StringComparison.Ordinal ))
				return path;
			else
				return path.Remove( 0, 4 );
		}

		// Returns the name and extension parts of the given path. The resulting
		// string contains the characters of path that follow the last
		// backslash ("\"), slash ("/"), or colon (":") character in 
		// path. The resulting string is the entire path if path
		// contains no backslash after removing trailing slashes, slash, or colon characters. The resulting 
		// string is null if path is null. 
		public static string GetFileName( string path )
		{
			if (path != null)
			{
				CheckInvalidPathChars( path );

				int length = path.Length;
				for (int i = length; --i >= 0; )
				{
					char ch = path[i];
					if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
						return path.Substring( i + 1, length - i - 1 );
				}
			}

			return path;
		}

		public static string GetFileNameWithoutExtension( string path )
		{
			path = GetFileName( path );
			if (path != null)
			{
				int i;
				if ((i = path.LastIndexOf( '.' )) == -1)
					return path; // No path extension found
				else
					return path.Substring( 0, i );
			}

			return null;
		}

		// Returns the root portion of the given path. The resulting string
		// consists of those rightmost characters of the path that constitute the 
		// root of the path. Possible patterns for the resulting string are: An
		// empty string (a relative path on the current drive), "\" (an absolute 
		// path on the current drive), "X:" (a relative path on a given drive, 
		// where X is the drive letter), "X:\" (an absolute path on a given drive),
		// and "\\server\share" (a UNC path for a given server and share name). 
		// The resulting string is null if path is null.
		[System.Security.SecuritySafeCritical]  // auto-generated
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public static string GetPathRoot( string path )
		{
			if (path == null) return null;
			path = NormalizePath( path, false );
			return path.Substring( 0, GetRootLength( path ) );
		}

		[System.Security.SecuritySafeCritical]  // auto-generated
		[ResourceExposure( ResourceScope.Machine )]
		[ResourceConsumption( ResourceScope.Machine )]
		public static string GetTempPath()
		{
			//new EnvironmentPermission(PermissionState.Unrestricted).Demand();
			//StringBuilder sb = new StringBuilder(MAX_PATH); 
			//uint r = Win32Native.GetTempPath(MAX_PATH, sb);
			//string path = sb.ToString();
			//if (r==0) __Error.WinIOError();
			//path = GetFullPathInternal(path); 
			//return path;
			throw new NotImplementedException( "GetTempPath()" );
		}

		internal static bool IsRelative( string path )
		{
			Contract.Assert( path != null, "path can't be null" );
#if !PLATFORM_UNIX
			if ((path.Length >= 3 && path[1] == VolumeSeparatorChar && path[2] == DirectorySeparatorChar &&
				   ((path[0] >= 'a' && path[0] <= 'z') || (path[0] >= 'A' && path[0] <= 'Z'))) ||
				  (path.Length >= 2 && path[0] == '\\' && path[1] == '\\'))
#else 
            if(path.Length >= 1 && path[0] == VolumeSeparatorChar) 
#endif // !PLATFORM_UNIX
				return false;
			else
				return true;

		}

		// Returns a cryptographically strong random 8.3 string that can be 
		// used as either a folder name or a file name. 
		public static string GetRandomFileName()
		{
			// 5 bytes == 40 bits == 40/5 == 8 chars in our encoding
			// This gives us exactly 8 chars. We want to avoid the 8.3 short name issue
			byte[] key = new byte[10];

			// RNGCryptoServiceProvider is disposable in post-Orcas desktop mscorlibs, but not in CoreCLR's
			// mscorlib, so we need to do a manual using block for it. 
			RNGCryptoServiceProvider rng = null;
			try
			{
				rng = new RNGCryptoServiceProvider();

				rng.GetBytes( key );
				// rndCharArray is expected to be 16 chars 
				char[] rndCharArray = Path.ToBase32StringSuitableForDirName( key ).ToCharArray();
				rndCharArray[8] = '.';
				return new string( rndCharArray, 0, 12 );
			}
			finally
			{
				if (rng != null)
				{
					rng.Dispose();
				}
			}
		}

		// Returns a unique temporary file name, and creates a 0-byte file by that 
		// name on disk.
		[System.Security.SecuritySafeCritical]  // auto-generated
		[ResourceExposure( ResourceScope.AppDomain )]
		[ResourceConsumption( ResourceScope.Machine, ResourceScope.Machine )]
		public static string GetTempFileName()
		{
			throw new NotImplementedException( "GetTempFileName()" );
			//string path = GetTempPath(); 

			//// Since this can write to the temp directory and theoretically 
			//// cause a denial of service attack, demand FileIOPermission to
			//// that directory.
			//new FileIOPermission(FileIOPermissionAccess.Write, path).Demand();

			//StringBuilder sb = new StringBuilder(MAX_PATH);
			//uint r = Win32Native.GetTempFileName(path, "tmp", 0, sb); 
			//if (r==0) __Error.WinIOError(); 
			//return sb.ToString();
		}

		// Tests if a path includes a file extension. The result is
		// true if the characters that follow the last directory
		// separator ('\\' or '/') or volume separator (':') in the path include 
		// a period (".") other than a terminal period. The result is false otherwise.
		// 
		public static bool HasExtension( string path )
		{
			if (path != null)
			{
				CheckInvalidPathChars( path );

				for (int i = path.Length; --i >= 0; )
				{
					char ch = path[i];
					if (ch == '.')
					{
						if (i != path.Length - 1)
							return true;
						else
							return false;
					}
					if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar) break;
				}
			}
			return false;
		}


		// Tests if the given path contains a root. A path is considered rooted
		// if it starts with a backslash ("\") or a drive letter and a colon (":"). 
		//
		public static bool IsPathRooted( string path )
		{
			if (path != null)
			{
				CheckInvalidPathChars( path );

				int length = path.Length;
				if ((length >= 1 && (path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar))
#if !PLATFORM_UNIX
 || (length >= 2 && path[1] == VolumeSeparatorChar)
#endif
) return true;
			}
			return false;
		}

		public static string Combine( string path1, string path2 )
		{
			if (path1 == null || path2 == null)
				throw new ArgumentNullException( (path1 == null) ? "path1" : "path2" );
			Contract.EndContractBlock();
			CheckInvalidPathChars( path1 );
			CheckInvalidPathChars( path2 );

			return CombineNoChecks( path1, path2 );
		}

		public static string Combine( string path1, string path2, string path3 )
		{
			if (path1 == null || path2 == null || path3 == null)
				throw new ArgumentNullException( (path1 == null) ? "path1" : (path2 == null) ? "path2" : "path3" );
			Contract.EndContractBlock();
			CheckInvalidPathChars( path1 );
			CheckInvalidPathChars( path2 );
			CheckInvalidPathChars( path3 );

			return CombineNoChecks( CombineNoChecks( path1, path2 ), path3 );
		}

		public static string Combine( string path1, string path2, string path3, string path4 )
		{
			if (path1 == null || path2 == null || path3 == null || path4 == null)
				throw new ArgumentNullException( (path1 == null) ? "path1" : (path2 == null) ? "path2" : (path3 == null) ? "path3" : "path4" );
			Contract.EndContractBlock();
			CheckInvalidPathChars( path1 );
			CheckInvalidPathChars( path2 );
			CheckInvalidPathChars( path3 );
			CheckInvalidPathChars( path4 );

			return CombineNoChecks( CombineNoChecks( CombineNoChecks( path1, path2 ), path3 ), path4 );
		}

		public static string Combine( params string[] paths )
		{
			if (paths == null)
			{
				throw new ArgumentNullException( "paths" );
			}
			Contract.EndContractBlock();

			int finalSize = 0;
			int firstComponent = 0;

			// We have two passes, the first calcuates how large a buffer to allocate and does some precondition 
			// checks on the paths passed in.  The second actually does the combination.

			for (int i = 0; i < paths.Length; i++)
			{
				if (paths[i] == null)
				{
					throw new ArgumentNullException( "paths" );
				}

				if (paths[i].Length == 0)
				{
					continue;
				}

				CheckInvalidPathChars( paths[i] );

				if (Path.IsPathRooted( paths[i] ))
				{
					firstComponent = i;
					finalSize = paths[i].Length;
				}
				else
				{
					finalSize += paths[i].Length;
				}

				char ch = paths[i][paths[i].Length - 1];
				if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
					finalSize++;
			}

			StringBuilder finalPath = new StringBuilder( finalSize );

			for (int i = firstComponent; i < paths.Length; i++)
			{
				if (paths[i].Length == 0)
				{
					continue;
				}

				if (finalPath.Length == 0)
				{
					finalPath.Append( paths[i] );
				}
				else
				{
					char ch = finalPath[finalPath.Length - 1];
					if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
					{
						finalPath.Append( DirectorySeparatorChar );
					}

					finalPath.Append( paths[i] );
				}
			}

			return finalPath.ToString();
		}

		private static string CombineNoChecks( string path1, string path2 )
		{
			if (path2.Length == 0)
				return path1;

			if (path1.Length == 0)
				return path2;

			if (IsPathRooted( path2 ))
				return path2;

			char ch = path1[path1.Length - 1];
			if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
				return path1 + DirectorySeparatorChar + path2;
			return path1 + path2;
		}

		private static Char[] s_Base32Char = {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 
                'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 
                'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
                'y', 'z', '0', '1', '2', '3', '4', '5'};

		internal static string ToBase32StringSuitableForDirName( byte[] buff )
		{
			// This routine is optimised to be used with buffs of length 20 
			Contract.Assert( ((buff.Length % 5) == 0), "Unexpected hash length" );

			StringBuilder sb = new StringBuilder();
			byte b0, b1, b2, b3, b4;
			int l, i;

			l = buff.Length;
			i = 0;

			// Create l chars using the last 5 bits of each byte.
			// Consume 3 MSB bits 5 bytes at a time. 
			do
			{
				b0 = (i < l) ? buff[i++] : (byte) 0;
				b1 = (i < l) ? buff[i++] : (byte) 0;
				b2 = (i < l) ? buff[i++] : (byte) 0;
				b3 = (i < l) ? buff[i++] : (byte) 0;
				b4 = (i < l) ? buff[i++] : (byte) 0;

				// Consume the 5 Least significant bits of each byte 
				sb.Append( s_Base32Char[b0 & 0x1F] );
				sb.Append( s_Base32Char[b1 & 0x1F] );
				sb.Append( s_Base32Char[b2 & 0x1F] );
				sb.Append( s_Base32Char[b3 & 0x1F] );
				sb.Append( s_Base32Char[b4 & 0x1F] );

				// Consume 3 MSB of b0, b1, MSB bits 6, 7 of b3, b4
				sb.Append( s_Base32Char[(
						((b0 & 0xE0) >> 5) |
						((b3 & 0x60) >> 2))] );

				sb.Append( s_Base32Char[(
						((b1 & 0xE0) >> 5) |
						((b4 & 0x60) >> 2))] );

				// Consume 3 MSB bits of b2, 1 MSB bit of b3, b4
				b2 >>= 5;

				Contract.Assert( ((b2 & 0xF8) == 0), "Unexpected set bits" );

				if ((b3 & 0x80) != 0)
					b2 |= 0x08;
				if ((b4 & 0x80) != 0)
					b2 |= 0x10;

				sb.Append( s_Base32Char[b2] );

			} while (i < l);

			return sb.ToString();
		}

		// ".." can only be used if it is specified as a part of a valid File/Directory name. We disallow
		//  the user being able to use it to move up directories. Here are some examples eg 
		//    Valid: a..b  abc..d 
		//    Invalid: ..ab   ab..  ..   abc..d\abc..
		[System.Security.SecuritySafeCritical]  // auto-generated
		internal static void CheckSearchPattern( string searchPattern )
		{
			int index;
			while ((index = searchPattern.IndexOf( "..", StringComparison.Ordinal )) != -1)
			{

				if (index + 2 == searchPattern.Length) // Terminal ".." . Files names cannot end in ".." 
					throw new ArgumentException( "Arg_InvalidSearchPattern" );

				if ((searchPattern[index + 2] == DirectorySeparatorChar)
				   || (searchPattern[index + 2] == AltDirectorySeparatorChar))
					throw new ArgumentException( "Arg_InvalidSearchPattern" );

				searchPattern = searchPattern.Substring( index + 2 );
			}
		}

		internal static void CheckInvalidPathChars( string path )
		{
#if PLATFORM_UNIX
            if (path.Length >= 2 && path[0] == '\\' && path[1] == '\\') 
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
            Contract.EndContractBlock(); 
#endif // PLATFORM_UNIX

			for (int i = 0; i < path.Length; i++)
			{
				int c = path[i];

				// Note: This list is duplicated in static char[] InvalidPathChars 
				if (c == '\"' || c == '<' || c == '>' || c == '|' || c < 32)
					throw new ArgumentException( "Argument_InvalidPathChars" );
			}
		}

		internal static string InternalCombine( string path1, string path2 )
		{
			if (path1 == null || path2 == null)
				throw new ArgumentNullException( (path1 == null) ? "path1" : "path2" );
			Contract.EndContractBlock();
			CheckInvalidPathChars( path1 );
			CheckInvalidPathChars( path2 );

			if (path2.Length == 0)
				throw new ArgumentException( "Argument_PathEmpty", "path2" );
			if (IsPathRooted( path2 ))
				throw new ArgumentException( "Arg_Path2IsRooted", "path2" );
			int i = path1.Length;
			if (i == 0) return path2;
			char ch = path1[i - 1];
			if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
				return path1 + DirectorySeparatorChar + path2;
			return path1 + path2;
		}

		// Windows API definitions
		internal const int MAX_PATH = 260;  // From WinDef.h 
		internal const int MAX_DIRECTORY_PATH = 248;   // cannot create directories greater than 248 characters
	}

	/*using System;
	using System.Collections;
	using System.Text;
	using Microsoft.Win32;
	using System.Runtime.InteropServices;
	using System.Runtime.CompilerServices;
	using System.Globalization;
	using System.Runtime.Versioning;
	using System.Security;
	using System.Security.Permissions;
	using System.Diagnostics.Contracts;

	namespace System.IO {*/
	// ABOUT:
	// Helps with path normalization; support allocating on the stack or heap
	// 
	// PathHelper can't stackalloc the array for obvious reasons; you must pass
	// in an array of chars allocated on the stack.
	// 
	// USAGE:
	// Suppose you need to represent a char array of length len. Then this is the
	// suggested way to instantiate PathHelper:
	// ***************************************************************************
	// PathHelper pathHelper;
	// if (charArrayLength less than stack alloc threshold == Path.MaxPath)
	//     char* arrayPtr = stackalloc char[Path.MaxPath];
	//     pathHelper = new PathHelper(arrayPtr);
	// else
	//     pathHelper = new PathHelper(capacity, maxPath);
	// ***************************************************************************
	//
	// note in the StringBuilder ctor:
	// - maxPath may be greater than Path.MaxPath (for isolated storage)
	// - capacity may be greater than maxPath. This is even used for non-isolated
	//   storage scenarios where we want to temporarily allow strings greater 
	//   than Path.MaxPath if they can be normalized down to Path.MaxPath. This
	//   can happen if the path contains escape characters "..".
	// 
	/*unsafe internal struct PathHelper
	{   // should not be serialized

		// maximum size, max be greater than max path if contains escape sequence
		private int m_capacity;
		// current length (next character position)
		private int m_length;
		// max path, may be less than capacity
		private int m_maxPath;

		// ptr to stack alloc'd array of chars
		[SecurityCritical]
		private char* m_arrayPtr;

		// StringBuilder
		private StringBuilder m_sb;

		// whether to operate on stack alloc'd or heap alloc'd array 
		private bool useStackAlloc;

		// Whether to skip calls to Win32Native.GetLongPathName becasue we tried before and failed:
		//private bool doNotTryExpandShortFileName;

		// Instantiates a PathHelper with a stack alloc'd array of chars
		[System.Security.SecurityCritical]
		internal PathHelper( char* charArrayPtr, int length )
		{
			Contract.Requires( charArrayPtr != null );
			// force callers to be aware of this
			Contract.Requires( length == Path.MaxPath );
			this.m_length = 0;
			this.m_sb = null;

			this.m_arrayPtr = charArrayPtr;
			this.m_capacity = length;
			this.m_maxPath = Path.MaxPath;
			useStackAlloc = true;
			//useStackAlloc = false;   // RdS // WRONG!
			// doNotTryExpandShortFileName = false;
		}

		// Instantiates a PathHelper with a heap alloc'd array of ints. Will create a StringBuilder
		[System.Security.SecurityCritical]
		internal PathHelper( int capacity, int maxPath )
		{
			this.m_length = 0;
			this.m_arrayPtr = null;
			this.useStackAlloc = false;

			this.m_sb = new StringBuilder( capacity );
			this.m_capacity = capacity;
			this.m_maxPath = maxPath;
			//doNotTryExpandShortFileName = false;
		}

		internal int Length
		{
			get
			{
				if (useStackAlloc)
				{
					return m_length;
				}
				else
				{
					return m_sb.Length;
				}
			}
			set
			{
				if (useStackAlloc)
				{
					m_length = value;
				}
				else
				{
					m_sb.Length = value;
				}
			}
		}

		internal int Capacity
		{
			get
			{
				return m_capacity;
			}
		}

		internal char this[int index]
		{
			[System.Security.SecurityCritical]
			get
			{
				Contract.Requires( index >= 0 && index < Length );
				if (useStackAlloc)
				{
					return m_arrayPtr[index];
				}
				else
				{
					return m_sb[index];
				}
			}
			[System.Security.SecurityCritical]
			set
			{
				Contract.Requires( index >= 0 && index < Length );
				if (useStackAlloc)
				{
					m_arrayPtr[index] = value;
				}
				else
				{
					m_sb[index] = value;
				}
			}
		}

		[System.Security.SecurityCritical]
		internal unsafe void Append( char value )
		{
			if (Length + 1 >= m_capacity)
				throw new PathTooLongException( "IO.PathTooLong" );

			if (useStackAlloc)
			{
				m_arrayPtr[Length] = value;
				m_length++;
			}
			else
			{
				m_sb.Append( value );
			}
		}

		[System.Security.SecurityCritical]
		internal unsafe int GetFullPathName()
		{
			if (useStackAlloc)
			{
				char* finalBuffer = stackalloc char[Path.MaxPath + 1];
				throw new NotImplementedException( "Missing GetFullPathName implementation" );
				int result = 0;// Win32Native.GetFullPathName( m_arrayPtr, Path.MaxPath + 1, finalBuffer, IntPtr.Zero );

				// If success, the return buffer length does not account for the terminating null character.
				// If in-sufficient buffer, the return buffer length does account for the path + the terminating null character.
				// If failure, the return buffer length is zero 
				if (result > Path.MaxPath)
				{
					char* tempBuffer = stackalloc char[result];
					finalBuffer = tempBuffer;
					result = 0;// Win32Native.GetFullPathName( m_arrayPtr, result, finalBuffer, IntPtr.Zero );
				}

				// Full path is genuinely long
				if (result >= Path.MaxPath)
					throw new PathTooLongException( "IO.PathTooLong" );

				Contract.Assert( result < Path.MaxPath, "did we accidently remove a PathTooLongException check?" );
				if (result == 0 && m_arrayPtr[0] != '\0')
				{
					__Error.WinIOError();
				}

				else if (result < Path.MaxPath)
				{
					// Null terminate explicitly (may be only needed for some cases such as empty strings)
					// GetFullPathName return length doesn't account for null terminating char...
					finalBuffer[result] = '\0'; // Safe to write directly as result is < Path.MaxPath
				}

				// We have expanded the paths and GetLongPathName may or may not behave differently from before.
				// We need to call it again to see:
				//doNotTryExpandShortFileName = false;

				//string.wstrcpy( m_arrayPtr, finalBuffer, result );
				// Doesn't account for null terminating char. Think of this as the last
				// valid index into the buffer but not the length of the buffer
				Length = result;
				return result;
			}
			else
			{
				StringBuilder finalBuffer = new StringBuilder( m_capacity + 1 );
				throw new NotImplementedException( "Missing GetFullPathName implementation" );
				int result = 0;// Win32Native.GetFullPathName( m_sb.ToString(), m_capacity + 1, finalBuffer, IntPtr.Zero );

				// If success, the return buffer length does not account for the terminating null character.
				// If in-sufficient buffer, the return buffer length does account for the path + the terminating null character.
				// If failure, the return buffer length is zero 
				if (result > m_maxPath)
				{
					finalBuffer.Length = result;
					result = 0;// Win32Native.GetFullPathName( m_sb.ToString(), result, finalBuffer, IntPtr.Zero );
				}

				// Fullpath is genuinely long
				if (result >= m_maxPath)
					throw new PathTooLongException( "IO.PathTooLong" );

				Contract.Assert( result < m_maxPath, "did we accidentally remove a PathTooLongException check?" );
				if (result == 0 && m_sb[0] != '\0')
				{
					if (Length >= m_maxPath)
					{
						throw new PathTooLongException( "IO.PathTooLong" );
					}
					__Error.WinIOError();
				}

				// We have expanded the paths and GetLongPathName may or may not behave differently from before.
				// We need to call it again to see:
				//doNotTryExpandShortFileName = false;

				m_sb = finalBuffer;
				return result;
			}
		}

		[System.Security.SecurityCritical]
		internal unsafe bool TryExpandShortFileName()
		{
			throw new NotImplementedException( "TryExpandShortFileName()" );
			if (doNotTryExpandShortFileName)
				return false;

			if (useStackAlloc)
			{
				NullTerminate();
				char* buffer = UnsafeGetArrayPtr();
				char* shortFileNameBuffer = stackalloc char[Path.MaxPath + 1];

				int r = Win32Native.GetLongPathName( buffer, shortFileNameBuffer, Path.MaxPath );

				// If success, the return buffer length does not account for the terminating null character.
				// If in-sufficient buffer, the return buffer length does account for the path + the terminating null character.
				// If failure, the return buffer length is zero 
				if (r >= Path.MaxPath)
					throw new PathTooLongException( Environment.GetResourceString( "IO.PathTooLong" ) );

				if (r == 0)
				{
					// Note: GetLongPathName will return ERROR_INVALID_FUNCTION on a 
					// path like \\.\PHYSICALDEVICE0 - some device driver doesn't 
					// support GetLongPathName on that string.  This behavior is 
					// by design, according to the Core File Services team.
					// We also get ERROR_NOT_ENOUGH_QUOTA in SQL_CLR_STRESS runs
					// intermittently on paths like D:\DOCUME~1\user\LOCALS~1\Temp\

					// We do not need to call GetLongPathName if we know it will fail becasue the path does not exist:
					int lastErr = Marshal.GetLastWin32Error();
					if (lastErr == Win32Native.ERROR_FILE_NOT_FOUND || lastErr == Win32Native.ERROR_PATH_NOT_FOUND)
						doNotTryExpandShortFileName = true;

					return false;
				}

				// Safe to copy as we have already done Path.MaxPath bound checking 
				string.wstrcpy( buffer, shortFileNameBuffer, r );
				Length = r;
				// We should explicitly null terminate as in some cases the long version of the path 
				// might actually be shorter than what we started with because of Win32's normalization
				// Safe to write directly as bufferLength is guaranteed to be < Path.MaxPath
				NullTerminate();
				return true;
			}
			else
			{
				StringBuilder sb = GetStringBuilder();

				string origName = sb.ToString();
				string tempName = origName;
				bool addedPrefix = false;
				if (tempName.Length > Path.MaxPath)
				{
					tempName = Path.AddLongPathPrefix( tempName );
					addedPrefix = true;
				}
				sb.Capacity = m_capacity;
				sb.Length = 0;
				int r = Win32Native.GetLongPathName( tempName, sb, m_capacity );

				if (r == 0)
				{
					// Note: GetLongPathName will return ERROR_INVALID_FUNCTION on a 
					// path like \\.\PHYSICALDEVICE0 - some device driver doesn't 
					// support GetLongPathName on that string.  This behavior is 
					// by design, according to the Core File Services team.
					// We also get ERROR_NOT_ENOUGH_QUOTA in SQL_CLR_STRESS runs
					// intermittently on paths like D:\DOCUME~1\user\LOCALS~1\Temp\

					// We do not need to call GetLongPathName if we know it will fail becasue the path does not exist:
					int lastErr = Marshal.GetLastWin32Error();
					if (Win32Native.ERROR_FILE_NOT_FOUND == lastErr || Win32Native.ERROR_PATH_NOT_FOUND == lastErr)
						doNotTryExpandShortFileName = true;

					sb.Length = 0;
					sb.Append( origName );
					return false;
				}

				if (addedPrefix)
					r -= 4;

				// If success, the return buffer length does not account for the terminating null character.
				// If in-sufficient buffer, the return buffer length does account for the path + the terminating null character.
				// If failure, the return buffer length is zero 
				if (r >= m_maxPath)
					throw new PathTooLongException( "IO.PathTooLong" );


				sb = Path.RemoveLongPathPrefix( sb );
				Length = sb.Length;
				return true;
			}
		}

		[System.Security.SecurityCritical]
		internal unsafe void Fixup( int lenSavedName, int lastSlash )
		{
			//if (useStackAlloc) {
			//	char* savedName = stackalloc char[lenSavedName];
			//	string.wstrcpy(savedName, m_arrayPtr + lastSlash + 1, lenSavedName);
			//	Length = lastSlash;
			//	NullTerminate();
			//	doNotTryExpandShortFileName = false;
			//	bool r = TryExpandShortFileName();
			//	// Clean up changes made to the newBuffer.
			//	Append(Path.DirectorySeparatorChar);
			//	if (Length + lenSavedName >= Path.MaxPath)
			//		throw new PathTooLongException("IO.PathTooLong");
			//	string.wstrcpy(m_arrayPtr + Length, savedName, lenSavedName);
			//	Length = Length + lenSavedName;

			//}
			//else {
			string savedName = m_sb.ToString( lastSlash + 1, lenSavedName );
			Length = lastSlash;
			//  doNotTryExpandShortFileName = false;
			bool r = TryExpandShortFileName();
			// Clean up changes made to the newBuffer.
			Append( Path.DirectorySeparatorChar );
			if (Length + lenSavedName >= m_maxPath)
				throw new PathTooLongException( "IO.PathTooLong" );
			m_sb.Append( savedName );
			// }
		}

		[System.Security.SecurityCritical]
		internal unsafe bool OrdinalStartsWith( string compareTo, bool ignoreCase )
		{
			if (Length < compareTo.Length)
				return false;

			if (useStackAlloc)
			{
				NullTerminate();
				if (ignoreCase)
				{
					string s = new string( m_arrayPtr, 0, compareTo.Length );
					return compareTo.Equals( s, StringComparison.OrdinalIgnoreCase );
				}
				else
				{
					for (int i = 0; i < compareTo.Length; i++)
					{
						if (m_arrayPtr[i] != compareTo[i])
						{
							return false;
						}
					}
					return true;
				}
			}
			else
			{
				if (ignoreCase)
				{
					return m_sb.ToString().StartsWith( compareTo, StringComparison.OrdinalIgnoreCase );
				}
				else
				{
					return m_sb.ToString().StartsWith( compareTo, StringComparison.Ordinal );
				}
			}
		}

		[System.Security.SecuritySafeCritical]
		public override string ToString()
		{
			if (useStackAlloc)
			{
				return new string( m_arrayPtr, 0, Length );
			}
			else
			{
				return m_sb.ToString();
			}
		}

		[System.Security.SecurityCritical]
		private unsafe char* UnsafeGetArrayPtr()
		{
			Contract.Requires( useStackAlloc, "This should never be called for PathHelpers wrapping a StringBuilder" );
			return m_arrayPtr;
		}

		private StringBuilder GetStringBuilder()
		{
			Contract.Requires( !useStackAlloc, "This should never be called for PathHelpers that wrap a stackalloc'd buffer" );
			return m_sb;
		}

		[System.Security.SecurityCritical]
		private unsafe void NullTerminate()
		{
			Contract.Requires( useStackAlloc, "This should never be called for PathHelpers wrapping a StringBuilder" );
			m_arrayPtr[m_length] = '\0';
		}
	}*/

	/// <summary>Contains internal path helpers that are shared between many projects.</summary>
	internal static partial class PathInternal
	{
		/// <summary>
		/// Checks for invalid path characters in the given path.
		/// </summary>
		/// <exception cref="System.ArgumentNullException">Thrown if the path is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the path has invalid characters.</exception>
		/// <param name="path">The path to check for invalid characters.</param>
		/*internal static void CheckInvalidPathChars( string path )
		{
			if (path == null)
				throw new ArgumentNullException( "path null" );//nameof(path));

			// %TODO% - need source to HasIllegalCharacters()
			//if (HasIllegalCharacters(path))
			//	throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));
		}*/

		internal static bool IsPathTooLong( string path )
		{
			// TODO: Check this assumption
			return false;
		}

		/// <summary>
		/// Returns true if the given StringBuilder starts with the given value.
		/// </summary>
		/// <param name="value">The string to compare against the start of the StringBuilder.</param>
		/*internal static bool StartsWithOrdinal( this StringBuilder builder, string value )
		{
			if (value == null || builder.Length < value.Length)
				return false;

			for (int i = 0; i < value.Length; i++)
			{
				if (builder[i] != value[i]) return false;
			}
			return true;
		}*/

		/// <summary>
		/// Returns true if the given string starts with the given value.
		/// </summary>
		/// <param name="value">The string to compare against the start of the source string.</param>
		/*internal static bool StartsWithOrdinal( this string source, string value )
		{
			if (value == null || source.Length < value.Length)
				return false;

			return source.StartsWith( value, StringComparison.Ordinal );
		}*/

		/// <summary>
		/// Trims the specified characters from the end of the StringBuilder.
		/// </summary>
		/*internal static StringBuilder TrimEnd( this StringBuilder builder, params char[] trimChars )
		{
			if (trimChars == null || trimChars.Length == 0)
				return builder;

			int end = builder.Length - 1;

			for (; end >= 0; end--)
			{
				int i = 0;
				char ch = builder[end];
				for (; i < trimChars.Length; i++)
				{
					if (trimChars[i] == ch) break;
				}
				if (i == trimChars.Length)
				{
					// Not a trim char
					break;
				}
			}

			builder.Length = end + 1;
			return builder;
		}*/

		/// <summary>
		/// Returns the start index of the filename
		/// in the given path, or 0 if no directory
		/// or volume separator is found.
		/// </summary>
		/// <param name="path">The path in which to find the index of the filename.</param>
		/// <remarks>
		/// This method returns path.Length for
		/// inputs like "/usr/foo/" on Unix. As such,
		/// it is not safe for being used to index
		/// the string without additional verification.
		/// </remarks>
		/*internal static int FindFileNameIndex( string path )
		{
			Debug.Assert( path != null );
			CheckInvalidPathChars( path );

			for (int i = path.Length - 1; i >= 0; i--)
			{
				char ch = path[i];
				if (IsDirectoryOrVolumeSeparator( ch ))
					return i + 1;
			}

			return 0; // the whole path is the filename
		}*/

		/// <summary>
		/// Returns true if the path ends in a directory separator.
		/// </summary>
		/*internal static bool EndsInDirectorySeparator( string path )
		{
			return !string.IsNullOrEmpty( path ) && IsDirectorySeparator( path[path.Length - 1] );
		}*/

		/// <summary>
		/// Get the common path length from the start of the string.
		/// </summary>
		/*internal static int GetCommonPathLength( string first, string second, bool ignoreCase )
		{
			int commonChars = EqualStartingCharacterCount( first, second, ignoreCase: ignoreCase );

			// If nothing matches
			if (commonChars == 0)
				return commonChars;

			// Or we're a full string and equal length or match to a separator
			if (commonChars == first.Length
				&& (commonChars == second.Length || IsDirectorySeparator( second[commonChars] )))
				return commonChars;

			if (commonChars == second.Length && IsDirectorySeparator( first[commonChars] ))
				return commonChars;

			// It's possible we matched somewhere in the middle of a segment e.g. C:\Foodie and C:\Foobar.
			while (commonChars > 0 && !IsDirectorySeparator( first[commonChars - 1] ))
				commonChars--;

			return commonChars;
		}*/

		/// <summary>
		/// Gets the count of common characters from the left optionally ignoring case
		/// </summary>
		/*internal static unsafe int EqualStartingCharacterCount( string first, string second, bool ignoreCase )
		{
			if (string.IsNullOrEmpty( first ) || string.IsNullOrEmpty( second )) return 0;

			int commonChars = 0;

			fixed (char* f = first)
			fixed (char* s = second)
			{
				char* l = f;
				char* r = s;
				char* leftEnd = l + first.Length;
				char* rightEnd = r + second.Length;

				while (l != leftEnd && r != rightEnd
					&& (*l == *r || (ignoreCase && char.ToUpperInvariant( (*l) ) == char.ToUpperInvariant( (*r) ))))
				{
					commonChars++;
					l++;
					r++;
				}
			}

			return commonChars;
		}*/

		/// <summary>
		/// Returns true if the two paths have the same root
		/// </summary>
		/*internal static bool AreRootsEqual( string first, string second, StringComparison comparisonType )
		{
			int firstRootLength = GetRootLength( first );
			int secondRootLength = GetRootLength( second );

			return firstRootLength == secondRootLength
				&& string.Compare(
					strA: first,
					indexA: 0,
					strB: second,
					indexB: 0,
					length: firstRootLength,
					comparisonType: comparisonType ) == 0;
		}*/
	}

	[Serializable]
	[System.Runtime.InteropServices.ComVisible( true )]
	public class PathTooLongException : IOException
	{
		public PathTooLongException()
			: base( "IO.PathTooLong" )
		{
			//SetErrorCode( __HResults.COR_E_PATHTOOLONG );
		}

		public PathTooLongException( string message )
			: base( message )
		{
			//SetErrorCode( __HResults.COR_E_PATHTOOLONG );
		}

		public PathTooLongException( string message, Exception innerException )
			: base( message, innerException )
		{
			//SetErrorCode( __HResults.COR_E_PATHTOOLONG );
		}

		protected PathTooLongException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
		}
	}
}