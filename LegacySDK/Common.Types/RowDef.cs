using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosys.Common.Types;

#if UNOSYS
namespace Unosys.Kernel
#else
namespace PeerRepublic.Kernel
#endif
{
    //[Serializable]
    internal class RowDef
    {
        #region Field Members
        //		private const int MEDIUM_ROW_FIXED_HEADER_SIZE = 9;
        private const int FIELD_UNINITIALIZED = 1;
        private const int FIELD_NULL = 0;
        internal byte[] rowBuffer;
        private SetSchema setschema;
        private int rowBufferSize = -1;
        private bool rowIsDirty = false;
        private int maxRecordDataSize = 0;
        #endregion


        #region Constructors
        //internal RowDef()	{ }

        internal RowDef(int bufferSize, SetSchema setSchema) //: this()
        {
            setschema = setSchema;
            rowBuffer = new byte[bufferSize];
            rowBufferSize = bufferSize;
            
            InitializeEmptyRecord();
            //RecordType recordType = setSchema.BestFitRecordType( bufferSize );
            //InitializeEmptyRow( recordType );
        }
        #endregion

        #region Public Interface
        #region Properties
        internal byte[] Record
        {
            set
            {
                rowBuffer = value;
                rowBufferSize = value.Length;
                rowIsDirty = false;
            }
        }
        // This indexer reads the contents of the field with ordinal index from the buffer
        internal object this[int fieldOrdinal]
        {
            get
            {
                object Value = null!;
                //byte[] fieldBytes;
                //int fieldOffset = -1;
                
                // Step #1: Determine offset in the buffer the field value should be read from
                int fieldOffset = GetFieldOffsetFromRecord(fieldOrdinal);
                if (IsFieldUninitializedInRow(fieldOffset))
                {
                    // Field value has never been set so return its default value
                    return setschema[fieldOrdinal].DefaultValue;
                }
                else
                {
                    var fieldDef = setschema[fieldOrdinal];
                    if (IsMultiValuedField(fieldOffset))
                    {
                        // Indicates an array offset
                        throw new NotImplementedException("Multi-valued fields not supported.");
                    }
                    else
                    {
                        // We have a legitamite offset...
                        //if (setschema[fieldOrdinal].TypeDefinition.IsFixedSize)
                        if (fieldDef.TypeDefinition.IsFixedSize)
                        {
                            // Can just inplace read the current value out of the record
                            //fieldBytes = GetFieldBytesFromRecord(fieldOffset, setschema[fieldOrdinal].Length);
                            //fieldBytes = GetFieldBytesFromRecord(fieldOffset, fieldDef.Length);
                            //Value = setschema[fieldOrdinal].GetObjectFromBytes(fieldBytes);
                            Value = fieldDef.GetObjectFromBytes(GetFieldBytesFromRecord(fieldOffset, fieldDef.Length));
                        }
                        else
                        {
                            // May need to move data around...
                            throw new NotImplementedException("Not implemented yet (4).");
                        }
                    }
                }
                return Value;
            }

            set
            {
                byte[] fieldBytes;
                int fieldOffset = -1;
                var fieldDef = setschema[fieldOrdinal];
                //if (value == null && setschema[fieldOrdinal].IsNullable)
                if (value == null && fieldDef.IsNullable)
                {
                    // we have a null value... 
                    throw new NotImplementedException("Not implemented yet (1).");
                }
                else
                {
                    //if (value == null && !setschema[fieldOrdinal].IsNullable)
                    if (value == null && !fieldDef.IsNullable)
                    {
                        // we have an empty value...
                        throw new NotImplementedException("Not implemented yet (2).");
                    }
                    else
                    {
                        // we have an actual value...

                        // Step #1: Determine the bytes of the value to be written 
                        //fieldBytes = setschema[fieldOrdinal].GetBytesFromObject(value!);
                        fieldBytes = fieldDef.GetBytesFromObject(value!);

                        // Step #2: Determine offset in the buffer the field value should be written to
                        fieldOffset = GetFieldOffsetFromRecord(fieldOrdinal);
                        if (IsFieldUninitializedInRow(fieldOffset))
                        {
                            // Field is currently empty...must make room
                            //
                            if (IsFirstFieldWrite())
                            {
                                // Empty record... This field write is the first one
                                PutFirstFieldIntoRecord(fieldOrdinal, fieldBytes);
                                //PutFieldOffsetIntoRecord( fieldOrdinal, GetFirstFieldOffsetFromRecord() );
                                //PutRecordSizeIntoRecord( fieldBytes.Length );
                            }
                            else
                            {
                                // Existing record, but first time we are righting this field to the record so tack the fieldbytes on the end
                                PutFieldOnEndOfRecord(fieldBytes, fieldOrdinal);

                            }
                        }
                        else
                        {
                            if (IsMultiValuedField(fieldOffset))
                            {
                                // Indicates an array offset
                                throw new NotImplementedException("Multi-valued fields not supported.");
                            }
                            else
                            {
                                // We have a legitamite offset...
                                //if (setschema[fieldOrdinal].TypeDefinition.IsFixedSize)
                                if (fieldDef.TypeDefinition.IsFixedSize)
                                {
                                    // Can just inplace overwrite the current value with the new value

                                    // Write the fieldBytes into the record
                                    PutFieldBytesIntoRecord(fieldBytes, fieldOffset);
                                }
                                else
                                {
                                    // May need to move data around...
                                    throw new NotImplementedException("Not implemented yet (4).");
                                }
                            }
                        }
                    }
                }
                // Step #3: Make room for the data if necessary
                // Step #5: Update row header offsets
            }
        }

        internal bool IsDirty
        {
            get { return rowIsDirty; }
            set { rowIsDirty = value; }
        }

        #endregion

        #region Methods
        internal void InitializeEmptyRecord()
        {
            ZeroBuffer(rowBuffer);  // Clear the buffer
            RecordType recordType = setschema.BestFitRecordType(rowBufferSize);
            InitializeEmptyRow(recordType);
        }


        internal byte[] GetFieldBytes(int fieldOrdinal)
        {
            byte[] fieldBytes;
            int fieldOffset = -1;
            var fieldDef = setschema[fieldOrdinal];
            // Step #1: Determine offset in the buffer the field value should be read from
            fieldOffset = GetFieldOffsetFromRecord(fieldOrdinal);
            if (IsFieldUninitializedInRow(fieldOffset))
            {
                // Field value has never been set so return its empty value as an array of bytes
                //return setschema[fieldOrdinal].DefaultAsBytes;
                return fieldDef.DefaultAsBytes;
            }
            else
            {
                if (IsMultiValuedField(fieldOffset))
                {
                    // Indicates an array offset
                    throw new NotImplementedException("Multi-valued fields not supported.");
                }
                else
                {
                    // We have a legitamite offset...
                    //if (setschema[fieldOrdinal].TypeDefinition.IsFixedSize)
                    if (fieldDef.TypeDefinition.IsFixedSize)
                    {
                        // Can just inplace read the current value out of the record
                        //fieldBytes = GetFieldBytesFromRecord(fieldOffset, setschema[fieldOrdinal].Length);
                        fieldBytes = GetFieldBytesFromRecord(fieldOffset, fieldDef.Length);
                    }
                    else
                    {
                        // May need to move data around...
                        throw new NotImplementedException("Not implemented yet (4).");
                    }
                }
            }
            return fieldBytes;
        }

        internal byte[] PutFieldBytes(int fieldOrdinal, byte[] fieldBytes)
        {
            // NOTE:  We don't set the rowIsDirty flag here since each of the individual Put....IntoRecord() calls below do so internally
            int fieldOffset = -1;
            var fieldDef = setschema[fieldOrdinal];
            //if (fieldBytes == null && setschema[fieldOrdinal].IsNullable)
            if (fieldBytes == null && fieldDef.IsNullable)
            {
                // we have a null value... 
                throw new NotImplementedException("Not implemented yet (1).");
            }
            else
            {
                //if (fieldBytes == null && !setschema[fieldOrdinal].IsNullable)
                if (fieldBytes == null && !fieldDef.IsNullable)
                {
                    // we have an empty value...
                    throw new NotImplementedException("Not implemented yet (2).");
                }
                else
                {
                    // we have the bytes of an actual value...

                    // Step #1: Determine offset in the buffer the field value should be written to
                    fieldOffset = GetFieldOffsetFromRecord(fieldOrdinal);
                    if (IsFieldUninitializedInRow(fieldOffset))
                    {
                        // Field is currently empty...must make room
                        //
                        if (IsFirstFieldWrite())
                        {
                            // Empty record... This field write is the first one
                            PutFirstFieldIntoRecord(fieldOrdinal, fieldBytes!);
                            //PutFieldOffsetIntoRecord( fieldOrdinal, GetFirstFieldOffsetFromRecord() );
                            //PutRecordSizeIntoRecord( fieldBytes.Length );
                        }
                        else
                        {
                            // Existing record, but first time we are righting this field to the record so tack the fieldbytes on the end
                            PutFieldOnEndOfRecord(fieldBytes!, fieldOrdinal);

                        }
                    }
                    else
                    {
                        if (IsMultiValuedField(fieldOffset))
                        {
                            // Indicates an array offset
                            throw new NotImplementedException("Multi-valued fields not supported.");
                        }
                        else
                        {
                            // We have a legitamite offset...
                            //if (setschema[fieldOrdinal].TypeDefinition.IsFixedSize)
                            if (fieldDef.TypeDefinition.IsFixedSize)
                            {
                                // Can just inplace overwrite the current value with the new value

                                // Write the fieldBytes into the record
                                PutFieldBytesIntoRecord(fieldBytes!, fieldOffset);
                            }
                            else
                            {
                                // May need to move data around...
                                throw new NotImplementedException("Not implemented yet (4).");
                            }
                        }
                    }
                }
            }
            return GetFieldBytes(fieldOrdinal);  // %TODO% - Handle care where write is allowed but read is not - i.e.; need to ignore read security for this call
        }

        internal byte[] GetRecordBytes(ref int byteCount)
        {
            byteCount = GetTotalUsedRecordSize();
            return rowBuffer;
        }
        #endregion
        #endregion

        #region Private Helpers
        #region Record Writing helpers
        private void InitializeEmptyRow(RecordType recordType)
        {
            rowIsDirty = false;  // an new empty row not dirty initially
            // Populate header from schema
            int offset = PutRecordType(rowBuffer, recordType);                                                          // placeholder for record type (i.e.; SMALL, MEDIUM or LARGE)
            //Buffer.BlockCopy( BitConverter.GetBytes( 0 ), 0, rowBuffer, offset, GetRecordVersionSize() );				// placeholder for overall record version
            //offset += GetRecordVersionSize();
            Buffer.BlockCopy(BitConverter.GetBytes(0), 0, rowBuffer, offset, GetRecordLengthSize());                    // placeholder for overall record length
            offset += GetRecordLengthSize();
            Buffer.BlockCopy(BitConverter.GetBytes(setschema.FieldCount), 0, rowBuffer, offset, GetFieldCountSize());   // field count
            offset += GetFieldCountSize();
            for (int i = 0; i < setschema.FieldCount; i++)
            {
                //Buffer.BlockCopy( BitConverter.GetBytes( 0 ), 0, rowBuffer, offset, GetFieldVersionSize() );			// placeholder for field version
                //offset += GetFieldVersionSize();
                Buffer.BlockCopy(BitConverter.GetBytes(1), 0, rowBuffer, offset, GetFieldOffsetSize());                 // placeholder for field offset  1-indicates "empty" (i.e.; no value) (NOTE: different than null!) 
                offset += GetFieldOffsetSize();
            }
        }

        private int PutRecordType(byte[] rowBuffer, RecordType recordType)
        {
            rowBuffer[0] = Convert.ToByte((int)recordType);  //  Asssume RecordType takes 1 (the first) byte of the row
            return GetRecordTypeSize();
        }

        private void PutFieldOffsetIntoRecord(int fieldOrdinal, int fieldOffset)
        {
            rowIsDirty = true;
            Buffer.BlockCopy(BitConverter.GetBytes(fieldOffset), 0, rowBuffer,
                    GetFixedRecordHeaderSize() + ((GetFieldOffsetSize() /*+GetFieldVersionSize()*/) * fieldOrdinal) /*+GetFieldVersionSize()*/, GetFieldOffsetSize());
        }

        private void PutFirstFieldIntoRecord(int fieldOrdinal, byte[] fieldBytes)
        {
            //			PutFirstFieldIntoRecord( fieldBytes );
            int offset = GetFirstFieldOffsetFromRecord();
            rowIsDirty = true;
            Buffer.BlockCopy(fieldBytes, 0, rowBuffer, offset, fieldBytes.Length);

            PutFieldOffsetIntoRecord(fieldOrdinal, offset);
            PutRecordSizeIntoRecord(fieldBytes.Length);
        }

        //private void PutFirstFieldIntoRecord( byte[] fieldBytes )
        //{
        //	rowIsDirty = true;
        //	Buffer.BlockCopy( fieldBytes, 0, rowBuffer, GetFixedRecordHeaderSize() + ((GetFieldOffsetSize()+GetFieldVersionSize()) * setschema.FieldCount), fieldBytes.Length );
        //}

        private void PutFieldOnEndOfRecord(byte[] fieldBytes, int fieldOrdinal)
        {
            int currentRecordSize = GetUsedRecordLength();
            int fieldOffset = GetFirstFieldOffsetFromRecord() + currentRecordSize;
            // Write field onto end of record
            PutFieldBytesIntoRecord(fieldBytes, fieldOffset);
            // Update field offset in record header
            PutFieldOffsetIntoRecord(fieldOrdinal, fieldOffset);
            // Update total record size
            PutRecordSizeIntoRecord(currentRecordSize + fieldBytes.Length);
        }

        private void PutRecordSizeIntoRecord(int recordSize)
        {
            rowIsDirty = true;
            //Buffer.BlockCopy( BitConverter.GetBytes( recordSize ), 0, rowBuffer, GetRecordTypeSize() + GetRecordVersionSize(), GetRecordLengthSize() );
            Buffer.BlockCopy(BitConverter.GetBytes(recordSize), 0, rowBuffer, GetRecordTypeSize(), GetRecordLengthSize());
        }

        private void PutFieldBytesIntoRecord(byte[] fieldBytes, int fieldOffset)
        {
            if (fieldOffset + fieldBytes.Length > rowBufferSize)
            {
                throw new InvalidOperationException("Field to big for record.");
            }
            // Write the fieldBytes into the rowBuffer
            rowIsDirty = true;
            Buffer.BlockCopy(fieldBytes, 0, rowBuffer, fieldOffset, fieldBytes.Length);
        }
        #endregion

        #region Record Reading helpers
        internal int GetFieldOffsetFromRecord(int fieldOrdinal)
        {
            int fieldOffset = 0;
            RecordType recordType = (RecordType)Convert.ToInt32(GetRecordType());
            switch (recordType)
            {
                case RecordType.Small:
                    fieldOffset = rowBuffer[GetFixedRecordHeaderSize() + ((GetFieldOffsetSize()/*+GetFieldVersionSize()*/) * fieldOrdinal)/*+GetFieldVersionSize()*/];
                    break;

                case RecordType.Medium:
                    fieldOffset = BitConverter.ToInt16(rowBuffer, GetFixedRecordHeaderSize() + ((GetFieldOffsetSize()/*+GetFieldVersionSize()*/) * fieldOrdinal)/*+GetFieldVersionSize()*/ );
                    break;

                case RecordType.Large:
                    fieldOffset = BitConverter.ToInt32(rowBuffer, GetFixedRecordHeaderSize() + ((GetFieldOffsetSize()/*+GetFieldVersionSize()*/) * fieldOrdinal) /*+ GetFieldVersionSize() */);
                    break;

                default:
                    throw new InvalidOperationException("Record corruption detected.");
            }
            return fieldOffset;
        }

        //internal int GetFieldVersionFromRecord( int fieldOrdinal )
        //{
        //	int fieldVersion = 0;
        //	RecordType recordType = (RecordType) Convert.ToInt32( GetRecordType() );
        //	switch (recordType)
        //	{
        //		case RecordType.Small:
        //			fieldVersion = rowBuffer[GetFixedRecordHeaderSize() + ((GetFieldOffsetSize() + GetFieldVersionSize()) * fieldOrdinal) ];
        //			break;

        //		case RecordType.Medium:
        //			fieldVersion = BitConverter.ToInt16( rowBuffer, GetFixedRecordHeaderSize() + ((GetFieldOffsetSize() + GetFieldVersionSize()) * fieldOrdinal) );
        //			break;

        //		case RecordType.Large:
        //			fieldVersion = BitConverter.ToInt32( rowBuffer, GetFixedRecordHeaderSize() + ((GetFieldOffsetSize() + GetFieldVersionSize()) * fieldOrdinal) );
        //			break;

        //		default:
        //			throw new InvalidOperationException( "Record corruption detected." );
        //	}
        //	return fieldVersion;
        //}

        internal byte[] GetFieldBytesFromRecord(int fieldOffset, int count)
        {
            if (fieldOffset + count > rowBufferSize)
            {
                throw new InvalidOperationException("Field to big for record.");
            }
            byte[] fieldBytes = new byte[count];
            // Read the fieldBytes into the rowBuffer
            Buffer.BlockCopy(rowBuffer, fieldOffset, fieldBytes, 0, count);
            //if (fieldBytes.Length != count)
            //throw new InvalidOperationException( "Wrong field length." );
            return fieldBytes;
        }
        #endregion

        #region Other helpers
        internal bool DoesFieldHaveDefaultValue(int fieldOrdinal)
        {
            return setschema[fieldOrdinal].HasDefaultValue;
        }

        internal bool IsFieldUninitializedInRow(int fieldOffset)
        {
            return (fieldOffset == FIELD_UNINITIALIZED);
        }

        private int GetFirstFieldOffsetFromRecord()
        {
            return GetTotalRecordHeaderSize();
        }

        //internal int GetTotalRecordSize() 
        //{
        //	return GetFirstFieldOffsetFromRecord() + GetRecordDataSize();
        //}

        private bool IsMultiValuedField(int fieldOffset)
        {
            return fieldOffset < 0;  // A negative fieldOffset value indicates a multi-valued field
        }

        private bool IsFirstFieldWrite()
        {
            return (GetUsedRecordLength() == 0);
        }

        internal byte GetRecordType()
        {
            return rowBuffer[0];  // first byte
        }

        //internal int GetRecordVersion()
        //{
        //	int recordVersion = 0;
        //	RecordType recordType = (RecordType) Convert.ToInt32( GetRecordType() );
        //	switch (recordType)
        //	{
        //		case RecordType.Small:
        //			recordVersion = rowBuffer[GetRecordTypeSize()];							// Record version is stored after RecordType 
        //			break;

        //		case RecordType.Medium:
        //			recordVersion = BitConverter.ToInt16( rowBuffer, GetRecordTypeSize() );	// Record size is stored after RecordType 
        //			break;

        //		case RecordType.Large:
        //			recordVersion = BitConverter.ToInt32( rowBuffer, GetRecordTypeSize() );	// Record size is stored after RecordType 
        //			break;

        //		default:
        //			throw new InvalidOperationException( "Record corruption detected." );
        //	}
        //	return recordVersion;

        //}

        internal int GetRecordTypeSize()
        {
            return sizeof(sbyte);
        }

        internal int GetUsedRecordLength()
        {
            int recordSize = 0;
            RecordType recordType = (RecordType)Convert.ToInt32(GetRecordType());
            switch (recordType)
            {
                case RecordType.Small:
                    recordSize = rowBuffer[GetRecordTypeSize() /*+ GetRecordVersionSize()*/];                           // Record size is stored after RecordType 
                    break;

                case RecordType.Medium:
                    recordSize = BitConverter.ToInt16(rowBuffer, GetRecordTypeSize() /*+ GetRecordVersionSize()*/ );    // Record size is stored after RecordType 
                    break;

                case RecordType.Large:
                    recordSize = BitConverter.ToInt32(rowBuffer, GetRecordTypeSize() /*+ GetRecordVersionSize()*/ );    // Record size is stored after RecordType 
                    break;

                default:
                    throw new InvalidOperationException("Record corruption detected.");
            }
            return recordSize;
        }

        private int GetFixedRecordHeaderSize()
        {
            return GetRecordTypeSize() /*+ GetRecordVersionSize()*/ + GetRecordLengthSize() + GetFieldCountSize();
        }

        private int GetVariableRecordHeaderSize()
        {
            return ((GetFieldOffsetSize() /*+ GetFieldVersionSize()*/) * setschema.FieldCount);
        }

        internal int GetTotalRecordHeaderSize()
        {
            return GetFixedRecordHeaderSize() + GetVariableRecordHeaderSize();
        }

        internal int GetTotalUsedRecordSize()
        {
            return GetTotalRecordHeaderSize() + GetUsedRecordLength();
        }

        internal int GetMaxRecordSize()
        {
            return GetTotalRecordHeaderSize() + GetFixedRecordLength();
        }

        internal int GetFixedRecordLength()
        {
            int fixedRecordLength = 0;
            foreach( var kvp in setschema.Fields!)
            {
                if (kvp.Value.TypeDefinition.IsFixedSize)
                {
                    fixedRecordLength += kvp.Value.Length;
                }
                else
                {
                    // Variable length field, so we cannot determine the fixed record length
                    throw new InvalidOperationException("Cannot determine fixed record length due to variable length fields.");
                }
            }
            return fixedRecordLength;
        }


        private int GetFieldOffsetSize()
        {
            return GetRecordLengthSize();  // The offset size is the same as the record length size
        }

        //private int GetFieldVersionSize()
        //{
        //	return 0; // GetRecordLengthSize(); // The field version size is the same as the record length size
        //}

        //private int GetRecordVersionSize()
        //{
        //	return 0; // GetRecordLengthSize(); // The record version size is the same as the record length size
        //}

        private int GetFieldCountSize()
        {
            return GetRecordLengthSize(); // The field count size is the same as the record length size
        }

        private int GetRecordLengthSize()
        {
            return Convert.ToInt32(GetRecordType());
        }

        private int GetMaxFieldCount()
        {
            int maxFieldCount = 0;
            RecordType recordType = (RecordType)Convert.ToInt32(GetRecordType());
            switch (recordType)
            {
                case RecordType.Small:
                    maxFieldCount = 32;  // %TODO for now...
                    break;

                case RecordType.Medium:
                    maxFieldCount = 4096; // %TODO for now
                    break;

                case RecordType.Large:
                    maxFieldCount = 65536; // %TODO% for now
                    break;

                default:
                    throw new InvalidOperationException("Record corruption detected.");
            }
            return maxFieldCount;
        }


        private void ZeroBuffer(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null.");
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
            }
        }
        #endregion
        #endregion


    }


}
