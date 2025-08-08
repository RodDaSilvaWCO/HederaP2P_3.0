using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UNOSYS
namespace Unosys.Common.Types
#else
namespace PeerRepublic.Common.Types
#endif
{
    public interface ITypeDef
    {
		Type ID { get; }
		bool IsFixedSize { get;}
		bool IsSystemType { get;}
		string Description { get;}
		DateTime Version { get;}
		int MaxSize { get;}
		//string LoadCode { get;}
		//string SaveCode { get;}
		object Default( FieldDef field );
		byte[] DefaultAsBytes( FieldDef field );
		byte[] GetBytesFromObject( object Value, FieldDef field );
		object GetObjectFromBytes( byte[] fieldBytes, FieldDef field );
	}
}
