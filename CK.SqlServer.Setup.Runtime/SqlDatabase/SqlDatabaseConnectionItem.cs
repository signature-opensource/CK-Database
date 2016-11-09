#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlDatabase\SqlDatabaseConnectionItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Collections.Generic;
using CK.Setup;
using CK.Core;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseConnectionItem : ISetupItem, IDependentItemRef
    {
        readonly SqlDatabaseItem _db;

        public SqlDatabaseConnectionItem( SqlDatabaseItem db )
        {
            _db = db;
        }

        public SqlDatabase SqlDatabase => _db.ActualObject;

        public string FullName => _db.FullName + ".Connection"; 

        public IDependentItemContainerRef Container => null; 

        public IDependentItemRef Generalization => null; 

        public IEnumerable<IDependentItemRef> Requires => null; 

        public IEnumerable<IDependentItemGroupRef> Groups => null; 

        public IEnumerable<IDependentItemRef> RequiredBy => null; 

        public object StartDependencySort( IActivityMonitor m ) => typeof( SqlDatabaseConnectionItemDriver );

        bool IDependentItemRef.Optional => false; 

        public string Context => _db.Context; 

        public string Location => _db.Location;

        string IContextLocNaming.TransformArg => null;

        public string Name => _db.Name + ".Connection"; 
    }
}
