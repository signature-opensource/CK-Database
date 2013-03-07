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
        DependentItemGroupList _groups;
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
        /// Gets the context of this model that is the same as the <see cref="P:Package"/>.
        /// </summary>
        public string Context
        {
            get { return _package.Context; }
        }

        /// <summary>
        /// Gets the location of this model that is the same as the <see cref="P:Package"/>.
        /// </summary>
        public string Location
        {
            get { return _package.Location; }
        }

        /// <summary>
        /// Gets the name of this model: it is the <see cref="P:Package"/>'s name prefixed by "Model.".
        /// </summary>
        public string Name
        {
            get { return "Model." + _package.Name; }
        }

        /// <summary>
        /// Gets the full name of this model.
        /// </summary>
        public string FullName
        {
            get { return DefaultContextLocNaming.Format( _package.Context, _package.Location, Name ); }
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

        /// <summary>
        /// Gets a mutable list of items that this Model requires.
        /// </summary>
        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of items that are required by this Model.
        /// </summary>
        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of groups to which this Model belongs.
        /// </summary>
        public IDependentItemGroupList Groups
        {
            get { return _groups ?? (_groups = new DependentItemGroupList()); }
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
            get { return _package.Generalization != null ? new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( _package.Generalization.FullName, "Model." ), true ) : null; }
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
                var thisRequires = _requires.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) );
                if( _automaticModelRequirement )
                {
                    var req = _package.Requires;
                    if( req != null )
                    {
                        var fromPackage = req.Where( r => !DefaultContextLocNaming.NameStartsWith( r.FullName, "Model." ) )
                                             .Select( r => new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( r.FullName, "Model." ), true ) );
                        return thisRequires != null ? thisRequires.Concat( fromPackage ) : fromPackage;
                    }
                }
                return thisRequires; 
            }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get
            {
                var thisRequiredBy = _requiredBy.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) );
                if( _automaticModelRequirement )
                {
                    var reqBy = _package.RequiredBy;
                    if( reqBy != null )
                    {
                        var fromPackage = reqBy.Where( r => !DefaultContextLocNaming.NameStartsWith( r.FullName, "Model." ) )
                                                .Select( r => new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( r.FullName, "Model." ), true ) );
                        return thisRequiredBy != null ? thisRequiredBy.Concat( fromPackage ) : fromPackage;
                    }
                }
                return thisRequiredBy;
            }
        }

        //TODO: CHECK that Group relationships supports optionality and projects them into potential "Model." groups. 
        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get { return _groups.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) ); }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get 
            {
                var pp = _package.PreviousNames;
                if( pp.Any() )
                {
                    var f = pp.First();
                    var name = f.FullName;
                    var newName = DefaultContextLocNaming.AddNamePrefix( name, "Model." );
                    var f2 = new VersionedName( newName, f.Version );
                }
                return _package.PreviousNames.Select( p => new VersionedName( DefaultContextLocNaming.AddNamePrefix( p.FullName, "Model." ), p.Version ) ); 
            }
        }

        string IVersionedItem.ItemType
        {
            get { return "Model"; }
        }

        //TODO: CHECK that Children relationships supports optionality and projects them into potential "Model." children. ?? Not sure it is a good idea for container/Children... 
        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) ); }
        }


        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            return new ReadOnlyListMono<IDependentItem>( _package );
        }
    }


}
