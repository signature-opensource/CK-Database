#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObject\SqlFunctionItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.SqlServer.Setup
{
    public class SqlFunctionItem : SqlObjectItem
    {
        internal SqlFunctionItem( SqlObjectProtoItem p )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeFunction );
        }
    }
}
