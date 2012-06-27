using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class DatabaseItem : IDependentItemContainer
    {
        public DatabaseItem( Database db )
        {
        }

        public Database Database { get; private set; }

        #region IDependentItemContainer Members

        public IEnumerable<IDependentItemRef> Children
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IDependentItem Members

        public string FullName
        {
            get { return Database.Name; }
        }

        public IDependentItemContainerRef Container
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IDependentItemRef> Requires
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IDependentItemRef> RequiredBy
        {
            get { throw new NotImplementedException(); }
        }

        object IDependentItem.StartDependencySort()
        {
            return typeof( DatabaseSetupDriver );
        }

        #endregion
    }
}
