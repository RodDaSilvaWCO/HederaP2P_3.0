using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class TypedSetMember
    {

        #region Field Members
        private const int MAX_ROW_CACHE_SIZE = 100;
        private SetSchema? setschema;
        private RowDef[]? rowCache;
        private int rowCacheIndex = -1;
        private ulong ordinal = 0;  // note: equivalent to IsEOT!! - positions are 1's based
        #endregion

        #region Constructors
        public TypedSetMember() { }

        public TypedSetMember(int rowCacheSize, SetSchema setSchema , int maxBufferSize) : this()
        {
            if (rowCacheSize <= 0 || rowCacheSize > MAX_ROW_CACHE_SIZE)
            {
                throw new InvalidOperationException(string.Format("rowCacheSize must be > 0 and < {0}", MAX_ROW_CACHE_SIZE));
            }

            // We begin by initializing the first empty row...
            rowCache = new RowDef[rowCacheSize];
            rowCacheIndex = 0;
            if (setSchema != null)
            {
                setschema = setSchema;
                rowCache[rowCacheIndex] = new RowDef(maxBufferSize, setSchema);

                //DumpRecord( "TypedSet.Ctor() - newly initialized empty record" );
            }
            else
            {
                throw new InvalidOperationException("Schema cannot be null.");
            }
        }
        #endregion

        #region Public Interface
        #region Properties
        internal byte[] SetMemberBuffer
        {
            get { return rowCache![rowCacheIndex].rowBuffer; }
            set {  rowCache![rowCacheIndex].rowBuffer = value; }
        }


        public int SetMemberSize 
        {
            get { return rowCache![rowCacheIndex].GetMaxRecordSize(); }
        }
        public ulong Ordinal
        {
            get { return ordinal; }
            set { ordinal = value; }
        }


        // This indexer allows for syntax like TypedSet["FieldName1"] to return the value of FieldName1 from the current row buffer
        //public object this[string fieldName]
        //{
        //	get 
        //	{ 
        //		return rowCache[rowCacheIndex][FieldOrdinal( fieldName )]; 
        //	}


        //	set 
        //	{
        //		DumpRecord( string.Format("Before field '{0}' update",fieldName) );
        //		rowCache[rowCacheIndex][FieldOrdinal( fieldName )] = value;
        //		DumpRecord( string.Format( "After field '{0}' update", fieldName ) );
        //	}
        //}

        // This indexer allows for syntax like TypedSet[1] to return the value of Field #1 from the current row buffer

        //public object this[int fieldOrdinal]
        //{
        //	get
        //	{
        //		return rowCache[rowCacheIndex][fieldOrdinal];
        //	}


        //	set
        //	{
        //		DumpRecord( string.Format( "Before field '{0}' update", fieldOrdinal ) );
        //		rowCache[rowCacheIndex][fieldOrdinal] = value;
        //		DumpRecord( string.Format( "After field '{0}' update", fieldOrdinal ) );
        //	}
        //}


        public SetSchema Schema
        {
            get { return setschema!; }
        }


        public int FieldCount
        {
            get
            {
                return setschema!.FieldCount;
            }
        }


        public bool IsDirty
        {
            get { return rowCache![rowCacheIndex].IsDirty; }
            set { rowCache![rowCacheIndex].IsDirty = value; }
        }
        #endregion

        #region Methods
        internal void ReInitializeRecord()
        {
            rowCache![rowCacheIndex].InitializeEmptyRecord();
        }

        internal byte[] GetFieldBytes(int fieldOrdinal)
        {
            //	DumpRecord( string.Format( "Before field '{0}' get", fieldOrdinal ) );
            byte[] fieldBytes = rowCache![rowCacheIndex].GetFieldBytes(fieldOrdinal);
            //	DumpRecord( string.Format( "After field '{0}' get", fieldOrdinal ) );
            return fieldBytes;
        }

        public byte[] PutFieldBytes(int fieldOrdinal, byte[] fieldBytes)
        {
            //	DumpRecord( string.Format( "Before field '{0}' put", fieldOrdinal ) );
            byte[] result = rowCache![rowCacheIndex].PutFieldBytes(fieldOrdinal, fieldBytes);
            //	DumpRecord( string.Format( "After field '{0}' put", fieldOrdinal ) );
            return result;
        }

        public byte[] GetRecordBytes(ref int byteCount)
        {
            return rowCache![rowCacheIndex].GetRecordBytes(ref byteCount);
        }

        public void PutRecordBytes(byte[] recordBytes)
        {
            //rowCache![rowCacheIndex].Record = recordBytes;
            Buffer.BlockCopy(recordBytes, 0, rowCache![rowCacheIndex].rowBuffer, 0, recordBytes.Length);
        }

        public int FieldOrdinal(string fieldName)
        {
            int result = -1;
            string fieldNameToSearch = fieldName.ToUpper();
            for (int i = 0; i < setschema!.FieldCount; i++)
            {
                if (setschema[i].Name == fieldNameToSearch)
                {
                    result = i;
                    break;
                }
            }
            if (result == -1)
            {
                throw new InvalidOperationException(string.Format("Unknown field name '{0}'", fieldNameToSearch));
            }
            return result;
        }

        #endregion
        #endregion

        #region Private Helpers
        internal void DumpSetMember(string contextmsg)
        {
            Debug.Print(" ============ SETMEMBER DUMP BEGIN {0} ==============", contextmsg);
            Debug.Print("CurrentOrdinal: {0}", ordinal);
            Debug.Print("IsDirty: {0}", IsDirty);
            Debug.Print("RecordType: {0}", (RecordType)rowCache[rowCacheIndex].GetRecordType());
            //Debug.Print( "RecordVersion: {0}", rowCache[rowCacheIndex].GetRecordVersion() );
            Debug.Print("RecordSize: Header={0}, Data={1},  Total={2}", rowCache[rowCacheIndex].GetTotalRecordHeaderSize(), rowCache[rowCacheIndex].GetUsedRecordLength(), rowCache[rowCacheIndex].GetTotalUsedRecordSize());
            Debug.Print("FieldCount: {0}", FieldCount);
            Debug.Print("--- FIELD DUMP BEGIN ------------------------");
            for (int i = 0; i < FieldCount; i++)
            {
                Debug.Print("    Field#{0}:   Offset:{1} {2}", i, rowCache[rowCacheIndex].GetFieldOffsetFromRecord(i),
                    (rowCache[rowCacheIndex].IsFieldUninitializedInRow(rowCache[rowCacheIndex].GetFieldOffsetFromRecord(i)) ? "[uninitialized]" : ""));
                byte[] fieldBytes = rowCache[rowCacheIndex].GetFieldBytes(i);
                Debug.Print("    FieldBytes: ");
                for (int j = 0; j < fieldBytes.Length; j++)
                {
                    Debug.Write("      {0}", fieldBytes[j].ToString());
                }
                Debug.Print("    ----");
            }
            Debug.Print("--- FIELD DUMP END   ------------------------");
            Debug.Print(" ============ SETMEMBER DUMP END  {0} ==============", contextmsg);
        }


        private void ValidateFieldIndex(int index)
        {
            if (index < 0 || index > setschema!.FieldCount - 1)
            {
                throw new InvalidOperationException("field index not valid");
            }
        }


        #endregion
    }
}
