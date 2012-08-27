using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    public class SqlConnectionItem : IDependentItem, IDependentItemRef
    {
        SqlDatabaseItem _db;
        DependentItemList _requires;
        DependentItemList _requiredBy;

        internal SqlConnectionItem( SqlDatabaseItem db )
        {
            Debug.Assert( db != null );
            _db = db;
        }

        public SqlDatabaseItem SqlDatabase 
        { 
            get { return _db; } 
        } 

        public string FullName
        {
            get { return _db.FullName + ".Cnx"; }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _db; }
        }

        public DependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        public DependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _requires; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _requiredBy; }
        }

        object IDependentItem.StartDependencySort()
        {
            return typeof( SqlConnectionSetupDriver );
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }
    }
}
