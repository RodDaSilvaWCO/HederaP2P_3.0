using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosys.Kernel;

namespace Unosys.Common.Types
{
    public class OrderDef
    {
        #region Field Members
        public List<byte> FieldOrdinals = new List<byte>();
        #endregion

        #region Constructors
        public OrderDef( List<byte> fieldOrdinals) 
        { 
            FieldOrdinals = fieldOrdinals;
        }
        #endregion
    }
}
