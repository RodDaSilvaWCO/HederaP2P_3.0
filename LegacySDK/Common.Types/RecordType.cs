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
    public enum RecordType
	{
		Small = sizeof( sbyte ),
		Medium = sizeof( short ),
		Large = sizeof( int )
	}
}
