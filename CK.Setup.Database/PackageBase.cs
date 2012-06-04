using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public abstract class PackageBase : MultiVersionedItem, ISetupableItemContainer
    {
        List<string> _requires;
        List<string> _requiredBy;
        List<IDependentItemRef> _children;
        IDependentItemContainerRef _container;

        public PackageBase()
            : this( "Package" )
        {
        }

        public PackageBase( string itemType )
            : base( itemType )
        {
        }

        public IList<string> Requires
        {
            get { return _requires ?? (_requires = new List<string>()); }
        }

        public IList<string> RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new List<string>()); }
        }

        /// <summary>
        /// Splits the parameter on the comma and appends the strings in <see cref="Requires"/>.
        /// </summary>
        /// <param name="commaSeparatedRequires">Comma separated requires. When null or empty, nothing is added.</param>
        public void AddRequiresString( string commaSeparatedRequires )
        {
            if( !String.IsNullOrWhiteSpace( commaSeparatedRequires ) )
            {
                Requires.AddRangeArray( commaSeparatedRequires.Split( new[] { ',', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries ) );
            }
        }

        /// <summary>
        /// Splits the parameter on the comma and appends the stings in <see cref="RequiredBy"/>.
        /// </summary>
        /// <param name="commaSeparatedRequiredBy">Comma separated requires. When null or empty, nothing is added.</param>
        public void AddRequiredByString( string commaSeparatedRequiredBy )
        {
            if( !String.IsNullOrWhiteSpace( commaSeparatedRequiredBy ) )
            {
                RequiredBy.AddRangeArray( commaSeparatedRequiredBy.Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) );
            }
        }

        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
        }

        public IList<IDependentItemRef> Children
        {
            get { return _children ?? (_children = new List<IDependentItemRef>()); }
        }

        protected abstract string GetSetupDriverTypeName();


        string ISetupableItem.SetupDriverTypeName
        {
            get { return GetSetupDriverTypeName(); }
        }

        string ISetupableItem.FullName
        {
            get { return GetFullName(); }
        }

        string IDependentItemContainer.FullName
        {
            get { return GetFullName(); }
        }

        string IDependentItemContainerRef.FullName
        {
            get { return GetFullName(); }
        }

        string IDependentItemRef.FullName
        {
            get { return GetFullName(); }
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        IEnumerable<string> IDependentItem.Requires
        {
            get { return _requires; }
        }

        IEnumerable<string> IDependentItem.RequiredBy
        {
            get { return _requiredBy; }
        }

        IEnumerable<IDependentItemRef> IDependentItemContainer.Children
        {
            get { return _children; }
        }
    }

}
