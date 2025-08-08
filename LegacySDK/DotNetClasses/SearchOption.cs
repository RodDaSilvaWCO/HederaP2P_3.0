using System;

namespace Unosys.SDK
{
	[Serializable]
	[System.Runtime.InteropServices.ComVisible( true )]
	public enum SearchOption
	{
		// Include only the current directory in the search operation
		TopDirectoryOnly,

		// Include the current directory and all the sub-directories
		// underneath it including reparse points in the search operation. 
		// This will traverse reparse points (i.e, mounted points and symbolic links)
		// recursively. If the directory structure searched contains a loop
		// because of hard links, the search operation will go on for ever. 
		AllDirectories,
	}
}