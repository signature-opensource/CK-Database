using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.Database
{
    /// <summary>
    /// A DynamicPackageModel is a <see cref="IPackage"/> associated to a <see cref="DynamicPackage"/>.
    /// It can only be built or removed by the Package itself through <see cref="DynamicPackage.EnsureModel()"/> and <see cref="DynamicPackage.SupressModel()"/> methods.
    /// A Model <see cref="FullName"/> is the "Model." prefix followed by the <see cref="DynamicPackage.FullName"/>.
    /// It is by default required by its DynamicPackage (<see cref="DynamicPackage.AutomaticModelRequirement"/> can change this).
    /// </summary>
    public class DynamicPackageModel : IPackage, IDependentItemDiscoverer, IDependentItemContainerRef
    {
        DynamicPackage _package;
        IDependentItemContainerRef _container;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        List<IDependentItemRef> _children;
        bool _automaticModelRequirement;

        internal DynamicPackageModel( DynamicPackage p )
        {
            _package = p;
            _automaticModelRequirement = true;
        }

        /// <summary>
        /// Gets the package for which this one is the Model.
        /// </summary>
        public DynamicPackage Package
        {
            get { return _package; }
        }

        /// <summary>
        /// Gets or sets whether any package requirements (that is not itself a Model) is automatically projected as a requirement on 
        /// a Model (the package name is prefixed with "?Model.").
        /// Defaults to true and applies to <see cref="DynamicPackage.Requires"/> and <see cref="DynamicPackage.RequiredBy"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// States whether the Models of the packages that our <see cref="Package"/> requires are automatically required by this Model
        /// and whether Models of the packages that states to be required by our package automatically require this Model.
        /// </para>
        /// <para>
        /// Said differently: 
        /// "If I require a package "A", then my own Model requires "Model.A" (if A has a model).".
        /// Or, for the "required by": 
        /// "If I want to be required by "B" (ie. I must be before "B"), then if "B" has a model, my Model must also be before "Model.B".".
        /// </para>
        /// </remarks>
        public bool AutomaticModelRequirement
        {
            get { return _automaticModelRequirement; }
            set { _automaticModelRequirement = value; }
        }

        /// <summary>
        /// Gets the full name of this model, that is "Model." prefixed to <see cref="Package"/>.<see cref="DynamicPackage.FullName">FullName</see>.
        /// </summary>
        public string FullName
        {
            get { return "Model." + _package.FullName; }
        }


        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
        }

        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        public Version Version
        {
            get { return _package.Version; }
        }

        public IList<IDependentItemRef> Children
        {
            get { return _children ?? (_children = new List<IDependentItemRef>()); }
        }

        object IDependentItem.StartDependencySort()
        {
            return typeof(ContainerDriver).AssemblyQualifiedName;
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get 
            {
                if( _automaticModelRequirement )
                {
                    var fromPackage = _package.Requires
                                                .Where( r => !r.FullName.StartsWith( "Model.", StringComparison.Ordinal ) )
                                                .Select( r => new NamedDependentItemRef( "?Model." + r.FullName ) );
                    return _requires != null ? _requires.Concat( fromPackage ) : fromPackage;
                }
                return _requires; 
            }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get 
            {
                if( _automaticModelRequirement )
                {
                    var fromPackage = _package.RequiredBy
                                                .Where( r => !r.FullName.StartsWith( "Model.", StringComparison.Ordinal ) )
                                                .Select( r => new NamedDependentItemRef( "?Model." + r.FullName ) );
                    return _requiredBy != null ? _requiredBy.Concat( fromPackage ) : fromPackage;
                }
                return _requiredBy; 
            }
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


        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            return new ReadOnlyListMono<IDependentItem>( _package );
        }
    }


}
