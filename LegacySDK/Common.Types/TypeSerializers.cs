#if _DYNAMIC_XMLSERIALIZER_COMPILATION
[assembly:System.Security.AllowPartiallyTrustedCallers()]
[assembly:System.Security.SecurityTransparent()]
[assembly:System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
#endif
[assembly: System.Xml.Serialization.XmlSerializerVersionAttribute( ParentAssemblyId = @"af7caa65-1f41-4f67-ae33-2985c5fff4c4,", Version = @"4.0.0.0" )]
#if UNOSYS
namespace Unosys.Common.Types
#else
namespace PeerRepublic.Common.Types
#endif
{

 //   public class XmlSerializationWriter1 : System.Xml.Serialization.XmlSerializationWriter
	//{

	//	public void Write10_FieldDef( object o )
	//	{
	//		WriteStartDocument();
	//		if (o == null)
	//		{
	//			WriteNullTagLiteral( @"FieldDef", @"" );
	//			return;
	//		}
	//		TopLevelElement();

	//		Write4_FieldDef( @"FieldDef", @"", ((FieldDef) o), true, false );
 //       }

	//	public void Write11_SetSchema( object o )
	//	{
	//		WriteStartDocument();
	//		if (o == null)
	//		{
	//			WriteNullTagLiteral( @"SetSchema", @"" );
	//			return;
	//		}
	//		TopLevelElement();
	//		Write5_SetSchema( @"SetSchema", @"", ((SetSchema) o), true, false );
	//	}

	//	public void Write12_TypeDef( object o )
	//	{
	//		WriteStartDocument();
	//		if (o == null)
	//		{
	//			WriteNullTagLiteral( @"TypeDef", @"" );
	//			return;
	//		}
	//		TopLevelElement();
	//		Write3_TypeDef( @"TypeDef", @"", ((TypeDef) o), true, false );
	//	}

	//	public void Write13_TypeEncoding( object o )
	//	{
	//		WriteStartDocument();
	//		if (o == null)
	//		{
	//			WriteEmptyTag( @"TypeEncoding", @"" );
	//			return;
	//		}
	//		WriteElementString( @"TypeEncoding", @"", Write1_TypeEncoding( ((TypeEncoding) o) ) );
	//	}

	//	public void Write14_CharacterTypeDef( object o )
	//	{
	//		WriteStartDocument();
	//		if (o == null)
	//		{
	//			WriteNullTagLiteral( @"CharacterTypeDef", @"" );
	//			return;
	//		}
	//		TopLevelElement();
	//		Write6_CharacterTypeDef( @"CharacterTypeDef", @"", ((CharacterTypeDef) o), true, false );
	//	}

	//	public void Write15_Signed32BitIntegerTypeDef( object o )
	//	{
	//		WriteStartDocument();
	//		if (o == null)
	//		{
	//			WriteNullTagLiteral( @"Signed32BitIntegerTypeDef", @"" );
	//			return;
	//		}
	//		TopLevelElement();
	//		Write7_Signed32BitIntegerTypeDef( @"Signed32BitIntegerTypeDef", @"", ((Signed32BitIntegerTypeDef) o), true, false );
	//	}

	//	public void Write16_BooleanTypeDef( object o )
	//	{
	//		WriteStartDocument();
	//		if (o == null)
	//		{
	//			WriteNullTagLiteral( @"BooleanTypeDef", @"" );
	//			return;
	//		}
	//		TopLevelElement();
	//		Write8_BooleanTypeDef( @"BooleanTypeDef", @"", ((BooleanTypeDef) o), true, false );
	//	}

	//	public void Write17_ProxyTableSet( object o )
	//	{
	//		WriteStartDocument();
	//		if (o == null)
	//		{
	//			WriteNullTagLiteral( @"ProxyTableSet", @"" );
	//			return;
	//		}
	//		TopLevelElement();
	//		Write9_ProxyTableSet( @"ProxyTableSet", @"", ((ProxyTableSet) o), true, false );
	//	}

	//	void Write9_ProxyTableSet( string n, string ns, ProxyTableSet o, bool isNullable, bool needType )
	//	{
	//		if ((object) o == null)
	//		{
	//			if (isNullable) WriteNullTagLiteral( n, ns );
	//			return;
	//		}
	//		if (!needType)
	//		{
	//			System.Type t = o.GetType();
	//			if (t == typeof( ProxyTableSet ))
	//			{
	//			}
	//			else
	//			{
	//				throw CreateUnknownTypeException( o );
	//			}
	//		}
	//		WriteStartElement( n, ns, o, false, null );
	//		if (needType) WriteXsiType( @"ProxyTableSet", @"" );
	//		WriteElementStringRaw( @"SETID", @"", System.Xml.XmlConvert.ToString( (global::System.Guid) ((global::System.Guid) o.@SETID) ) );
	//		WriteElementStringRaw( @"SCID", @"", System.Xml.XmlConvert.ToString( (global::System.Guid) ((global::System.Guid) o.@SCID) ) );
	//		WriteElementStringRaw( @"SCHID", @"", System.Xml.XmlConvert.ToString( (global::System.Guid) ((global::System.Guid) o.@SCHID) ) );
	//		Write5_SetSchema( @"Schema", @"", ((SetSchema) o.@Schema), false, false );
	//		WriteElementStringRaw( @"EndOfSet", @"", System.Xml.XmlConvert.ToString( (global::System.UInt64) ((global::System.UInt64) o.@EndOfSet) ) );
	//		WriteElementStringRaw( @"SymKey", @"", FromByteArrayBase64( ((global::System.Byte[]) o.@SymKey) ) );
	//		WriteElementStringRaw( @"MaxRecordSize", @"", System.Xml.XmlConvert.ToString( (global::System.Int32) ((global::System.Int32) o.@MaxRecordSize) ) );
	//		WriteEndElement( o );
	//	}

	//	void Write5_SetSchema( string n, string ns, SetSchema o, bool isNullable, bool needType )
	//	{
	//		if ((object) o == null)
	//		{
	//			if (isNullable) WriteNullTagLiteral( n, ns );
	//			return;
	//		}
	//		if (!needType)
	//		{
	//			System.Type t = o.GetType();
	//			if (t == typeof( SetSchema ))
	//			{
	//			}
	//			else
	//			{
	//				throw CreateUnknownTypeException( o );
	//			}
	//		}
	//		WriteStartElement( n, ns, o, false, null );
	//		if (needType) WriteXsiType( @"SetSchema", @"" );
	//		WriteElementStringRaw( @"Version", @"", System.Xml.XmlConvert.ToString( (global::System.Byte) ((global::System.Byte) o.@Version) ) );
	//		{
	//			FieldDef[] a = (FieldDef[]) ((FieldDef[]) o.@Fields);
	//			if (a != null)
	//			{
	//				WriteStartElement( @"Fields", @"", null, false );
	//				for (int ia = 0; ia < a.Length; ia++)
	//				{
	//					Write4_FieldDef( @"FieldDef", @"", ((FieldDef) a[ia]), true, false );
	//				}
	//				WriteEndElement();
	//			}
	//		}
	//		WriteEndElement( o );
	//	}

	//	void Write4_FieldDef( string n, string ns, FieldDef o, bool isNullable, bool needType )
	//	{
	//		if ((object) o == null)
	//		{
	//			if (isNullable) WriteNullTagLiteral( n, ns );
	//			return;
	//		}
	//		if (!needType)
	//		{
	//			System.Type t = o.GetType();
	//			if (t == typeof( FieldDef ))
	//			{
	//			}
	//			else
	//			{
	//				throw CreateUnknownTypeException( o );
	//			}
	//		}
	//		WriteStartElement( n, ns, o, false, null );
	//		if (needType) WriteXsiType( @"FieldDef", @"" );
	//		WriteElementString( @"Name", @"", ((global::System.String) o.@Name) );
	//		WriteElementString( @"Description", @"", ((global::System.String) o.@Description) );
	//		WriteElementStringRaw( @"Ordinal", @"", System.Xml.XmlConvert.ToString( (global::System.Int32) ((global::System.Int32) o.@Ordinal) ) );
	//		WriteElementStringRaw( @"Length", @"", System.Xml.XmlConvert.ToString( (global::System.Int32) ((global::System.Int32) o.@Length) ) );
	//		WriteElementStringRaw( @"Decimals", @"", System.Xml.XmlConvert.ToString( (global::System.Int32) ((global::System.Int32) o.@Decimals) ) );
	//		WriteElementStringRaw( @"IsNullable", @"", System.Xml.XmlConvert.ToString( (global::System.Boolean) ((global::System.Boolean) o.@IsNullable) ) );
	//		WriteElementStringRaw( @"IsMultiValued", @"", System.Xml.XmlConvert.ToString( (global::System.Boolean) ((global::System.Boolean) o.@IsMultiValued) ) );
	//		WriteElementString( @"FieldEncoding", @"", Write1_TypeEncoding( ((TypeEncoding) o.@FieldEncoding) ) );
	//		WriteElementString( @"DefaultValue", @"", ((global::System.String) o.@DefaultValue) );
	//		Write3_TypeDef( @"TypeDefinition", @"", ((TypeDef) o.@TypeDefinition), false, false );
	//		WriteEndElement( o );
	//	}

	//	void Write3_TypeDef( string n, string ns, TypeDef o, bool isNullable, bool needType )
	//	{
	//		if ((object) o == null)
	//		{
	//			if (isNullable) WriteNullTagLiteral( n, ns );
	//			return;
	//		}
	//		if (!needType)
	//		{
	//			System.Type t = o.GetType();
	//			if (t == typeof( TypeDef ))
	//			{
	//			}
	//			else if (t == typeof( BooleanTypeDef ))
	//			{
	//				Write8_BooleanTypeDef( n, ns, (BooleanTypeDef) o, isNullable, true );
	//				return;
	//			}
	//			else if (t == typeof( Signed32BitIntegerTypeDef ))
	//			{
	//				Write7_Signed32BitIntegerTypeDef( n, ns, (Signed32BitIntegerTypeDef) o, isNullable, true );
	//				return;
	//			}
	//			else if (t == typeof( CharacterTypeDef ))
	//			{
	//				Write6_CharacterTypeDef( n, ns, (CharacterTypeDef) o, isNullable, true );
	//				return;
	//			}
	//			else
	//			{
	//				throw CreateUnknownTypeException( o );
	//			}
	//		}
	//		WriteStartElement( n, ns, o, false, null );
	//		if (needType) WriteXsiType( @"TypeDef", @"" );
	//		WriteEndElement( o );
	//	}

	//	void Write6_CharacterTypeDef( string n, string ns, CharacterTypeDef o, bool isNullable, bool needType )
	//	{
	//		if ((object) o == null)
	//		{
	//			if (isNullable) WriteNullTagLiteral( n, ns );
	//			return;
	//		}
	//		if (!needType)
	//		{
	//			System.Type t = o.GetType();
	//			if (t == typeof( CharacterTypeDef ))
	//			{
	//			}
	//			else
	//			{
	//				throw CreateUnknownTypeException( o );
	//			}
	//		}
	//		WriteStartElement( n, ns, o, false, null );
	//		if (needType) WriteXsiType( @"CharacterTypeDef", @"" );
	//		WriteEndElement( o );
	//	}

	//	void Write7_Signed32BitIntegerTypeDef( string n, string ns, Signed32BitIntegerTypeDef o, bool isNullable, bool needType )
	//	{
	//		if ((object) o == null)
	//		{
	//			if (isNullable) WriteNullTagLiteral( n, ns );
	//			return;
	//		}
	//		if (!needType)
	//		{
	//			System.Type t = o.GetType();
	//			if (t == typeof( Signed32BitIntegerTypeDef ))
	//			{
	//			}
	//			else
	//			{
	//				throw CreateUnknownTypeException( o );
	//			}
	//		}
	//		WriteStartElement( n, ns, o, false, null );
	//		if (needType) WriteXsiType( @"Signed32BitIntegerTypeDef", @"" );
	//		WriteEndElement( o );
	//	}

	//	void Write8_BooleanTypeDef( string n, string ns, BooleanTypeDef o, bool isNullable, bool needType )
	//	{
	//		if ((object) o == null)
	//		{
	//			if (isNullable) WriteNullTagLiteral( n, ns );
	//			return;
	//		}
	//		if (!needType)
	//		{
	//			System.Type t = o.GetType();
	//			if (t == typeof( BooleanTypeDef ))
	//			{
	//			}
	//			else
	//			{
	//				throw CreateUnknownTypeException( o );
	//			}
	//		}
	//		WriteStartElement( n, ns, o, false, null );
	//		if (needType) WriteXsiType( @"BooleanTypeDef", @"" );
	//		WriteEndElement( o );
	//	}

	//	string Write1_TypeEncoding( TypeEncoding v )
	//	{
	//		string s = null;
	//		switch (v)
	//		{
	//			case TypeEncoding.@None: s = @"None"; break;
	//			case TypeEncoding.@ASCII: s = @"ASCII"; break;
	//			case TypeEncoding.@ANSI: s = @"ANSI"; break;
	//			case TypeEncoding.@UTF7: s = @"UTF7"; break;
	//			case TypeEncoding.@UTF8: s = @"UTF8"; break;
	//			case TypeEncoding.@UTF32: s = @"UTF32"; break;
	//			case TypeEncoding.@Unicode: s = @"Unicode"; break;
	//			case TypeEncoding.@BigEndianUnicode: s = @"BigEndianUnicode"; break;
	//			default: throw CreateInvalidEnumValueException( ((System.Int64) v).ToString( System.Globalization.CultureInfo.InvariantCulture ), @"Unosys.Common.Types.TypeEncoding" );
	//		}
	//		return s;
	//	}

	//	protected override void InitCallbacks()
	//	{
	//	}
	//}

	//public class XmlSerializationReader1 : System.Xml.Serialization.XmlSerializationReader
	//{

	//	public object Read10_FieldDef()
	//	{
	//		object o = null;
	//		Reader.MoveToContent();
	//		if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//		{
	//			if (((object) Reader.LocalName == (object) id1_FieldDef && (object) Reader.NamespaceURI == (object) id2_Item))
	//			{
	//				o = Read4_FieldDef( true, true );
	//			}
	//			else
	//			{
	//				throw CreateUnknownNodeException();
	//			}
	//		}
	//		else
	//		{
	//			UnknownNode( null, @":FieldDef" );
	//		}
	//		return (object) o;
	//	}

	//	public object Read11_SetSchema()
	//	{
	//		object o = null;
	//		Reader.MoveToContent();
	//		if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//		{
	//			if (((object) Reader.LocalName == (object) id3_SetSchema && (object) Reader.NamespaceURI == (object) id2_Item))
	//			{
	//				o = Read5_SetSchema( true, true );
	//			}
	//			else
	//			{
	//				throw CreateUnknownNodeException();
	//			}
	//		}
	//		else
	//		{
	//			UnknownNode( null, @":SetSchema" );
	//		}
	//		return (object) o;
	//	}

	//	public object Read12_TypeDef()
	//	{
	//		object o = null;
	//		Reader.MoveToContent();
	//		if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//		{
	//			if (((object) Reader.LocalName == (object) id4_TypeDef && (object) Reader.NamespaceURI == (object) id2_Item))
	//			{
	//				o = Read3_TypeDef( true, true );
	//			}
	//			else
	//			{
	//				throw CreateUnknownNodeException();
	//			}
	//		}
	//		else
	//		{
	//			UnknownNode( null, @":TypeDef" );
	//		}
	//		return (object) o;
	//	}

	//	public object Read13_TypeEncoding()
	//	{
	//		object o = null;
	//		Reader.MoveToContent();
	//		if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//		{
	//			if (((object) Reader.LocalName == (object) id5_TypeEncoding && (object) Reader.NamespaceURI == (object) id2_Item))
	//			{
	//				{
	//					o = Read1_TypeEncoding( Reader.ReadElementString() );
	//				}
	//			}
	//			else
	//			{
	//				throw CreateUnknownNodeException();
	//			}
	//		}
	//		else
	//		{
	//			UnknownNode( null, @":TypeEncoding" );
	//		}
	//		return (object) o;
	//	}

	//	public object Read14_CharacterTypeDef()
	//	{
	//		object o = null;
	//		Reader.MoveToContent();
	//		if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//		{
	//			if (((object) Reader.LocalName == (object) id6_CharacterTypeDef && (object) Reader.NamespaceURI == (object) id2_Item))
	//			{
	//				o = Read6_CharacterTypeDef( true, true );
	//			}
	//			else
	//			{
	//				throw CreateUnknownNodeException();
	//			}
	//		}
	//		else
	//		{
	//			UnknownNode( null, @":CharacterTypeDef" );
	//		}
	//		return (object) o;
	//	}

	//	public object Read15_Signed32BitIntegerTypeDef()
	//	{
	//		object o = null;
	//		Reader.MoveToContent();
	//		if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//		{
	//			if (((object) Reader.LocalName == (object) id7_Signed32BitIntegerTypeDef && (object) Reader.NamespaceURI == (object) id2_Item))
	//			{
	//				o = Read7_Signed32BitIntegerTypeDef( true, true );
	//			}
	//			else
	//			{
	//				throw CreateUnknownNodeException();
	//			}
	//		}
	//		else
	//		{
	//			UnknownNode( null, @":Signed32BitIntegerTypeDef" );
	//		}
	//		return (object) o;
	//	}

	//	public object Read16_BooleanTypeDef()
	//	{
	//		object o = null;
	//		Reader.MoveToContent();
	//		if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//		{
	//			if (((object) Reader.LocalName == (object) id8_BooleanTypeDef && (object) Reader.NamespaceURI == (object) id2_Item))
	//			{
	//				o = Read8_BooleanTypeDef( true, true );
	//			}
	//			else
	//			{
	//				throw CreateUnknownNodeException();
	//			}
	//		}
	//		else
	//		{
	//			UnknownNode( null, @":BooleanTypeDef" );
	//		}
	//		return (object) o;
	//	}

	//	public object Read17_ProxyTableSet()
	//	{
	//		object o = null;
	//		Reader.MoveToContent();
	//		if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//		{
	//			if (((object) Reader.LocalName == (object) id9_ProxyTableSet && (object) Reader.NamespaceURI == (object) id2_Item))
	//			{
	//				o = Read9_ProxyTableSet( true, true );
	//			}
	//			else
	//			{
	//				throw CreateUnknownNodeException();
	//			}
	//		}
	//		else
	//		{
	//			UnknownNode( null, @":ProxyTableSet" );
	//		}
	//		return (object) o;
	//	}

	//	ProxyTableSet Read9_ProxyTableSet( bool isNullable, bool checkType )
	//	{
	//		System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
	//		bool isNull = false;
	//		if (isNullable) isNull = ReadNull();
	//		if (checkType)
	//		{
	//			if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id9_ProxyTableSet && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//			{
	//			}
	//			else
	//				throw CreateUnknownTypeException( (System.Xml.XmlQualifiedName) xsiType );
	//		}
	//		if (isNull) return null;
	//		ProxyTableSet o;
	//		o = new ProxyTableSet();
	//		bool[] paramsRead = new bool[7];
	//		while (Reader.MoveToNextAttribute())
	//		{
	//			if (!IsXmlnsAttribute( Reader.Name ))
	//			{
	//				UnknownNode( (object) o );
	//			}
	//		}
	//		Reader.MoveToElement();
	//		if (Reader.IsEmptyElement)
	//		{
	//			Reader.Skip();
	//			return o;
	//		}
	//		Reader.ReadStartElement();
	//		Reader.MoveToContent();
	//		int whileIterations0 = 0;
	//		int readerCount0 = ReaderCount;
	//		while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
	//		{
	//			if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//			{
	//				if (!paramsRead[0] && ((object) Reader.LocalName == (object) id10_SETID && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@SETID = System.Xml.XmlConvert.ToGuid( Reader.ReadElementString() );
	//					}
	//					paramsRead[0] = true;
	//				}
	//				else if (!paramsRead[1] && ((object) Reader.LocalName == (object) id11_SCID && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@SCID = System.Xml.XmlConvert.ToGuid( Reader.ReadElementString() );
	//					}
	//					paramsRead[1] = true;
	//				}
	//				else if (!paramsRead[2] && ((object) Reader.LocalName == (object) id12_SCHID && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@SCHID = System.Xml.XmlConvert.ToGuid( Reader.ReadElementString() );
	//					}
	//					paramsRead[2] = true;
	//				}
	//				else if (!paramsRead[3] && ((object) Reader.LocalName == (object) id13_Schema && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					o.@Schema = Read5_SetSchema( false, true );
	//					paramsRead[3] = true;
	//				}
	//				else if (!paramsRead[4] && ((object) Reader.LocalName == (object) id14_EndOfSet && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@EndOfSet = System.Xml.XmlConvert.ToUInt64( Reader.ReadElementString() );
	//					}
	//					paramsRead[4] = true;
	//				}
	//				else if (!paramsRead[5] && ((object) Reader.LocalName == (object) id15_SymKey && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@SymKey = ToByteArrayBase64( false );
	//					}
	//					paramsRead[5] = true;
	//				}
	//				else if (!paramsRead[6] && ((object) Reader.LocalName == (object) id16_MaxRecordSize && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@MaxRecordSize = System.Xml.XmlConvert.ToInt32( Reader.ReadElementString() );
	//					}
	//					paramsRead[6] = true;
	//				}
	//				else
	//				{
	//					UnknownNode( (object) o, @":SETID, :SCID, :SCHID, :Schema, :EndOfSet, :SymKey, :MaxRecordSize" );
	//				}
	//			}
	//			else
	//			{
	//				UnknownNode( (object) o, @":SETID, :SCID, :SCHID, :Schema, :EndOfSet, :SymKey, :MaxRecordSize" );
	//			}
	//			Reader.MoveToContent();
	//			CheckReaderCount( ref whileIterations0, ref readerCount0 );
	//		}
	//		ReadEndElement();
	//		return o;
	//	}

	//	SetSchema Read5_SetSchema( bool isNullable, bool checkType )
	//	{
	//		System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
	//		bool isNull = false;
	//		if (isNullable) isNull = ReadNull();
	//		if (checkType)
	//		{
	//			if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id3_SetSchema && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//			{
	//			}
	//			else
	//				throw CreateUnknownTypeException( (System.Xml.XmlQualifiedName) xsiType );
	//		}
	//		if (isNull) return null;
	//		SetSchema o;
	//		o = new SetSchema();
	//		bool[] paramsRead = new bool[2];
	//		while (Reader.MoveToNextAttribute())
	//		{
	//			if (!IsXmlnsAttribute( Reader.Name ))
	//			{
	//				UnknownNode( (object) o );
	//			}
	//		}
	//		Reader.MoveToElement();
	//		if (Reader.IsEmptyElement)
	//		{
	//			Reader.Skip();
	//			return o;
	//		}
	//		Reader.ReadStartElement();
	//		Reader.MoveToContent();
	//		int whileIterations1 = 0;
	//		int readerCount1 = ReaderCount;
	//		while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
	//		{
	//			if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//			{
	//				if (!paramsRead[0] && ((object) Reader.LocalName == (object) id17_Version && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@Version = System.Xml.XmlConvert.ToByte( Reader.ReadElementString() );
	//					}
	//					paramsRead[0] = true;
	//				}
	//				else if (((object) Reader.LocalName == (object) id18_Fields && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					if (!ReadNull())
	//					{
	//						FieldDef[] a_1_0 = null;
	//						int ca_1_0 = 0;
	//						if ((Reader.IsEmptyElement))
	//						{
	//							Reader.Skip();
	//						}
	//						else
	//						{
	//							Reader.ReadStartElement();
	//							Reader.MoveToContent();
	//							int whileIterations2 = 0;
	//							int readerCount2 = ReaderCount;
	//							while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
	//							{
	//								if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//								{
	//									if (((object) Reader.LocalName == (object) id1_FieldDef && (object) Reader.NamespaceURI == (object) id2_Item))
	//									{
	//										a_1_0 = (FieldDef[]) EnsureArrayIndex( a_1_0, ca_1_0, typeof( FieldDef ) ); a_1_0[ca_1_0++] = Read4_FieldDef( true, true );
	//									}
	//									else
	//									{
	//										UnknownNode( null, @":FieldDef" );
	//									}
	//								}
	//								else
	//								{
	//									UnknownNode( null, @":FieldDef" );
	//								}
	//								Reader.MoveToContent();
	//								CheckReaderCount( ref whileIterations2, ref readerCount2 );
	//							}
	//							ReadEndElement();
	//						}
	//						o.@Fields = (FieldDef[]) ShrinkArray( a_1_0, ca_1_0, typeof( FieldDef ), false );
	//					}
	//				}
	//				else
	//				{
	//					UnknownNode( (object) o, @":Version, :Fields" );
	//				}
	//			}
	//			else
	//			{
	//				UnknownNode( (object) o, @":Version, :Fields" );
	//			}
	//			Reader.MoveToContent();
	//			CheckReaderCount( ref whileIterations1, ref readerCount1 );
	//		}
	//		ReadEndElement();
	//		return o;
	//	}

	//	FieldDef Read4_FieldDef( bool isNullable, bool checkType )
	//	{
	//		System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
	//		bool isNull = false;
	//		if (isNullable) isNull = ReadNull();
	//		if (checkType)
	//		{
	//			if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id1_FieldDef && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//			{
	//			}
	//			else
	//				throw CreateUnknownTypeException( (System.Xml.XmlQualifiedName) xsiType );
	//		}
	//		if (isNull) return null;
	//		FieldDef o;
	//		o = new FieldDef();
	//		bool[] paramsRead = new bool[10];
	//		while (Reader.MoveToNextAttribute())
	//		{
	//			if (!IsXmlnsAttribute( Reader.Name ))
	//			{
	//				UnknownNode( (object) o );
	//			}
	//		}
	//		Reader.MoveToElement();
	//		if (Reader.IsEmptyElement)
	//		{
	//			Reader.Skip();
	//			return o;
	//		}
	//		Reader.ReadStartElement();
	//		Reader.MoveToContent();
	//		int whileIterations3 = 0;
	//		int readerCount3 = ReaderCount;
	//		while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
	//		{
	//			if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//			{
	//				if (!paramsRead[0] && ((object) Reader.LocalName == (object) id19_Name && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@Name = Reader.ReadElementString();
	//					}
	//					paramsRead[0] = true;
	//				}
	//				else if (!paramsRead[1] && ((object) Reader.LocalName == (object) id20_Description && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@Description = Reader.ReadElementString();
	//					}
	//					paramsRead[1] = true;
	//				}
	//				else if (!paramsRead[2] && ((object) Reader.LocalName == (object) id21_Ordinal && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@Ordinal = System.Xml.XmlConvert.ToInt32( Reader.ReadElementString() );
	//					}
	//					paramsRead[2] = true;
	//				}
	//				else if (!paramsRead[3] && ((object) Reader.LocalName == (object) id22_Length && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@Length = System.Xml.XmlConvert.ToInt32( Reader.ReadElementString() );
	//					}
	//					paramsRead[3] = true;
	//				}
	//				else if (!paramsRead[4] && ((object) Reader.LocalName == (object) id23_Decimals && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@Decimals = System.Xml.XmlConvert.ToInt32( Reader.ReadElementString() );
	//					}
	//					paramsRead[4] = true;
	//				}
	//				else if (!paramsRead[5] && ((object) Reader.LocalName == (object) id24_IsNullable && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@IsNullable = System.Xml.XmlConvert.ToBoolean( Reader.ReadElementString() );
	//					}
	//					paramsRead[5] = true;
	//				}
	//				else if (!paramsRead[6] && ((object) Reader.LocalName == (object) id25_IsMultiValued && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@IsMultiValued = System.Xml.XmlConvert.ToBoolean( Reader.ReadElementString() );
	//					}
	//					paramsRead[6] = true;
	//				}
	//				else if (!paramsRead[7] && ((object) Reader.LocalName == (object) id26_FieldEncoding && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@FieldEncoding = Read1_TypeEncoding( Reader.ReadElementString() );
	//					}
	//					paramsRead[7] = true;
	//				}
	//				else if (!paramsRead[8] && ((object) Reader.LocalName == (object) id27_DefaultValue && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					{
	//						o.@DefaultValue = Reader.ReadElementString();
	//					}
	//					paramsRead[8] = true;
	//				}
	//				else if (!paramsRead[9] && ((object) Reader.LocalName == (object) id28_TypeDefinition && (object) Reader.NamespaceURI == (object) id2_Item))
	//				{
	//					o.@TypeDefinition = Read3_TypeDef( false, true );
	//					paramsRead[9] = true;
	//				}
	//				else
	//				{
	//					UnknownNode( (object) o, @":Name, :Description, :Ordinal, :Length, :Decimals, :IsNullable, :IsMultiValued, :FieldEncoding, :DefaultValue, :TypeDefinition" );
	//				}
	//			}
	//			else
	//			{
	//				UnknownNode( (object) o, @":Name, :Description, :Ordinal, :Length, :Decimals, :IsNullable, :IsMultiValued, :FieldEncoding, :DefaultValue, :TypeDefinition" );
	//			}
	//			Reader.MoveToContent();
	//			CheckReaderCount( ref whileIterations3, ref readerCount3 );
	//		}
	//		ReadEndElement();
	//		return o;
	//	}

	//	TypeDef Read3_TypeDef( bool isNullable, bool checkType )
	//	{
	//		System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
	//		bool isNull = false;
	//		if (isNullable) isNull = ReadNull();
	//		if (checkType)
	//		{
	//			if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id4_TypeDef && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//			{
	//			}
	//			else if (((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id8_BooleanTypeDef && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//				return Read8_BooleanTypeDef( isNullable, false );
	//			else if (((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id7_Signed32BitIntegerTypeDef && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//				return Read7_Signed32BitIntegerTypeDef( isNullable, false );
	//			else if (((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id6_CharacterTypeDef && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//				return Read6_CharacterTypeDef( isNullable, false );
	//			else
	//				throw CreateUnknownTypeException( (System.Xml.XmlQualifiedName) xsiType );
	//		}
	//		if (isNull) return null;
	//		TypeDef o;
	//		o = new TypeDef();
	//		bool[] paramsRead = new bool[0];
	//		while (Reader.MoveToNextAttribute())
	//		{
	//			if (!IsXmlnsAttribute( Reader.Name ))
	//			{
	//				UnknownNode( (object) o );
	//			}
	//		}
	//		Reader.MoveToElement();
	//		if (Reader.IsEmptyElement)
	//		{
	//			Reader.Skip();
	//			return o;
	//		}
	//		Reader.ReadStartElement();
	//		Reader.MoveToContent();
	//		int whileIterations4 = 0;
	//		int readerCount4 = ReaderCount;
	//		while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
	//		{
	//			if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//			{
	//				UnknownNode( (object) o, @"" );
	//			}
	//			else
	//			{
	//				UnknownNode( (object) o, @"" );
	//			}
	//			Reader.MoveToContent();
	//			CheckReaderCount( ref whileIterations4, ref readerCount4 );
	//		}
	//		ReadEndElement();
	//		return o;
	//	}

	//	CharacterTypeDef Read6_CharacterTypeDef( bool isNullable, bool checkType )
	//	{
	//		System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
	//		bool isNull = false;
	//		if (isNullable) isNull = ReadNull();
	//		if (checkType)
	//		{
	//			if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id6_CharacterTypeDef && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//			{
	//			}
	//			else
	//				throw CreateUnknownTypeException( (System.Xml.XmlQualifiedName) xsiType );
	//		}
	//		if (isNull) return null;
	//		CharacterTypeDef o;
	//		o = new CharacterTypeDef();
	//		bool[] paramsRead = new bool[0];
	//		while (Reader.MoveToNextAttribute())
	//		{
	//			if (!IsXmlnsAttribute( Reader.Name ))
	//			{
	//				UnknownNode( (object) o );
	//			}
	//		}
	//		Reader.MoveToElement();
	//		if (Reader.IsEmptyElement)
	//		{
	//			Reader.Skip();
	//			return o;
	//		}
	//		Reader.ReadStartElement();
	//		Reader.MoveToContent();
	//		int whileIterations5 = 0;
	//		int readerCount5 = ReaderCount;
	//		while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
	//		{
	//			if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//			{
	//				UnknownNode( (object) o, @"" );
	//			}
	//			else
	//			{
	//				UnknownNode( (object) o, @"" );
	//			}
	//			Reader.MoveToContent();
	//			CheckReaderCount( ref whileIterations5, ref readerCount5 );
	//		}
	//		ReadEndElement();
	//		return o;
	//	}

	//	Signed32BitIntegerTypeDef Read7_Signed32BitIntegerTypeDef( bool isNullable, bool checkType )
	//	{
	//		System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
	//		bool isNull = false;
	//		if (isNullable) isNull = ReadNull();
	//		if (checkType)
	//		{
	//			if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id7_Signed32BitIntegerTypeDef && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//			{
	//			}
	//			else
	//				throw CreateUnknownTypeException( (System.Xml.XmlQualifiedName) xsiType );
	//		}
	//		if (isNull) return null;
	//		Signed32BitIntegerTypeDef o;
	//		o = new Signed32BitIntegerTypeDef();
	//		bool[] paramsRead = new bool[0];
	//		while (Reader.MoveToNextAttribute())
	//		{
	//			if (!IsXmlnsAttribute( Reader.Name ))
	//			{
	//				UnknownNode( (object) o );
	//			}
	//		}
	//		Reader.MoveToElement();
	//		if (Reader.IsEmptyElement)
	//		{
	//			Reader.Skip();
	//			return o;
	//		}
	//		Reader.ReadStartElement();
	//		Reader.MoveToContent();
	//		int whileIterations6 = 0;
	//		int readerCount6 = ReaderCount;
	//		while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
	//		{
	//			if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//			{
	//				UnknownNode( (object) o, @"" );
	//			}
	//			else
	//			{
	//				UnknownNode( (object) o, @"" );
	//			}
	//			Reader.MoveToContent();
	//			CheckReaderCount( ref whileIterations6, ref readerCount6 );
	//		}
	//		ReadEndElement();
	//		return o;
	//	}

	//	BooleanTypeDef Read8_BooleanTypeDef( bool isNullable, bool checkType )
	//	{
	//		System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
	//		bool isNull = false;
	//		if (isNullable) isNull = ReadNull();
	//		if (checkType)
	//		{
	//			if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName) xsiType).Name == (object) id8_BooleanTypeDef && (object) ((System.Xml.XmlQualifiedName) xsiType).Namespace == (object) id2_Item))
	//			{
	//			}
	//			else
	//				throw CreateUnknownTypeException( (System.Xml.XmlQualifiedName) xsiType );
	//		}
	//		if (isNull) return null;
	//		BooleanTypeDef o;
	//		o = new BooleanTypeDef();
	//		bool[] paramsRead = new bool[0];
	//		while (Reader.MoveToNextAttribute())
	//		{
	//			if (!IsXmlnsAttribute( Reader.Name ))
	//			{
	//				UnknownNode( (object) o );
	//			}
	//		}
	//		Reader.MoveToElement();
	//		if (Reader.IsEmptyElement)
	//		{
	//			Reader.Skip();
	//			return o;
	//		}
	//		Reader.ReadStartElement();
	//		Reader.MoveToContent();
	//		int whileIterations7 = 0;
	//		int readerCount7 = ReaderCount;
	//		while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None)
	//		{
	//			if (Reader.NodeType == System.Xml.XmlNodeType.Element)
	//			{
	//				UnknownNode( (object) o, @"" );
	//			}
	//			else
	//			{
	//				UnknownNode( (object) o, @"" );
	//			}
	//			Reader.MoveToContent();
	//			CheckReaderCount( ref whileIterations7, ref readerCount7 );
	//		}
	//		ReadEndElement();
	//		return o;
	//	}

	//	TypeEncoding Read1_TypeEncoding( string s )
	//	{
	//		switch (s)
	//		{
	//			case @"None": return TypeEncoding.@None;
	//			case @"ASCII": return TypeEncoding.@ASCII;
	//			case @"ANSI": return TypeEncoding.@ANSI;
	//			case @"UTF7": return TypeEncoding.@UTF7;
	//			case @"UTF8": return TypeEncoding.@UTF8;
	//			case @"UTF32": return TypeEncoding.@UTF32;
	//			case @"Unicode": return TypeEncoding.@Unicode;
	//			case @"BigEndianUnicode": return TypeEncoding.@BigEndianUnicode;
	//			default: throw CreateUnknownConstantException( s, typeof( TypeEncoding ) );
	//		}
	//	}

	//	protected override void InitCallbacks()
	//	{
	//	}

	//	string id7_Signed32BitIntegerTypeDef;
	//	string id13_Schema;
	//	string id3_SetSchema;
	//	string id21_Ordinal;
	//	string id22_Length;
	//	string id23_Decimals;
	//	string id20_Description;
	//	string id6_CharacterTypeDef;
	//	string id4_TypeDef;
	//	string id15_SymKey;
	//	string id1_FieldDef;
	//	string id17_Version;
	//	string id5_TypeEncoding;
	//	string id27_DefaultValue;
	//	string id2_Item;
	//	string id9_ProxyTableSet;
	//	string id25_IsMultiValued;
	//	string id19_Name;
	//	string id16_MaxRecordSize;
	//	string id24_IsNullable;
	//	string id28_TypeDefinition;
	//	string id14_EndOfSet;
	//	string id10_SETID;
	//	string id8_BooleanTypeDef;
	//	string id11_SCID;
	//	string id12_SCHID;
	//	string id18_Fields;
	//	string id26_FieldEncoding;

	//	protected override void InitIDs()
	//	{
	//		id7_Signed32BitIntegerTypeDef = Reader.NameTable.Add( @"Signed32BitIntegerTypeDef" );
	//		id13_Schema = Reader.NameTable.Add( @"Schema" );
	//		id3_SetSchema = Reader.NameTable.Add( @"SetSchema" );
	//		id21_Ordinal = Reader.NameTable.Add( @"Ordinal" );
	//		id22_Length = Reader.NameTable.Add( @"Length" );
	//		id23_Decimals = Reader.NameTable.Add( @"Decimals" );
	//		id20_Description = Reader.NameTable.Add( @"Description" );
	//		id6_CharacterTypeDef = Reader.NameTable.Add( @"CharacterTypeDef" );
	//		id4_TypeDef = Reader.NameTable.Add( @"TypeDef" );
	//		id15_SymKey = Reader.NameTable.Add( @"SymKey" );
	//		id1_FieldDef = Reader.NameTable.Add( @"FieldDef" );
	//		id17_Version = Reader.NameTable.Add( @"Version" );
	//		id5_TypeEncoding = Reader.NameTable.Add( @"TypeEncoding" );
	//		id27_DefaultValue = Reader.NameTable.Add( @"DefaultValue" );
	//		id2_Item = Reader.NameTable.Add( @"" );
	//		id9_ProxyTableSet = Reader.NameTable.Add( @"ProxyTableSet" );
	//		id25_IsMultiValued = Reader.NameTable.Add( @"IsMultiValued" );
	//		id19_Name = Reader.NameTable.Add( @"Name" );
	//		id16_MaxRecordSize = Reader.NameTable.Add( @"MaxRecordSize" );
	//		id24_IsNullable = Reader.NameTable.Add( @"IsNullable" );
	//		id28_TypeDefinition = Reader.NameTable.Add( @"TypeDefinition" );
	//		id14_EndOfSet = Reader.NameTable.Add( @"EndOfSet" );
	//		id10_SETID = Reader.NameTable.Add( @"SETID" );
	//		id8_BooleanTypeDef = Reader.NameTable.Add( @"BooleanTypeDef" );
	//		id11_SCID = Reader.NameTable.Add( @"SCID" );
	//		id12_SCHID = Reader.NameTable.Add( @"SCHID" );
	//		id18_Fields = Reader.NameTable.Add( @"Fields" );
	//		id26_FieldEncoding = Reader.NameTable.Add( @"FieldEncoding" );
	//	}
	//}

	//public abstract class XmlSerializer1 : System.Xml.Serialization.XmlSerializer
	//{
	//	protected override System.Xml.Serialization.XmlSerializationReader CreateReader()
	//	{
	//		return new XmlSerializationReader1();
	//	}
	//	protected override System.Xml.Serialization.XmlSerializationWriter CreateWriter()
	//	{
	//		return new XmlSerializationWriter1();
	//	}
	//}

	//public sealed class FieldDefSerializer : XmlSerializer1
	//{

	//	public override System.Boolean CanDeserialize( System.Xml.XmlReader xmlReader )
	//	{
	//		return xmlReader.IsStartElement( @"FieldDef", @"" );
	//	}

	//	protected override void Serialize( object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer )
	//	{
	//		((XmlSerializationWriter1) writer).Write10_FieldDef( objectToSerialize );
	//	}

	//	protected override object Deserialize( System.Xml.Serialization.XmlSerializationReader reader )
	//	{
	//		return ((XmlSerializationReader1) reader).Read10_FieldDef();
	//	}
	//}

	//public sealed class SetSchemaSerializer : XmlSerializer1
	//{

	//	public override System.Boolean CanDeserialize( System.Xml.XmlReader xmlReader )
	//	{
	//		return xmlReader.IsStartElement( @"SetSchema", @"" );
	//	}

	//	protected override void Serialize( object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer )
	//	{
	//		((XmlSerializationWriter1) writer).Write11_SetSchema( objectToSerialize );
	//	}

	//	protected override object Deserialize( System.Xml.Serialization.XmlSerializationReader reader )
	//	{
	//		return ((XmlSerializationReader1) reader).Read11_SetSchema();
	//	}
	//}

	//public sealed class TypeDefSerializer : XmlSerializer1
	//{

	//	public override System.Boolean CanDeserialize( System.Xml.XmlReader xmlReader )
	//	{
	//		return xmlReader.IsStartElement( @"TypeDef", @"" );
	//	}

	//	protected override void Serialize( object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer )
	//	{
	//		((XmlSerializationWriter1) writer).Write12_TypeDef( objectToSerialize );
	//	}

	//	protected override object Deserialize( System.Xml.Serialization.XmlSerializationReader reader )
	//	{
	//		return ((XmlSerializationReader1) reader).Read12_TypeDef();
	//	}
	//}

	//public sealed class TypeEncodingSerializer : XmlSerializer1
	//{

	//	public override System.Boolean CanDeserialize( System.Xml.XmlReader xmlReader )
	//	{
	//		return xmlReader.IsStartElement( @"TypeEncoding", @"" );
	//	}

	//	protected override void Serialize( object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer )
	//	{
	//		((XmlSerializationWriter1) writer).Write13_TypeEncoding( objectToSerialize );
	//	}

	//	protected override object Deserialize( System.Xml.Serialization.XmlSerializationReader reader )
	//	{
	//		return ((XmlSerializationReader1) reader).Read13_TypeEncoding();
	//	}
	//}

	//public sealed class CharacterTypeDefSerializer : XmlSerializer1
	//{

	//	public override System.Boolean CanDeserialize( System.Xml.XmlReader xmlReader )
	//	{
	//		return xmlReader.IsStartElement( @"CharacterTypeDef", @"" );
	//	}

	//	protected override void Serialize( object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer )
	//	{
	//		((XmlSerializationWriter1) writer).Write14_CharacterTypeDef( objectToSerialize );
	//	}

	//	protected override object Deserialize( System.Xml.Serialization.XmlSerializationReader reader )
	//	{
	//		return ((XmlSerializationReader1) reader).Read14_CharacterTypeDef();
	//	}
	//}

	//public sealed class Signed32BitIntegerTypeDefSerializer : XmlSerializer1
	//{

	//	public override System.Boolean CanDeserialize( System.Xml.XmlReader xmlReader )
	//	{
	//		return xmlReader.IsStartElement( @"Signed32BitIntegerTypeDef", @"" );
	//	}

	//	protected override void Serialize( object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer )
	//	{
	//		((XmlSerializationWriter1) writer).Write15_Signed32BitIntegerTypeDef( objectToSerialize );
	//	}

	//	protected override object Deserialize( System.Xml.Serialization.XmlSerializationReader reader )
	//	{
	//		return ((XmlSerializationReader1) reader).Read15_Signed32BitIntegerTypeDef();
	//	}
	//}

	//public sealed class BooleanTypeDefSerializer : XmlSerializer1
	//{

	//	public override System.Boolean CanDeserialize( System.Xml.XmlReader xmlReader )
	//	{
	//		return xmlReader.IsStartElement( @"BooleanTypeDef", @"" );
	//	}

	//	protected override void Serialize( object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer )
	//	{
	//		((XmlSerializationWriter1) writer).Write16_BooleanTypeDef( objectToSerialize );
	//	}

	//	protected override object Deserialize( System.Xml.Serialization.XmlSerializationReader reader )
	//	{
	//		return ((XmlSerializationReader1) reader).Read16_BooleanTypeDef();
	//	}
	//}

	//public sealed class ProxyTableSetSerializer : XmlSerializer1
	//{

	//	public override System.Boolean CanDeserialize( System.Xml.XmlReader xmlReader )
	//	{
	//		return xmlReader.IsStartElement( @"ProxyTableSet", @"" );
	//	}

	//	protected override void Serialize( object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer )
	//	{
	//		((XmlSerializationWriter1) writer).Write17_ProxyTableSet( objectToSerialize );
	//	}

	//	protected override object Deserialize( System.Xml.Serialization.XmlSerializationReader reader )
	//	{
	//		return ((XmlSerializationReader1) reader).Read17_ProxyTableSet();
	//	}
	//}

	//public class XmlSerializerContract : global::System.Xml.Serialization.XmlSerializerImplementation
	//{
	//	public override global::System.Xml.Serialization.XmlSerializationReader Reader { get { return new XmlSerializationReader1(); } }
	//	public override global::System.Xml.Serialization.XmlSerializationWriter Writer { get { return new XmlSerializationWriter1(); } }
	//	System.Collections.Hashtable readMethods = null;
	//	public override System.Collections.Hashtable ReadMethods
	//	{
	//		get
	//		{
	//			if (readMethods == null)
	//			{
	//				System.Collections.Hashtable _tmp = new System.Collections.Hashtable();
	//				_tmp[@"Unosys.Common.Types.FieldDef::"] = @"Read10_FieldDef";
	//				_tmp[@"Unosys.Common.Types.SetSchema::"] = @"Read11_SetSchema";
	//				_tmp[@"Unosys.Common.Types.TypeDef::"] = @"Read12_TypeDef";
	//				_tmp[@"Unosys.Common.Types.TypeEncoding::"] = @"Read13_TypeEncoding";
	//				_tmp[@"Unosys.Common.Types.CharacterTypeDef::"] = @"Read14_CharacterTypeDef";
	//				_tmp[@"Unosys.Common.Types.Signed32BitIntegerTypeDef::"] = @"Read15_Signed32BitIntegerTypeDef";
	//				_tmp[@"Unosys.Common.Types.BooleanTypeDef::"] = @"Read16_BooleanTypeDef";
	//				_tmp[@"Unosys.Common.Types.ProxyTableSet::"] = @"Read17_ProxyTableSet";
	//				if (readMethods == null) readMethods = _tmp;
	//			}
	//			return readMethods;
	//		}
	//	}
	//	System.Collections.Hashtable writeMethods = null;
	//	public override System.Collections.Hashtable WriteMethods
	//	{
	//		get
	//		{
	//			if (writeMethods == null)
	//			{
	//				System.Collections.Hashtable _tmp = new System.Collections.Hashtable();
	//				_tmp[@"Unosys.Common.Types.FieldDef::"] = @"Write10_FieldDef";
	//				_tmp[@"Unosys.Common.Types.SetSchema::"] = @"Write11_SetSchema";
	//				_tmp[@"Unosys.Common.Types.TypeDef::"] = @"Write12_TypeDef";
	//				_tmp[@"Unosys.Common.Types.TypeEncoding::"] = @"Write13_TypeEncoding";
	//				_tmp[@"Unosys.Common.Types.CharacterTypeDef::"] = @"Write14_CharacterTypeDef";
	//				_tmp[@"Unosys.Common.Types.Signed32BitIntegerTypeDef::"] = @"Write15_Signed32BitIntegerTypeDef";
	//				_tmp[@"Unosys.Common.Types.BooleanTypeDef::"] = @"Write16_BooleanTypeDef";
	//				_tmp[@"Unosys.Common.Types.ProxyTableSet::"] = @"Write17_ProxyTableSet";
	//				if (writeMethods == null) writeMethods = _tmp;
	//			}
	//			return writeMethods;
	//		}
	//	}
	//	System.Collections.Hashtable typedSerializers = null;
	//	public override System.Collections.Hashtable TypedSerializers
	//	{
	//		get
	//		{
	//			if (typedSerializers == null)
	//			{
	//				System.Collections.Hashtable _tmp = new System.Collections.Hashtable();
	//				_tmp.Add( @"Unosys.Common.Types.Signed32BitIntegerTypeDef::", new Signed32BitIntegerTypeDefSerializer() );
	//				_tmp.Add( @"Unosys.Common.Types.SetSchema::", new SetSchemaSerializer() );
	//				_tmp.Add( @"Unosys.Common.Types.TypeEncoding::", new TypeEncodingSerializer() );
	//				_tmp.Add( @"Unosys.Common.Types.ProxyTableSet::", new ProxyTableSetSerializer() );
	//				_tmp.Add( @"Unosys.Common.Types.BooleanTypeDef::", new BooleanTypeDefSerializer() );
	//				_tmp.Add( @"Unosys.Common.Types.FieldDef::", new FieldDefSerializer() );
	//				_tmp.Add( @"Unosys.Common.Types.TypeDef::", new TypeDefSerializer() );
	//				_tmp.Add( @"Unosys.Common.Types.CharacterTypeDef::", new CharacterTypeDefSerializer() );
	//				if (typedSerializers == null) typedSerializers = _tmp;
	//			}
	//			return typedSerializers;
	//		}
	//	}
	//	public override System.Boolean CanSerialize( System.Type type )
	//	{
	//		if (type == typeof( FieldDef )) return true;
	//		if (type == typeof( SetSchema )) return true;
	//		if (type == typeof( TypeDef )) return true;
	//		if (type == typeof( TypeEncoding )) return true;
	//		if (type == typeof( CharacterTypeDef )) return true;
	//		if (type == typeof( Signed32BitIntegerTypeDef )) return true;
	//		if (type == typeof( BooleanTypeDef )) return true;
	//		if (type == typeof( ProxyTableSet )) return true;
	//		return false;
	//	}
	//	public override System.Xml.Serialization.XmlSerializer GetSerializer( System.Type type )
	//	{
	//		if (type == typeof( FieldDef )) return new FieldDefSerializer();
	//		if (type == typeof( SetSchema )) return new SetSchemaSerializer();
	//		if (type == typeof( TypeDef )) return new TypeDefSerializer();
	//		if (type == typeof( TypeEncoding )) return new TypeEncodingSerializer();
	//		if (type == typeof( CharacterTypeDef )) return new CharacterTypeDefSerializer();
	//		if (type == typeof( Signed32BitIntegerTypeDef )) return new Signed32BitIntegerTypeDefSerializer();
	//		if (type == typeof( BooleanTypeDef )) return new BooleanTypeDefSerializer();
	//		if (type == typeof( ProxyTableSet )) return new ProxyTableSetSerializer();
	//		return null;
	//	}
	//}
}
