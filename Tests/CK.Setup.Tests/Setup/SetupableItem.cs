using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Tests
{
    class SetupableItem : Item, ISetupableItem
    {
        public SetupableItem( string fullName, params object[] content )
            : base( fullName, content )
        {
        }

       public Version Version
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<VersionedName> PreviousNames
        {
            get { throw new NotImplementedException(); }
        }

        public string SetupDriverTypeName
        {
            get { throw new NotImplementedException(); }
        }

        string IVersionedItem.ItemType
        {
            get { return "TestItem"; }
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

    }
}
