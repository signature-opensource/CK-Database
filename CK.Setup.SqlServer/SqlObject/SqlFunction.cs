using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup.Database;

namespace CK.Setup.SqlServer
{
    public class SqlFunction : SqlObject
    {
        internal SqlFunction( ReadInfo readInfo )
            : base( SqlObject.TypeFunction, readInfo )
        {
        }
    }
}
