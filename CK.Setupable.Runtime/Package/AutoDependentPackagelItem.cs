using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// An AutoDependentPackageItem is a <see cref="IPackageItem"/> associated to a <see cref="IPackageItem"/> that owns it (in 
    /// terms of lifecyle, not in terms of containment) that is projected before or after its owner and reproduces the set of its
    /// owner dependencies on its own dependencies.
    /// Its <see cref="FullName"/> uses a "XXX." prefix followed by the FullName of the associated Package.
    /// </summary>
    /// <remarks>
    /// An AutoDependentPackageItem should only be built or removed by its owner Package itself 
    /// thanks to specific methods that control the existence of the model. The <see cref="DynamicPackageItem"/> use this pattern with its <see cref="DynamicPackageItem.EnsureModelPackage()"/> 
    /// and <see cref="DynamicPackageItem.SupressModelPackage()"/> methods for instance.
    /// </remarks>
    public class AutoDependentPackageItem : IPackageItem, IDependentItemDiscoverer<ISetupItem>, IDependentItemContainerRef
    {
        readonly IPackageItem _package;
        readonly string _prefix;
        readonly string _prefixWithDot;
        IDependentItemContainerRef _explicitContaner;
        IDependentItemList _requires;
        IDependentItemList _requiredBy;
        IDependentItemGroupList _groups;
        IDependentItemList _children;
        bool _frontPackage;
        bool _autoProjectRequirements;

        /// <summary>
        /// Initializes a new <see cref="AutoDependentPackageItem"/> with a specific prefix.
        /// </summary>
        /// <param name="owner">The associated package.</param>
        /// <param name="frontPackage">True to make this package required by its owner, false to make it require its owner.</param>
        /// <param name="prefix">Prefix that must not be null or whitespace ("Objects").</param>
        /// <param name="prefixWithDot">Prefix with additional dot ("Objects.").</param>
        public AutoDependentPackageItem( IPackageItem owner, bool frontPackage, string prefix, string prefixWithDot )
        {
            if( owner == null ) throw new ArgumentNullException( "package" );
            if( string.IsNullOrWhiteSpace( prefix ) ) throw new ArgumentException( "prefix" );
            if( string.IsNullOrWhiteSpace( prefixWithDot )
                || prefixWithDot.Length != prefix.Length + 1
                || prefixWithDot[prefixWithDot.Length-1] != '.'
                || string.CompareOrdinal( prefix, 0, prefixWithDot, 0, prefix.Length ) != 0 ) throw new ArgumentException( "prefixWithDot" );
            _frontPackage = frontPackage;
            _package = owner;
            _prefix = prefix;
            _prefixWithDot = prefixWithDot;
            _autoProjectRequirements = true;
        }

        /// <summary>
        /// Gets the "owner" package.
        /// </summary>
        public IPackageItem Package => _package; 

        /// <summary>
        /// Gets the prefix.
        /// </summary>
        public string Prefix => _prefix; 

        /// <summary>
        /// Gets or sets whether any <see cref="Package"/> requirements (that is not itself a AutoDependentPackageItem) is 
        /// automatically projected as a requirement to its AutoDependentPackageItem with the same prefix on this 
        /// one (the package name is prefixed with "?<see cref="Prefix"/>.").
        /// Defaults to true and applies to Requires, RequiredBy, Groups, Children, Generalization relations and Container
        /// if an explicit <see cref="Container"/> is not set.
        /// </summary>
        /// <remarks>
        /// <para>
        /// States whether the AutoDependentPackageItem with the same Prefix of the packages that our <see cref="Package"/> requires are automatically required by this AutoDependentPackageItem
        /// and whether AutoDependentPackageItem with the same prefix of the packages that states to be required by our package automatically require this AutoDependentPackageItem.
        /// </para>
        /// <para>
        /// Said differently: 
        /// "If I require a package "A", then my own "XXX" requires "XXX.A" (if A has an AutoDependentPackageItem with prefix "XXX").".
        /// Or, for the "required by": 
        /// "If I want to be required by "B" (ie. I must be before "B"), then if "B" has a "XXX", my "XXX" must also be before "XXX.B".".
        /// </para>
        /// </remarks>
        public bool AutoProjectRequirements
        {
            get { return _autoProjectRequirements; }
            set { _autoProjectRequirements = value; }
        }

        /// <summary>
        /// Gets the context of this model that is the same as the <see cref="P:Package"/>.
        /// </summary>
        public string Context => _package.Context; 

        /// <summary>
        /// Gets the location of this model that is the same as the <see cref="P:Package"/>.
        /// </summary>
        public string Location => _package.Location; 

        /// <summary>
        /// Gets the name of this <see cref="AutoDependentPackageItem"/>: it is the <see cref="P:Package"/>'s name prefixed by "<see cref="Prefix"/>.".
        /// </summary>
        public string Name => _prefixWithDot + _package.Name; 

        /// <summary>
        /// Gets the full name of this model.
        /// </summary>
        public string FullName => DefaultContextLocNaming.Format( _package.Context, _package.Location, Name ); 

        string IContextLocNaming.TransformArg => null;

        /// <summary>
        /// Gets or set the container to which this dependent package explicitly belongs. 
        /// It is totally independent of the <see cref="Package"/>'s container and should be let to null:
        /// if Package's Container has a similar (<see cref="Prefix"/>) package it will be the container of 
        /// this dependent package.
        /// </summary>
        public IDependentItemContainerRef Container
        {
            get { return _explicitContaner; }
            set { _explicitContaner = value; }
        }

        /// <summary>
        /// Gets a mutable list of items that this AutoDependentPackageItem requires.
        /// </summary>
        public IDependentItemList Requires => _requires ?? (_requires = DependentItemListFactory.CreateItemList()); 

        /// <summary>
        /// Gets a mutable list of items that are required by this AutoDependentPackageItem.
        /// </summary>
        public IDependentItemList RequiredBy => _requiredBy ?? (_requiredBy = DependentItemListFactory.CreateItemList()); 

        /// <summary>
        /// Gets a mutable list of groups to which this AutoDependentPackageItem belongs.
        /// </summary>
        public IDependentItemGroupList Groups => _groups ?? (_groups = DependentItemListFactory.CreateItemGroupList()); 
        
        /// <summary>
        /// Gets the version: it is the same as the <see cref="P:Package"/>'s one.
        /// </summary>
        public Version Version => _package.Version; 

        /// <summary>
        /// Gets the mutable children list.
        /// </summary>
        public IDependentItemList Children => _children ?? (_children = DependentItemListFactory.CreateItemList()); 

        IDependentItemRef IDependentItem.Generalization
        {
            get { return _package.Generalization != null ? new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( _package.Generalization.FullName, _prefixWithDot ), true ) : null; }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get
            {
                return _explicitContaner 
                        ?? (_package.Container != null 
                            ? new NamedDependentItemContainerRef( DefaultContextLocNaming.AddNamePrefix( _package.Container.FullName, _prefixWithDot ), true ) 
                            : null);
            }
        }

        object IDependentItem.StartDependencySort( IActivityMonitor m ) => typeof( SetupItemDriver );

        bool IDependentItemRef.Optional => false; 

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get 
            {
                var thisRequires = _requires.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) );
                if( _autoProjectRequirements )
                {
                    var req = _package.Requires;
                    if( req != null )
                    {
                        var fromPackage = req.Where( r => !DefaultContextLocNaming.NameStartsWith( r.FullName, _prefixWithDot ) )
                                             .Select( r => new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( r.FullName, _prefixWithDot ), true ) );
                        thisRequires = thisRequires != null ? thisRequires.Concat( fromPackage ) : fromPackage;
                    }
                }
                if( _frontPackage ) return thisRequires;
                return thisRequires != null ? thisRequires.Append( _package.GetReference() ) : new [] { _package.GetReference() };
            }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get
            {
                var thisRequiredBy = _requiredBy.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) );
                if( _autoProjectRequirements )
                {
                    var reqBy = _package.RequiredBy;
                    if( reqBy != null )
                    {
                        var fromPackage = reqBy.Where( r => !DefaultContextLocNaming.NameStartsWith( r.FullName, _prefixWithDot ) )
                                                .Select( r => new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( r.FullName, _prefixWithDot ), true ) );
                        thisRequiredBy = thisRequiredBy != null ? thisRequiredBy.Concat( fromPackage ) : fromPackage;
                    }
                }
                if( !_frontPackage ) return thisRequiredBy;
                return thisRequiredBy != null ? thisRequiredBy.Append( _package.GetReference() ) : new[] { _package.GetReference() };
            }
        }

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get
            {
                var thisGroups = _groups.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) );
                if( _autoProjectRequirements )
                {
                    var groups = _package.Groups;
                    if( groups != null )
                    {
                        var fromPackage = groups.Where( r => !DefaultContextLocNaming.NameStartsWith( r.FullName, _prefixWithDot ) )
                                                .Select( r => new NamedDependentItemGroupRef( DefaultContextLocNaming.AddNamePrefix( r.FullName, _prefixWithDot ), true ) );
                        thisGroups = thisGroups != null ? thisGroups.Concat( fromPackage ) : fromPackage;
                    }
                }
                return thisGroups;
            }
        }


        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get
            {
                var thisChildren = _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) );
                if( _autoProjectRequirements )
                {
                    var children = _package.Children;
                    if( children != null )
                    {
                        var fromPackage = children.Where( r => !DefaultContextLocNaming.NameStartsWith( r.FullName, _prefixWithDot ) )
                                                    .Select( r => new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( r.FullName, _prefixWithDot ), true ) );
                        thisChildren = thisChildren != null ? thisChildren.Concat( fromPackage ) : fromPackage;
                    }
                }
                return thisChildren;
            }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get 
            {
                //var pp = _package.PreviousNames;
                //if( pp.Any() )
                //{
                //    var f = pp.First();
                //    var name = f.FullName;
                //    var newName = DefaultContextLocNaming.AddNamePrefix( name, _prefixWithDot );
                //    var f2 = new VersionedName( newName, f.Version );
                //}
                return _package.PreviousNames.Select( p => new VersionedName( DefaultContextLocNaming.AddNamePrefix( p.FullName, _prefixWithDot ), p.Version ) ); 
            }
        }

        string IVersionedItem.ItemType =>  _prefix; 


        IEnumerable<ISetupItem> IDependentItemDiscoverer<ISetupItem>.GetOtherItemsToRegister() => new[] { _package };

    }


}
