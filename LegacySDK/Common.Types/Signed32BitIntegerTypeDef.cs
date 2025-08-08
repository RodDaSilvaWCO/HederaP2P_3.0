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
    [Serializable]
    public class Signed32BitIntegerTypeDef : TypeDef
    {

		#region Constructors
		public Signed32BitIntegerTypeDef() { }
		#endregion

		#region Implementation
		public override object Default( FieldDef field )
		{ 
			if (string.IsNullOrEmpty( field.DefaultValue ))
			{
				return 0;
			}
			else
			{
				try
				{
					return int.Parse( field.DefaultValue );
				}
				catch
				{
					throw new InvalidOperationException( string.Format( "Invalid default literal value for INT32 field." ) );
				}
			}
		}


		public override byte[] GetBytesFromObject( object Value, FieldDef field )
		{

			if (Value is Int32)
			{
				return BitConverter.GetBytes( (int) Value );
			}
			else
			{
				throw new InvalidOperationException( "Field value is not a valid INT32" );
			}
		}

		public override object GetObjectFromBytes( byte[] fieldBytes, FieldDef field )
		{
			if (fieldBytes == null || fieldBytes.Length != MaxSize) 
			{
				throw new InvalidOperationException( "Invalid field value." );
			}

			return BitConverter.ToInt32( fieldBytes, 0 );
		}

		public override Type ID
		{
			get { return Type.INT32; }
		}


		public override string Description
		{
			get { return "Signed 32-bit integer"; }
		}


		public override int MaxSize
		{
			get { return 4; }   // size of Int32 is 4 bytes
		}

		#endregion
	}
}
