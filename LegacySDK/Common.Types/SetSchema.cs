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
	public class SetSchema
	{
		#region Field Members
		public byte Version;
		public Dictionary<int,FieldDef>? Fields;
		public Dictionary<int,OrderDef>? Orders;
		#endregion

		#region Constructors
		public SetSchema()	{ }

		public SetSchema( byte version, Dictionary<int,FieldDef>? fielddefinitions, Dictionary<int,OrderDef>? orderdefinitions)
		{
			if (!fielddefinitions!.ContainsKey(0))
			{
				#region Add initial ROWID ($RID$) field (Ordinal 0 (zero))
				var rowFieldDef = new Unosys.Common.Types.FieldDef
				{
					Name = "$RID$",
					TypeDefinition = Unosys.Common.Types.TypeDef.Factory(Unosys.Common.Types.Type.CHARACTER),
					Length = 32,
					Decimals = 0,
					IsNullable = false,
					IsMultiValued = false,
					FieldEncoding = Unosys.Common.Types.TypeEncoding.UTF8,
					DefaultValue = "",
					Description = "",
					Ordinal = 0,
				};
				#endregion
				fielddefinitions!.Add(0, rowFieldDef);
			}
			Version = version;
			Fields = fielddefinitions;

			if(orderdefinitions != null )
            {
                Orders = orderdefinitions;
            }
            else
            {
                Orders = new Dictionary<int, OrderDef>();
            }
			if( !Orders.ContainsKey(0))
			{
				Orders.Add(0, new OrderDef(new List<byte> { 0 })); // Add default order definition for "natural" ordinal order
            }
        }

		public SetSchema( byte version  )
		{
			Version = version;
		}
		#endregion

		#region Public Interface
		#region Properties
		public int FieldCount
		{
			get
			{
				return Fields!.Count;
			}
		}

		public int OrderCount
        {
            get
            {
                return Orders!.Count;
            }
        }



        //private int GetFixedRecordHeaderSize()
        //{
        //	return sizeof( sbyte ) + (3*Convert.ToInt32( RecordType.Medium ));
        //}

        //private int GetVariableRecordHeaderSize()
        //{
        //	return ((2*Convert.ToInt32(RecordType.Medium)) * Fields.Length);
        //}

        //private int GetTotalRecordHeaderSize()
        //{
        //	return GetFixedRecordHeaderSize() + GetVariableRecordHeaderSize();
        //}


        public FieldDef this[int fieldOrdinal]
		{
			get
			{
				return Fields[fieldOrdinal];
			}
			set
			{
				Fields[fieldOrdinal] = value;
			}
		}
		#endregion

		#region Methods
		public int RecordLength( RecordType recType = RecordType.Medium )
		{
			int maxDataSize = 0;
			for (int i = 0; i < FieldCount; i++)
			{
				maxDataSize += Fields![i].Length;
			}
			int baseSize = Convert.ToInt32( recType );  
			int totalRecSize = sizeof( byte ) + (3 * baseSize)	// Fixed header length size
				+ ((2 * baseSize) * FieldCount)				// Variable header length size
				+ maxDataSize;									// Max data size
			return totalRecSize;
		}

		public int BestFitMaxRecordLength()
		{
			RecordType recType = RecordType.Small;
			int bestFitMaxRecordLength = RecordLength( recType  );
			int recLength = RecordTypeLength( recType );
			if( bestFitMaxRecordLength <= recLength )
				return recLength;

			recType = RecordType.Medium;
			bestFitMaxRecordLength = RecordLength( recType  );
			recLength = RecordTypeLength( recType );
			if( bestFitMaxRecordLength <= recLength )
				return recLength;

			recType = RecordType.Large;
			bestFitMaxRecordLength = RecordLength( recType  );
			recLength = RecordTypeLength( recType );
			if( bestFitMaxRecordLength <= recLength )
				return recLength;

			throw new InvalidOperationException("Record too large.");

		}

		public RecordType BestFitRecordType( int maxRecordSize )
		{
			RecordType recType = RecordType.Small;
			if (maxRecordSize <= Convert.ToInt32(sbyte.MaxValue))
				return recType;
			recType = RecordType.Medium;
			if (maxRecordSize <= Convert.ToInt32(short.MaxValue ))
				return recType;
			return RecordType.Large;
		}

		public byte[] SerializeAllFieldDefinitionsToBytes()
		{
			int totalBytesWritten = 0;
			byte[][] fieldDefList = new byte[FieldCount][];
			for (int i = 0; i < FieldCount; i++)
			{
				FieldDef fieldDef = Fields![i];
                //int fieldDefSize = (7 * sizeof( int )) + (2 * sizeof( byte )) + Encoding.UTF8.GetByteCount( fieldDef.Name ) +
                //									(string.IsNullOrEmpty( fieldDef.DefaultValue ) ? 0 : fieldDef.DefaultEncoding.GetByteCount( fieldDef.DefaultValue )) +
                //									(string.IsNullOrEmpty( fieldDef.Description ) ? 0 : Encoding.UTF8.GetByteCount( fieldDef.Description ));
                //fieldDefList[i] = new byte[fieldDefSize];
                fieldDefList[i] = new byte[(4 * sizeof(int)) + Encoding.UTF8.GetByteCount(fieldDef.Name)];
				int bytesWritten = 0;
				Buffer.BlockCopy( BitConverter.GetBytes( (int) fieldDef.TypeDefinition.ID ), 0, fieldDefList[i], bytesWritten, sizeof( int ) );					// field type
				bytesWritten += sizeof( int );
				Buffer.BlockCopy( BitConverter.GetBytes( (int) fieldDef.Length ), 0, fieldDefList[i], bytesWritten, sizeof( int ) );							// field length
				bytesWritten += sizeof( int );
				Buffer.BlockCopy( BitConverter.GetBytes( (int) fieldDef.Decimals ), 0, fieldDefList[i], bytesWritten, sizeof( int ) );							// field decimals
				bytesWritten += sizeof( int );
				//byte byteVal = (fieldDef.IsNullable ? (byte) 1 : (byte) 0);
				//fieldDefList[i][bytesWritten] = byteVal;
    //            bytesWritten += sizeof( byte );
				//byteVal = (fieldDef.IsMultiValued ? (byte) 1 : (byte) 0);
    //            fieldDefList[i][bytesWritten] = byteVal;
    //            bytesWritten += sizeof( byte );
				//Buffer.BlockCopy( BitConverter.GetBytes( (int) fieldDef.FieldEncoding ), 0, fieldDefList[i], bytesWritten, sizeof( int ) );					// field encoding
				//bytesWritten += sizeof( int );
				int fieldNameLen = Encoding.UTF8.GetByteCount( fieldDef.Name );
				Buffer.BlockCopy( BitConverter.GetBytes( fieldNameLen ), 0, fieldDefList[i], bytesWritten, sizeof( int ) );										// field name length
				bytesWritten += sizeof( int );
				Buffer.BlockCopy( Encoding.UTF8.GetBytes( fieldDef.Name ), 0, fieldDefList[i], bytesWritten, fieldNameLen );									// field name 
				bytesWritten += fieldNameLen;
				//int fieldDescriptionLength = (string.IsNullOrEmpty( fieldDef.Description ) ? 0 : Encoding.UTF8.GetByteCount( fieldDef.Description ));
				//Buffer.BlockCopy( BitConverter.GetBytes( fieldDescriptionLength ), 0, fieldDefList[i], bytesWritten, sizeof( int ) );							// field description length
				//bytesWritten += sizeof( int );
				//if (fieldDescriptionLength > 0)
				//{
				//	Buffer.BlockCopy( Encoding.UTF8.GetBytes( fieldDef.Description ), 0, fieldDefList[i], bytesWritten, fieldDescriptionLength );				// field description
				//	bytesWritten += fieldDescriptionLength;
				//}
				//int defaultValueLength = (string.IsNullOrEmpty( fieldDef.DefaultValue ) ? 0 : fieldDef.DefaultEncoding.GetByteCount( fieldDef.DefaultValue ));
				//Buffer.BlockCopy( BitConverter.GetBytes( defaultValueLength ), 0, fieldDefList[i], bytesWritten, sizeof( int ) );								// DefaultValue length 
				//bytesWritten += sizeof( int );
				//if (defaultValueLength > 0)
				//{
				//	Buffer.BlockCopy( fieldDef.DefaultEncoding.GetBytes( fieldDef.DefaultValue ), 0, fieldDefList[i], bytesWritten, defaultValueLength );		// DefaultValue
				//	bytesWritten += defaultValueLength;
				//}
				totalBytesWritten += bytesWritten;
			}
			// Add the bytes for each fieldDef of schema to fieldBytes end-to-end
			byte[] fieldBytes = new byte[totalBytesWritten];
			int fieldOffset = 0;
			for (int i = 0; i < FieldCount; i++)
			{
				Buffer.BlockCopy( fieldDefList[i], 0, fieldBytes, fieldOffset, fieldDefList[i].Length );
				fieldOffset += fieldDefList[i].Length;
			}
			return fieldBytes;
		}



        public byte[] SerializeAllOrderDefinitionsToBytes()
        {
            int totalBytesWritten = 0;
            byte[][] orderDefList = new byte[OrderCount][];
			int orderIndex = 0;
            //for (int i = 0; i < OrderCount; i++)
			foreach( var orderDef in Orders!)
            {
                //OrderDef orderDef = Orders![i];
				if (orderDef.Value.FieldOrdinals[0] != 0)  // Skip the natural order
				{
					orderDefList[orderIndex] = new byte[sizeof(byte) + orderDef.Value.FieldOrdinals.Count];
					int bytesWritten = 0;
					orderDefList[orderIndex][bytesWritten++] = (byte)(orderDef.Value.FieldOrdinals.Count); // field ordinal count (i.e.; # of fields referenced in order expression)

                    foreach (var orderFieldOrdinal in orderDef.Value.FieldOrdinals)
					{
                        orderDefList[orderIndex][bytesWritten++] = (byte)orderFieldOrdinal;                // order field ordinal

                    }
					totalBytesWritten += bytesWritten;
				}
				orderIndex++;
            }
            // Add the bytes for each orderDef of schema to orderBytes end-to-end
            byte[] orderBytes = new byte[totalBytesWritten];
            int orderOffset = 0;
			for (int i = 0; i < orderDefList.Length; i++)
			{
                if (orderDefList[i] != null)
                {
                    Buffer.BlockCopy(orderDefList[i], 0, orderBytes, orderOffset, orderDefList[i].Length);
                    orderOffset += orderDefList[i].Length;
                }
            }
            return orderBytes;
        }


        static public Dictionary<int, OrderDef>? DeserializeAllOrderDefinitionsFromBytes(byte[] orderDefList, int offset, int orderCount)
        {
            //Debug.Print( "A orderList.Length={0}, offset={1}, orderCount={2}", orderList.Length, offset, orderCount );
            Dictionary<int, OrderDef>? orders = new Dictionary<int, OrderDef>();
			orders.Add(0, new OrderDef(new List<byte> { 0 })); // Add default order definition for "natural" ordinal order
            for (int i = 1; i <= orderCount; i++)
            {
                orders.Add(i, new OrderDef(new List<byte>()));
				byte orderFieldOrdinalCount = orderDefList[offset++]; // field ordinal count (i.e.; # of fields referenced in order expression)
                                                                      //var orderFieldOrdinalCount = BitConverter.ToInt32(orderDefList, offset); // field ordinal count (i.e.; # of fields referenced in order expression)
                                                                      //offset += sizeof(int);
                for (byte j = 0; j < orderFieldOrdinalCount; j++)
				{
                    byte fieldOrdinal = orderDefList[offset++]; // field ordinal referenced in order expression
                    //offset += sizeof(int);
                    orders[i]!.FieldOrdinals.Add(fieldOrdinal);
                }
            }
            return orders;
        }


        //public byte[] SerializeFieldDefinitionToBytes(int fieldOrdinal )
        //{
        //	int bytesWritten = 0;
        //	byte[] fieldDefBytes = null;

        //	FieldDef fieldDef = Fields[fieldOrdinal];
        //	fieldDefBytes = new byte[(7 * sizeof( int )) + (2 * sizeof( byte )) + Encoding.UTF8.GetByteCount( fieldDef.Name ) +
        //										(string.IsNullOrEmpty( fieldDef.DefaultValue ) ? 0 : fieldDef.DefaultEncoding.GetByteCount( fieldDef.DefaultValue )) +
        //										(string.IsNullOrEmpty( fieldDef.Description ) ? 0 : Encoding.UTF8.GetByteCount( fieldDef.Description ))];
        //	Buffer.BlockCopy( BitConverter.GetBytes( (int) fieldDef.TypeDefinition.ID ), 0, fieldDefBytes, bytesWritten, sizeof( int ) );					// field type
        //	bytesWritten += sizeof( int );
        //	Buffer.BlockCopy( BitConverter.GetBytes( (int) fieldDef.Length ), 0, fieldDefBytes, bytesWritten, sizeof( int ) );								// field length
        //	bytesWritten += sizeof( int );
        //	Buffer.BlockCopy( BitConverter.GetBytes( (int) fieldDef.Decimals ), 0, fieldDefBytes, bytesWritten, sizeof( int ) );							// field decimals
        //	bytesWritten += sizeof( int );
        //	byte byteVal = (fieldDef.IsNullable ? (byte) 1 : (byte) 0);
        //          //Buffer.BlockCopy( BitConverter.GetBytes( byteVal ), 0, fieldDefBytes, bytesWritten, sizeof( byte ) );											// IsNullable?
        //          fieldDefBytes[bytesWritten] = byteVal;
        //          bytesWritten += sizeof( byte );
        //	byteVal = (fieldDef.IsMultiValued ? (byte) 1 : (byte) 0);
        //          //Buffer.BlockCopy( BitConverter.GetBytes( byteVal ), 0, fieldDefBytes, bytesWritten, sizeof( byte ) );											// IsMultiValued?
        //          fieldDefBytes[bytesWritten] = byteVal;
        //          bytesWritten += sizeof( byte );
        //	Buffer.BlockCopy( BitConverter.GetBytes( (int) fieldDef.FieldEncoding ), 0, fieldDefBytes, bytesWritten, sizeof( int ) );							// field encoding
        //	bytesWritten += sizeof( int );
        //	int fieldNameLen = Encoding.UTF8.GetByteCount( fieldDef.Name );
        //	Buffer.BlockCopy( BitConverter.GetBytes( fieldNameLen ), 0, fieldDefBytes, bytesWritten, sizeof( int ) );										// field name length
        //	bytesWritten += sizeof( int );
        //	Buffer.BlockCopy( Encoding.UTF8.GetBytes( fieldDef.Name ), 0, fieldDefBytes, bytesWritten, fieldNameLen );										// field name 
        //	bytesWritten += fieldNameLen;
        //	int fieldDescriptionLength = (string.IsNullOrEmpty( fieldDef.Description ) ? 0 : Encoding.UTF8.GetByteCount( fieldDef.Description ));
        //	Buffer.BlockCopy( BitConverter.GetBytes( fieldDescriptionLength ), 0, fieldDefBytes, bytesWritten, sizeof( int ) );							// field description length
        //	bytesWritten += sizeof( int );
        //	if (fieldDescriptionLength > 0)
        //	{
        //		Buffer.BlockCopy( Encoding.UTF8.GetBytes( fieldDef.Description ), 0, fieldDefBytes, bytesWritten, fieldDescriptionLength );				// field description
        //		bytesWritten += fieldDescriptionLength;
        //	}
        //	int defaultValueLength = (string.IsNullOrEmpty( fieldDef.DefaultValue ) ? 0 : fieldDef.DefaultEncoding.GetByteCount( fieldDef.DefaultValue ));
        //	Buffer.BlockCopy( BitConverter.GetBytes( defaultValueLength ), 0, fieldDefBytes, bytesWritten, sizeof( int ) );								// DefaultValue length 
        //	bytesWritten += sizeof( int );
        //	if (defaultValueLength > 0)
        //	{
        //		Buffer.BlockCopy( fieldDef.DefaultEncoding.GetBytes( fieldDef.DefaultValue ), 0, fieldDefBytes, bytesWritten, defaultValueLength );		// DefaultValue
        //		bytesWritten += defaultValueLength;
        //	}
        //	return fieldDefBytes;
        //}

        static public Dictionary<int,FieldDef>? DeserializeAllFieldDefinitionsFromBytes( byte[] fieldDefList, int offset, int fieldCount )
		{
			//Debug.Print( "A fieldList.Length={0}, offset={1}, fieldCount={2}", fieldList.Length, offset, fieldCount );
			//FieldDef[] fields = new FieldDef[fieldCount]; ;
			Dictionary<int, FieldDef?> fields = new Dictionary<int, FieldDef?>(fieldCount);
            for (int i = 0; i < fieldCount; i++)
			{
				fields.Add(i, new FieldDef());
				fields[i]!.Ordinal = i;
				fields[i]!.TypeDefinition = TypeDef.Factory( (Type) BitConverter.ToInt32( fieldDefList, offset ) );								// field type
				offset += sizeof( int );
				fields[i]!.Length = BitConverter.ToInt32( fieldDefList, offset );																// field length
				offset += sizeof( int );
				fields[i]!.Decimals = BitConverter.ToInt32( fieldDefList, offset );																// field decimals
				offset += sizeof( int );
				//fields[i].IsNullable = (fieldDefList[offset] == 1 ? true : false);																// IsNullable
				//offset++;
				//fields[i].IsMultiValued = (fieldDefList[offset] == 1 ? true : false);															// IsMultiValued
				//offset++;
				//fields[i].FieldEncoding = (TypeEncoding) BitConverter.ToInt32( fieldDefList, offset );											// field encoding
				//offset += sizeof( int );
				int fieldNameLength = BitConverter.ToInt32( fieldDefList, offset );																// field name length
				offset += sizeof( int );
				byte[] fileNameBytes = new byte[fieldNameLength];
				Buffer.BlockCopy( fieldDefList, offset, fileNameBytes, 0, fieldNameLength );													// field name
				offset += fieldNameLength;
				fields[i]!.Name = Encoding.UTF8.GetString( fileNameBytes );
				//int descriptionLength = BitConverter.ToInt32( fieldDefList, offset );															// field description length
				//offset += sizeof( int );
				//if (descriptionLength > 0)
				//{
				//	byte[] description = new byte[descriptionLength];
				//	Buffer.BlockCopy( fieldDefList, offset, description, 0, descriptionLength );												// description 
				//	fields[i].Description = Encoding.UTF8.GetString( description );
				//	offset += descriptionLength;
				//}
				//else
				//{
				//	fields[i].Description = string.Empty;
				//}
				//int defaultValueLength = BitConverter.ToInt32( fieldDefList, offset );															// default value length				
				//offset += sizeof( int );
				//if (defaultValueLength > 0)
				//{
				//	byte[] defaultValue = new byte[defaultValueLength];
				//	Buffer.BlockCopy( fieldDefList, offset, defaultValue, 0, defaultValueLength );												// default value
				//	fields[i].DefaultValue = fields[i].DefaultEncoding.GetString( defaultValue );
				//	offset += defaultValueLength;
				//}
				//else
				//{
				//	fields[i].DefaultValue = string.Empty;
				//}
			}
			return fields;
		}

		static public FieldDef DeserializeFieldDefinitionFromBytes(byte[] fieldDefBytes, ref int offset, int fieldOrdinal)
		{
			FieldDef fieldDef = new FieldDef();
			fieldDef.Ordinal = fieldOrdinal;
			fieldDef.TypeDefinition = TypeDef.Factory((Type)BitConverter.ToInt32(fieldDefBytes, offset));								// field type
			offset += sizeof(int);
			fieldDef.Length = BitConverter.ToInt32(fieldDefBytes, offset);                                                              // field length
			offset += sizeof(int);
			fieldDef.Decimals = BitConverter.ToInt32(fieldDefBytes, offset);                                                                // field decimals
			offset += sizeof(int);
			//fieldDef.IsNullable = (fieldDefBytes[offset] == 1 ? true : false);                                                              // IsNullable
			//offset++;
			//fieldDef.IsMultiValued = (fieldDefBytes[offset] == 1 ? true : false);                                                           // IsMultiValued
			//offset++;
			//fieldDef.FieldEncoding = (TypeEncoding)BitConverter.ToInt32(fieldDefBytes, offset);                                         // field encoding
			//offset += sizeof(int);
			int fieldNameLength = BitConverter.ToInt32(fieldDefBytes, offset);                                                          // field name length
			offset += sizeof(int);
			byte[] fileNameBytes = new byte[fieldNameLength];
			Buffer.BlockCopy(fieldDefBytes, offset, fileNameBytes, 0, fieldNameLength);                                                 // field name
			offset += fieldNameLength;
			fieldDef.Name = Encoding.UTF8.GetString(fileNameBytes);
			//int descriptionLength = BitConverter.ToInt32(fieldDefBytes, offset);                                                            // field description length
			//offset += sizeof(int);
			//if (descriptionLength > 0)
			//{
			//	byte[] description = new byte[descriptionLength];
			//	Buffer.BlockCopy(fieldDefBytes, offset, description, 0, descriptionLength);                                             // description 
			//	fieldDef.Description = Encoding.UTF8.GetString(description);
			//	offset += descriptionLength;
			//}
			//else
			//{
			//	fieldDef.Description = string.Empty;
			//}
			//int defaultValueLength = BitConverter.ToInt32(fieldDefBytes, offset);                                                           // default value length				
			//offset += sizeof(int);
			//if (defaultValueLength > 0)
			//{
			//	byte[] defaultValue = new byte[defaultValueLength];
			//	Buffer.BlockCopy(fieldDefBytes, offset, defaultValue, 0, defaultValueLength);                                               // default value
			//	fieldDef.DefaultValue = fieldDef.DefaultEncoding.GetString(defaultValue);
			//	offset += defaultValueLength;
			//}
			//else
			//{
			//	fieldDef.DefaultValue = string.Empty;
			//}
			return fieldDef;
		}



		#endregion
		#endregion

		#region Private Helpers
		private int RecordTypeLength( RecordType recordType = RecordType.Medium )
		{
			int maxRecordLength = 0;
			switch (recordType)
			{
				case RecordType.Small:
					maxRecordLength = SByte.MaxValue;
					break;

				case RecordType.Medium:
					maxRecordLength = Int16.MaxValue;
					break;

				case RecordType.Large:
					maxRecordLength = Int32.MaxValue;
					break;

				default:
					throw new InvalidOperationException( "Invalid RecordType." );
			}
			return maxRecordLength;
		}

		#endregion

	}
}
