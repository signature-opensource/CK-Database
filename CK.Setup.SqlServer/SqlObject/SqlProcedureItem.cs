using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup.Database;

namespace CK.Setup.SqlServer
{
    public class SqlProcedureItem : SqlObjectItem
    {
        internal SqlProcedureItem( ReadInfo readInfo )
            : base( SqlObjectItem.TypeProcedure, readInfo )
        {
        }


    }
}
