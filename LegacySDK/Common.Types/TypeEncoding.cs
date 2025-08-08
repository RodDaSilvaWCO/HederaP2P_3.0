using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UNOSYS
namespace Unosys.Common.Types
#else
namespace PeerRepublic.Common.Types
#endif
{
    [Serializable]
	public enum TypeEncoding
	{
		/*
TEID	Encoding	Description
1	ASCII	ASCII (7-bit) character set
2	ANSI	ANSI code page
3	UTF7	UTF-7 format
4	UTF8	UTF-8 format
5	UTF32	UTF-32 format using the little endian byte order
6	Unicode	UTF-16 format using the little endian byte order
8	BigEndianUnicode	UTF-16 format that uses the big endian byte order		 
		 */
		None = 0,
		ASCII = 1,
		ANSI,
		UTF7,
		UTF8,
		UTF32,
		Unicode,
		BigEndianUnicode
	}
}
