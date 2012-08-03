using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlTableTypeItem : PackageItemBase //, ITypedObjectDependentItem
    {
        readonly Type _itemType;
        // For root class.
        readonly SqlTableAttribute _attr;
        // For inherited class.
        readonly SqlTableOverrideAttribute _overrideAttr;
        readonly SqlTableTypeItem _inherited;

        // Initialized properties.
        string _fullName;

        internal SqlTableTypeItem( Type itemType, SqlTableAttribute attr )
            : base( "TABLE" )
        {
            _itemType = itemType;
            _attr = attr;
            _overrideAttr = null;
            _inherited = null;
        }

        internal SqlTableTypeItem( Type itemType, SqlTableOverrideAttribute attr, SqlTableTypeItem inherited )
            : base( "TABLE" )
        {
            _itemType = itemType;
            _attr = null;
            _overrideAttr = attr;
            _inherited = inherited;
        }

        public Type ItemType
        {
            get { return _itemType; }
        }

        //void ITypedObjectDependentItem.InitDependentItem( IActivityLogger logger, ITypedObjectMapper mapper )
        //{
        //    throw new NotImplementedException();
        //}

        protected override string GetFullName()
        {
            throw new NotImplementedException();
        }

        protected override object StartDependencySort()
        {
            throw new NotImplementedException();
        }
    }
}
