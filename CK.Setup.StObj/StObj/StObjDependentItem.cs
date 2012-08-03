using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    internal class StObjDependentItem : IStObjDependentItem
    {
        Type _itemType;

        public StObjDependentItem( Type itemType )
        {
            _itemType = itemType;
        }

        #region IStObjDependentItem Members

        public Type ItemType
        {
            get { return _itemType; }
        }

        public void InitDependentItem( IActivityLogger logger, IStObjMapper mapper )
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDependentItem Members

        public string FullName
        {
            get { throw new NotImplementedException(); }
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

        public object StartDependencySort()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
