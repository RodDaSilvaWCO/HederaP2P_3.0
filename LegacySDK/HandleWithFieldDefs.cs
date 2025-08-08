using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosys.Common.Types;

namespace Unosys.SDK
{
    public class HandleWithFieldDefs
    {
        public long Handle;
        public SetSchema TableSchema = null!;
        public APIOperationResponseCode responseCode = APIOperationResponseCode.OK;
        public ulong InitialRecordOrdinal = 0;
        public ulong LastRecordOrdinal = 0;
        public bool IsDeleted = false;

        public HandleWithFieldDefs(long handle, ulong initialRecordOrdinal, ulong lastRecordOrdinal, bool isDeleted, SetSchema tableSchema, APIOperationResponseCode result)
        {
            Handle = handle;
            InitialRecordOrdinal = initialRecordOrdinal;  // the record ordinal of the first record to be positioned on after Open/Create
            LastRecordOrdinal = lastRecordOrdinal;        // the last record of the Open/Create Table
            IsDeleted = isDeleted;                        // the deleted status of the first record to be positioned on after Open/Create
            TableSchema = tableSchema;
            responseCode = result;
        }
    }
}
