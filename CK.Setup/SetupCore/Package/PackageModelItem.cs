using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A PackageModel is a <see cref="IPackageItem"/> associated to a <see cref="IPackageItem"/>.
    /// A Model <see cref="FullName"/> is the "Model." prefix followed by the <see cref="DynamicPackageItem.FullName"/>.
    /// It is by default required by its DynamicPackage (<see cref="DynamicPackageItem.AutomaticModelRequirement"/> can change this).
    /// </summary>
    /// <remarks>
    /// Since there can be at most one Model per Package, a PackageModel should only be built or removed by its owner Package itself 
    /// thanks to specific methods that control the existence of the model. The <see cref="DynamicPackageItem"/> use this pattern with its <see cref="DynamicPackageItem.EnsureModel()"/> 
    /// and <see cref="DynamicPackageItem.SupressModel()"/> methods.
    /// </remarks>
    public class PackageModelItem : IPackageItem, IDependentItemDiscoverer, IDependentItemContainerRef
    {
        IPackageItem _package;
        IDependentItemContainerRef _container;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        IDependentItemList _children;
        bool _automaticModelRequirement;

        /// <summary>
        /// Initializes a new <see cref="PackageModelItem"/>.
        /// </summary>
        /// <param name="p">The associated package.</param>
        public PackageModelItem( IPackageItem package )
        {
            if( package == null ) throw new ArgumentNullException( "package" );
            _package = package;
            _automaticModelRequirement = true;
        }

        /// <summary>
        /// Gets the package for which this one is the Model.
        /// </summary>
        public IPackageItem Package
        {
            get { return _package; }
        }

        /// <summary>
        /// Gets or sets whether any <see cref="Package"/> requirements (that is not itself a Model) is automatically projected as a requirement to its Model on 
        /// this Model (the package name is prefixed with "?Model.").
        /// Defaults to true and applies to <see cref="DynamicPackageItem.Requires"/> and <see cref="DynamicPackageItem.RequiredBy"/>.
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
        /// Gets the full name of this model, that is "Model." prefixed to <see cref="Package"/>.<see cref="DynamicPackageItem.FullName">FullName</see>.
        /// </summary>
        public string FullName
        {
            get { return "Model." + _package.FullName; }
        }

        /// <summary>
        /// Gets the container to which this Model belongs. 
        /// It is totally independent of the <see cref="Package"/>'s container and should be let to null
        /// to minimize constraints on the graph.
        /// </summary>
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

        /// <summary>
        /// Gets the version: it is the same as the <see cref="P:Package"/>'s one.
        /// </summary>
        public Version Version
        {
            get { return _package.Version; }
        }

        public IDependentItemList Children
        {
            get { return _children ?? (_children = new DependentItemList()); }
        }

        IDependentItemRef IDependentItem.Generalization
        {
            get { return null; }
        }

        object IDependentItem.StartDependencySort()
        {
            return typeof( SetupDriver );
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
            get { return "Model"; }
        }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children; }
        }


        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            return new ReadOnlyListMono<IDependentItem>( _package );
        }
    }


}
