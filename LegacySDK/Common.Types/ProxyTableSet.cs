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
	public class ProxyTableSet
	{
		public Guid SETID;
		public Guid SCID;
		public Guid SCHID;
		public SetSchema Schema;
		public ulong EndOfSet;
		public byte[] SymKey;
		public int MaxRecordSize;
		public ProxyTableSet() { }
	}
}
