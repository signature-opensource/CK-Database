using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public abstract class Schema : ISetupableItem
    {
        string _name;

        protected Schema()
        {
            _name = GetType().Name;
        }

        public string FullName
        {
            get { return _name; }
            set { _name = value; }
        }

        string ISetupableItem.SetupDriverTypeName
        {
            get { return typeof(SchemaSetupDriver).AssemblyQualifiedName; }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return null; }
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        IEnumerable<string> IDependentItem.Requires
        {
            get { return null; }
        }

        IEnumerable<string> IDependentItem.RequiredBy
        {
            get { return null; }
        }

        string IVersionedItem.ItemType
        {
            get { return "Schema"; }
        }

        Version IVersionedItem.Version
        {
            get { return Util.EmptyVersion; }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return null; }
        }
    }
}
