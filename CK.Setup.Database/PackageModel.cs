using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.Database
{
    public class PackageModel : ISetupableItemContainer, IDependentItemDiscoverer
    {
        Package _package;
        IDependentItemContainerRef _container;
        List<string> _requires;
        List<string> _requiredBy;
        List<IDependentItemRef> _children;
        bool _automaticModelRequirement;

        internal PackageModel( Package p )
        {
            _package = p;
            _automaticModelRequirement = true;
        }

        public Package Package
        {
            get { return _package; }
        }

        /// <summary>
        /// Gets or sets whether the Models of the packages required by
        /// our <see cref="Pachage"/> are automatically required by this Model.
        /// Defaults to true.
        /// </summary>
        public bool AutomaticModelRequirement
        {
            get { return _automaticModelRequirement; }
            set { _automaticModelRequirement = value; }
        }

        public string FullName
        {
            get { return "Model." + _package.FullName; }
        }

        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
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
        /// Splits the parameter on the comma and appends the stings in <see cref="Requires"/>.
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

        public Version Version
        {
            get { return _package.Version; }
        }

        public IList<IDependentItemRef> Children
        {
            get { return _children ?? (_children = new List<IDependentItemRef>()); }
        }

        string ISetupableItem.SetupDriverTypeName
        {
            get { return typeof(SetupDriverContainer).AssemblyQualifiedName; }
        }

        IEnumerable<string> IDependentItem.Requires
        {
            get 
            {
                if( _automaticModelRequirement )
                {
                    return _requires.Concat( _package.Requires.Select( r => "?Model." + r ) );
                }
                return _requires; 
            }
        }

        IEnumerable<string> IDependentItem.RequiredBy
        {
            get { return _requiredBy; }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _package.PreviousNames.Select( p => new VersionedName( "Model." + p.FullName, p.Version ) ); }
        }

        string IVersionedItem.ItemType
        {
            get { return "Package"; }
        }

        IEnumerable<IDependentItemRef> IDependentItemContainer.Children
        {
            get { return _children; }
        }


        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetDependentItems()
        {
            return new []{ _package };
        }
    }


}
