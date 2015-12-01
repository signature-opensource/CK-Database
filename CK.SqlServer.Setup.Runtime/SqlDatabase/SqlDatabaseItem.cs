#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlDatabase\SqlDatabaseItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseItem : StObjDynamicContainerItem
    {
        internal readonly SqlDatabaseConnectionItem ConnectionItem;
        
        public SqlDatabaseItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Context = data.StObj.Context.Context;
            Location = GetObject().Name;
            ConnectionItem = new SqlDatabaseConnectionItem( this );
            Requires.Add( ConnectionItem );
        }

        public new SqlDatabase GetObject()
        {
            return (SqlDatabase)base.GetObject();
        }
    }
}
