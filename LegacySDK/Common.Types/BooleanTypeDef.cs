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
    public class BooleanTypeDef : TypeDef
    {
		#region Constructors
		public BooleanTypeDef() { }
		#endregion

		#region  Implementation
		public override object Default( FieldDef field )
		{ 
			if (string.IsNullOrEmpty( field.DefaultValue ))
			{
				return false;
			}
			else
			{
				try
				{
					return bool.Parse( field.DefaultValue );
				}
				catch
				{
					throw new InvalidOperationException( string.Format( "Invalid default literal value for BOOLEAN field." ) );
				}
			}
		}


		public override byte[] GetBytesFromObject( object Value, FieldDef field )
		{
			if (field.IsNullable && Value == null)
			{
				return null;
			}
			else
			{
				if (Value is bool)
				{
					return BitConverter.GetBytes( (bool) Value );
				}
				else
				{
					throw new InvalidOperationException( "Field value is not a valid BOOLEAN." );
				}
			}
		}

		public override object GetObjectFromBytes( byte[] fieldBytes, FieldDef field )
		{
			if (fieldBytes == null || fieldBytes.Length != 1)
			{
				throw new InvalidOperationException( "Invalid field value." );
			}

			return BitConverter.ToBoolean( fieldBytes, 0 );
		}

		public override Type ID
		{
			get { return Type.BOOLEAN; }
		}

		public override string Description
		{
			get { return "Logical two state value storing true or false"; }
		}


		public override int MaxSize
		{
			get { return sizeof(bool); }
		}


		#endregion
	}
}
