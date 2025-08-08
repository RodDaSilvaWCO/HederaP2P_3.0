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
    public class CharacterTypeDef : TypeDef
    {

		#region Field Members
		#endregion

		#region Constructors
		public CharacterTypeDef() { }
		#endregion

		#region  Implementation
		public override object Default( FieldDef field )
		{ 
			if (string.IsNullOrEmpty( field.DefaultValue ))
			{
				return "".PadRight( field.Length );
			}
			else
			{
				return field.DefaultValue.PadRight( field.Length );
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
				if (Value is string)
				{
					// Pad string to fixed size
					string v = ((string) Value).PadRight( field.Length );
					if (v.Length != field.Length)  // can happen if original string was longer than field.Length (Padding is ignored in this case)
						throw new InvalidOperationException( "Invalid field length." );
					switch (field.FieldEncoding)
					{
						case TypeEncoding.None:
						case TypeEncoding.ASCII:
							return Encoding.ASCII.GetBytes( v );

						case TypeEncoding.ANSI:
							return Encoding.Default.GetBytes( v );

						case TypeEncoding.UTF7:
							return Encoding.UTF7.GetBytes( v );

						case TypeEncoding.UTF8:
							return Encoding.UTF8.GetBytes( v );

						case TypeEncoding.UTF32:
							return Encoding.UTF32.GetBytes( v );

						case TypeEncoding.Unicode:
							return Encoding.Unicode.GetBytes( v );

						case TypeEncoding.BigEndianUnicode:
							return Encoding.BigEndianUnicode.GetBytes( v );


						default:
							throw new InvalidOperationException( string.Format( "Encoding {0} not permitted for CHARACTER field.", field.FieldEncoding ) );
					}
				}
				else
				{
					throw new InvalidOperationException( "Field value is not a valid CHARACTER." );
				}
			}
		}

		public override object GetObjectFromBytes( byte[] fieldBytes, FieldDef field )
		{
			if (fieldBytes == null)
			{
				throw new InvalidOperationException( "Invalid field value." );
			}


			switch (field.FieldEncoding)
			{
				case TypeEncoding.None:
				case TypeEncoding.ASCII:
					return Encoding.ASCII.GetString( fieldBytes );

				case TypeEncoding.ANSI:
					return Encoding.Default.GetString( fieldBytes );

				case TypeEncoding.UTF7:
					return Encoding.UTF7.GetString( fieldBytes );

				case TypeEncoding.UTF8:
					return Encoding.UTF8.GetString( fieldBytes );

				case TypeEncoding.UTF32:
					return Encoding.UTF32.GetString( fieldBytes );

				case TypeEncoding.Unicode:
					return Encoding.Unicode.GetString( fieldBytes );

				case TypeEncoding.BigEndianUnicode:
					return Encoding.BigEndianUnicode.GetString( fieldBytes );


				default:
					throw new InvalidOperationException( string.Format( "Encoding {0} not permitted for CHARACTER field.", field.FieldEncoding ) );
			}
		}

		public override Type ID
		{
			get { return Type.CHARACTER; }
		}

		public override string Description
		{
			get { return "Fixed-length, non-Unicode string data from 1 through 8,000 characters in length"; }
		}



		public override int MaxSize
		{
			get { return 8000; }
		}


		#endregion
	}
}
