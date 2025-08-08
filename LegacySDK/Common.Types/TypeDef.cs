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
    public class TypeDef : ITypeDef
    {
		static public TypeDef Factory( Type type )
		{
			TypeDef result = null;
			switch( type )
			{
				case Type.CHARACTER:
					result = new CharacterTypeDef();
					break;

				case Type.INT32:
					result = new Signed32BitIntegerTypeDef();
					break;

				case Type.BOOLEAN:
					result = new BooleanTypeDef();
					break;

				default:
					throw new InvalidOperationException( "Unknown Type." );
			}
			return result;
		}

		#region Constructors
		public TypeDef() { }
		#endregion

		#region ITypeDef Implementation
		public virtual object Default( FieldDef field )
		{
			throw new NotImplementedException();
			//if (IsSystemType)
			//{
			//	if (id == Type.CHARACTER)  
			//	{
			//		if (string.IsNullOrEmpty( field.DefaultValue ))
			//		{
			//			return "".PadRight( field.Length );
			//		}
			//		else
			//		{
			//			return field.DefaultValue.PadRight( field.Length );
			//		}
			//	}
			//	if (id == Type.BOOLEAN)
			//	{
			//		if (string.IsNullOrEmpty( field.DefaultValue ))
			//		{
			//			return false;
			//		}
			//		else
			//		{
			//			bool result;
			//			if (bool.TryParse( field.DefaultValue, out result ))
			//			{
			//				return result;
			//			}
			//			else
			//			{
			//				throw new InvalidOperationException( "Invalid default value for BOOLEAN field." );
			//			}
			//		}
			//	}
			//	//if( ID == Type.DATE || ID == Type.TIME || ID == Type.TIMESTAMP)
			//	//{
			//	//	return DateTime.MinValue;
			//	//}
			//	if (id == Type.INT8 || id == Type.INT16 || id == Type.INT32 || id == Type.INT64 || id == Type.UINT8 || id == Type.UINT16 || id == Type.UINT32 || id == Type.UINT64)
			//	{
			//		if (string.IsNullOrEmpty( field.DefaultValue ))
			//		{
			//			return 0;
			//		}
			//		else
			//		{
			//			try
			//			{
			//				switch (id)
			//				{
			//					case Type.INT8:
			//						return sbyte.Parse( field.DefaultValue );
			//					case Type.UINT8:
			//						return byte.Parse( field.DefaultValue );
			//					case Type.INT16:
			//						return short.Parse( field.DefaultValue );
			//					case Type.UINT16:
			//						return ushort.Parse( field.DefaultValue );
			//					case Type.INT32:
			//						return int.Parse( field.DefaultValue );
			//					case Type.UINT32:
			//						return uint.Parse( field.DefaultValue );
			//					case Type.INT64:
			//						return long.Parse( field.DefaultValue );
			//					case Type.UINT64:
			//						return ulong.Parse( field.DefaultValue );
			//					default:
			//						throw new InvalidOperationException( "Unknow Type." );
			//				}
			//			}
			//			catch
			//			{
			//				throw new InvalidOperationException( string.Format( "Invalid default literal value for {0} field.", id ) );
			//			}
			//		}
			//	}
				
			//	throw new InvalidOperationException("Unknown type.");
			//}
			//else
			//{ 
			//	throw new NotImplementedException();
			//}
		}

		public virtual byte[] DefaultAsBytes( FieldDef field )
		{
			object defaultFieldValue = Default(field);
			byte[] defaultFieldValueAsBytes = GetBytesFromObject( defaultFieldValue, field );
			return defaultFieldValueAsBytes;
		}

		public virtual byte[] GetBytesFromObject( object Value, FieldDef field )
		{
			throw new NotImplementedException();
			//if (IsSystemType)
			//{
			//	switch (id)
			//	{
			//		case Type.CHARACTER:
			//			if (field.IsNullable && Value == null)
			//			{
			//				return null;
			//			}
			//			else
			//			{
			//				if (Value is string)
			//				{
			//					// Pad string to fixed size
			//					string v = ((string) Value).PadRight( field.Length );
			//					if (v.Length != field.Length)  // can happen if original string was longer than field.Length (Padding is ignored in this case)
			//						throw new InvalidOperationException( "Invalid field length." );
			//					switch (field.FieldEncoding)
			//					{
			//						case TypeEncoding.None:
			//						case TypeEncoding.ASCII:
			//							return Encoding.ASCII.GetBytes( v );

			//						case TypeEncoding.ANSI:
			//							return Encoding.Default.GetBytes( v );

			//						case TypeEncoding.UTF7:
			//							return Encoding.UTF7.GetBytes( v );

			//						case TypeEncoding.UTF8:
			//							return Encoding.UTF8.GetBytes( v );

			//						case TypeEncoding.UTF32:
			//							return Encoding.UTF32.GetBytes( v );

			//						case TypeEncoding.Unicode:
			//							return Encoding.Unicode.GetBytes( v );

			//						case TypeEncoding.BigEndianUnicode:
			//							return Encoding.BigEndianUnicode.GetBytes( v );


			//						default:
			//							throw new InvalidOperationException( string.Format( "Encoding {0} not permitted for CHARACTER field.", field.FieldEncoding ) );
			//					}
			//				}
			//				else
			//				{
			//					throw new InvalidOperationException( "Field value is not a valid CHARACTER." );
			//				}
			//			}

			//		case Type.INT32:
			//			if (Value is Int32)
			//			{
			//				return BitConverter.GetBytes( (int) Value );
			//			}
			//			else
			//			{
			//				throw new InvalidOperationException( "Field value is not a valid INT32" );
			//			}

			//		default:
			//			throw new InvalidOperationException( "Unknown type." );
			//	}
			//	//if (ID == Type.BOOLEAN)
			//	//{
			//	//	if (field.IsNullable && Value == null)
			//	//	{
			//	//		return null;
			//	//	}
			//	//	else
			//	//	{
			//	//		if (!(Value is bool))
			//	//			throw new InvalidOperationException( "Value not a valid BOOLEAN." );

			//	//		return ((bool)Value ? new byte[] {1} : new byte[] {0});
			//	//	}
			//	//}
			//	//if (ID == Type.DATE || ID == Type.TIME || ID == Type.TIMESTAMP)
			//	//{

			//	//	return DateTime.MinValue;
			//	//}
			//	//if (ID == Type.INT8 || ID == Type.INT16 || ID == Type.INT32 || ID == Type.INT64 || ID == Type.UINT8 || ID == Type.UINT16 || ID == Type.UINT32 || ID == Type.UINT64)
			//	//{
			//	//	return 0;
			//	//}

			//}
			//else
			//{
			//	throw new NotImplementedException();
			//}
		}

		public virtual object GetObjectFromBytes( byte[] fieldBytes, FieldDef field )
		{
			throw new NotImplementedException();
			//if (fieldBytes == null)
			//	throw new InvalidOperationException( "Invalid field value." );

			//if (IsSystemType)
			//{
			//	switch (id)
			//	{
			//		case Type.CHARACTER:

			//			switch (field.FieldEncoding)
			//			{
			//				case TypeEncoding.None:
			//				case TypeEncoding.ASCII:
			//					return Encoding.ASCII.GetString( fieldBytes );

			//				case TypeEncoding.ANSI:
			//					return Encoding.Default.GetString( fieldBytes );

			//				case TypeEncoding.UTF7:
			//					return Encoding.UTF7.GetString( fieldBytes );

			//				case TypeEncoding.UTF8:
			//					return Encoding.UTF8.GetString( fieldBytes );

			//				case TypeEncoding.UTF32:
			//					return Encoding.UTF32.GetString( fieldBytes );

			//				case TypeEncoding.Unicode:
			//					return Encoding.Unicode.GetString( fieldBytes );

			//				case TypeEncoding.BigEndianUnicode:
			//					return Encoding.BigEndianUnicode.GetString( fieldBytes );


			//				default:
			//					throw new InvalidOperationException( string.Format( "Encoding {0} not permitted for CHARACTER field.", field.FieldEncoding ) );
			//			}

			//		case Type.INT32:
			//			return BitConverter.ToInt32( fieldBytes, 0 );

			//		default:
			//			throw new InvalidOperationException( "Unknown type." );
			//	}
			//	//if (ID == Type.BOOLEAN)
			//	//{
			//	//	if (field.IsNullable && Value == null)
			//	//	{
			//	//		return null;
			//	//	}
			//	//	else
			//	//	{
			//	//		if (!(Value is bool))
			//	//			throw new InvalidOperationException( "Value not a valid BOOLEAN." );

			//	//		return ((bool) Value ? new byte[] { 1 } : new byte[] { 0 });
			//	//	}
			//	//}
			//	//if (ID == Type.DATE || ID == Type.TIME || ID == Type.TIMESTAMP)
			//	//{

			//	//	return DateTime.MinValue;
			//	//}
			//	//if (ID == Type.INT8 || ID == Type.INT16 || ID == Type.INT32 || ID == Type.INT64 || ID == Type.UINT8 || ID == Type.UINT16 || ID == Type.UINT32 || ID == Type.UINT64)
			//	//{
			//	//	return 0;
			//	//}

			//}
			//else
			//{
			//	throw new NotImplementedException();
			//}
		}

		public virtual Type ID
		{
			get { throw new NotImplementedException(); }
		}

		public virtual bool IsFixedSize
		{
			get { return true; }  // default
		}

		public virtual bool IsSystemType
		{
			get { return true; }  // default
		}

		public virtual string Description
		{
			get { throw new NotImplementedException(); }
		}

		public virtual DateTime Version
		{
			get { throw new NotImplementedException(); }
		}

		public virtual int MaxSize
		{
			get { throw new NotImplementedException(); }
		}

		//public virtual string  LoadCode
		//{
		//	get { throw new NotImplementedException(); }
		//}

		//public virtual string  SaveCode
		//{
		//	get { throw new NotImplementedException(); }
		//}
		#endregion
	}
}
