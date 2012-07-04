using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup.Database;

namespace CK.Setup.SqlServer
{
    public class SqlProcedure : SqlObject
    {
        internal SqlProcedure( ReadInfo readInfo )
            : base( SqlObject.TypeProcedure, readInfo )
        {
        }


    }
}
