#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlDatabase\SqlDatabaseConnectionItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Collections.Generic;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseConnectionItem : ISetupItem, IDependentItemRef
    {
        readonly SqlDatabaseItem _db;

        public SqlDatabaseConnectionItem( SqlDatabaseItem db )
        {
            _db = db;
        }

        public SqlDatabase SqlDatabase { get { return _db.Object; } }

        public string FullName
        {
            get { return _db.FullName + ".Connection"; }
        }

        public IDependentItemContainerRef Container
        {
            get { return null; }
        }

        public IDependentItemRef Generalization
        {
            get { return null; }
        }

        public IEnumerable<IDependentItemRef> Requires
        {
            get { return null; }
        }

        public IEnumerable<IDependentItemGroupRef> Groups
        {
            get { return null; }
        }

        public IEnumerable<IDependentItemRef> RequiredBy
        {
            get { return null; }
        }

        public object StartDependencySort()
        {
            return typeof( SqlDatabaseConnectionSetupDriver );
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }


        bool ISetupItem.OnDriverCreated( GenericItemSetupDriver driver )
        {
            return true;
        }

        public string Context
        {
            get { return _db.Context; }
        }

        public string Location
        {
            get { return _db.Location; }
        }

        public string Name
        {
            get { return _db.Name + ".Connection"; }
        }
    }
}
