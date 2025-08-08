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
	public class FieldDef
	{
		#region Field Members
		public string Name;
		public string Description;
		public int Ordinal;
		public int Length;
		public int Decimals;
		public bool IsNullable;
		public bool IsMultiValued;
		public TypeEncoding FieldEncoding;
		public string DefaultValue;
		public TypeDef TypeDefinition;
		#endregion

		#region Constructors
		public FieldDef() { }

		public FieldDef( TypeDef typeDef )
		{
			TypeDefinition = typeDef;
		}

		public FieldDef( Type type )
		{
			TypeDefinition = TypeDef.Factory( type );
		}
		#endregion

		#region Public Interface
		#region Properties
		//public int MaxSize
		//{
		//	get
		//	{
		//		return Length;
		//	}
		//}

		public object Default
		{
			get
			{
				return TypeDefinition.Default( this );
			}
		}

		public byte[] DefaultAsBytes
		{
			get
			{
				return TypeDefinition.DefaultAsBytes( this );
			}
		}

		public bool HasDefaultValue
		{
			get
			{
				return !string.IsNullOrEmpty( DefaultValue );
			}
		}

		//public byte[] EmptyValueBytes
		//{

		//}

		//public byte[] DefaultValueBytes
		//{
		//	get 
		//	{ 
		//		if( string.IsNullOrEmpty( DefaultValue ))
		//		{
		//			Debug.Print( "Boom!" );
		//			return null;  // equivalent to empty string
		//		}
		//		else
		//		{
		//			switch (Encoding)
		//			{
		//				case TypeEncoding.None:  // default encoding if not specified
		//				case TypeEncoding.ASCII:
		//					return System.Text.Encoding.ASCII.GetBytes( DefaultValue );
		//				case TypeEncoding.ANSI:
		//					return System.Text.Encoding.Default.GetBytes( DefaultValue );
		//				case TypeEncoding.UTF7:
		//					return System.Text.Encoding.UTF7.GetBytes( DefaultValue );
		//				case TypeEncoding.UTF8:
		//					return System.Text.Encoding.UTF8.GetBytes( DefaultValue );
		//				case TypeEncoding.UTF32:
		//					return System.Text.Encoding.UTF32.GetBytes( DefaultValue );
		//				case TypeEncoding.Unicode:
		//					return System.Text.Encoding.Unicode.GetBytes( DefaultValue );
		//				case TypeEncoding.BigEndianUnicode:
		//					return System.Text.Encoding.BigEndianUnicode.GetBytes( DefaultValue );
		//				default:
		//					throw new InvalidOperationException( string.Format( "Invlid Encoding {0}.", Encoding ) );
		//			}
		//		}
				
		//	}
		//}
		#endregion

		#region Methods
		public byte[] GetBytesFromObject( object Value )
		{
			return TypeDefinition.GetBytesFromObject( Value, this );
		}

		public object GetObjectFromBytes( byte[] fieldBytes )
		{
			return TypeDefinition.GetObjectFromBytes( fieldBytes, this );
		}

		public Encoding DefaultEncoding
		{
			get
			{
				Encoding fldEncoding = null;
				switch (FieldEncoding)
				{
					case TypeEncoding.ASCII:
						fldEncoding = Encoding.ASCII;
						break;
					case TypeEncoding.ANSI:
						fldEncoding = Encoding.Default;
						break;
					case TypeEncoding.BigEndianUnicode:
						fldEncoding = Encoding.BigEndianUnicode;
						break;
					case TypeEncoding.UTF32:
						fldEncoding = Encoding.UTF32;
						break;
					case TypeEncoding.UTF7:
						fldEncoding = Encoding.UTF7;
						break;
					case TypeEncoding.UTF8:
						fldEncoding = Encoding.UTF8;
						break;
					case TypeEncoding.Unicode:
						fldEncoding = Encoding.Unicode;
						break;

					default:
						fldEncoding = null;
						break;
				}
				return fldEncoding;
			}
		}
		#endregion
		#endregion

	}
}
