#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObject\SqlViewObjectItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Diagnostics;

namespace CK.SqlServer.Setup
{
    public class SqlViewObjectItem : SqlObjectItem
    {
        internal SqlViewObjectItem( SqlObjectProtoItem p )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeView );
        }
    }
}
