using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.SqlServer
{
    internal class SqlDatabaseConnectionItem : IDependentItem, IDependentItemRef
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
    }
}
