#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\PackageModelItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// thanks to specific methods that control the existence of the model. The <see cref="DynamicPackageItem"/> use this pattern with its <see cref="DynamicPackageItem.EnsureModel()"/> 
    /// and <see cref="DynamicPackageItem.SupressModel()"/> methods for instance.
    /// </remarks>
    public class AutoDependentPackageItem : IPackageItem, IDependentItemDiscoverer<ISetupItem>, IDependentItemContainerRef
    {
        readonly IPackageItem _package;
        readonly string _prefix;
        readonly string _prefixWithDot;
        IDependentItemContainerRef _container;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        IDependentItemList _children;
        bool _frontPackage;
        bool _automaticModelRequirement;

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
            if( String.IsNullOrWhiteSpace( prefix ) ) throw new ArgumentException( "prefix" );
            if( String.IsNullOrWhiteSpace( prefixWithDot )
                || prefixWithDot.Length != prefix.Length + 1
                || prefixWithDot[prefixWithDot.Length-1] != '.'
                || String.CompareOrdinal( prefix, 0, prefixWithDot, 0, prefix.Length ) != 0 ) throw new ArgumentException( "prefixWithDot" );
            _frontPackage = frontPackage;
            _package = owner;
            _prefix = prefix;
            _prefixWithDot = prefixWithDot;
            _automaticModelRequirement = true;
        }

        /// <summary>
        /// Gets the "owner" package.
        /// </summary>
        public IPackageItem Package
        {
            get { return _package; }
        }

        /// <summary>
        /// Gets the prefix.
        /// </summary>
        public string Prefix
        {
            get { return _prefix; }
        }

        /// <summary>
        /// Gets or sets whether any <see cref="Package"/> requirements (that is not itself a AutoDependentPackageItem) is automatically projected as a requirement to its AutoDependentPackageItem 
        /// with the same prefix on this one (the package name is prefixed with "?<see cref="Prefix"/>.").
        /// Defaults to true and applies to Requires and RequiredBy relations.
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
        /// Gets the name of this <see cref="AutoDependentPackageItem"/>: it is the <see cref="P:Package"/>'s name prefixed by "<see cref="Prefix"/>.".
        /// </summary>
        public string Name
        {
            get { return _prefixWithDot + _package.Name; }
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
        /// Gets a mutable list of items that this AutoDependentPackageItem requires.
        /// </summary>
        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of items that are required by this AutoDependentPackageItem.
        /// </summary>
        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of groups to which this AutoDependentPackageItem belongs.
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

        /// <summary>
        /// Gets the children list.
        /// </summary>
        public IDependentItemList Children
        {
            get { return _children ?? (_children = new DependentItemList()); }
        }

        IDependentItemRef IDependentItem.Generalization
        {
            get { return _package.Generalization != null ? new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( _package.Generalization.FullName, _prefixWithDot ), true ) : null; }
        }

        object IDependentItem.StartDependencySort()
        {
            return typeof( GenericItemSetupDriver );
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
                        var fromPackage = req.Where( r => !DefaultContextLocNaming.NameStartsWith( r.FullName, _prefixWithDot ) )
                                             .Select( r => new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( r.FullName, _prefixWithDot ), true ) );
                        thisRequires = thisRequires != null ? thisRequires.Concat( fromPackage ) : fromPackage;
                    }
                }
                if( _frontPackage ) return thisRequires;
                return thisRequires != null ? thisRequires.Append( _package.GetReference() ) : new CKReadOnlyListMono<IDependentItemRef>( _package.GetReference() );
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
                        var fromPackage = reqBy.Where( r => !DefaultContextLocNaming.NameStartsWith( r.FullName, _prefixWithDot ) )
                                                .Select( r => new NamedDependentItemRef( DefaultContextLocNaming.AddNamePrefix( r.FullName, _prefixWithDot ), true ) );
                        thisRequiredBy = thisRequiredBy != null ? thisRequiredBy.Concat( fromPackage ) : fromPackage;
                    }
                }
                if( !_frontPackage ) return thisRequiredBy;
                return thisRequiredBy != null ? thisRequiredBy.Append( _package.GetReference() ) : new CKReadOnlyListMono<IDependentItemRef>( _package.GetReference() );
            }
        }

        //TODO: CHECK that Group relationships supports optionality and projects them into potential "Prefix." groups. 
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
                    var newName = DefaultContextLocNaming.AddNamePrefix( name, _prefixWithDot );
                    var f2 = new VersionedName( newName, f.Version );
                }
                return _package.PreviousNames.Select( p => new VersionedName( DefaultContextLocNaming.AddNamePrefix( p.FullName, _prefixWithDot ), p.Version ) ); 
            }
        }

        string IVersionedItem.ItemType
        {
            get { return _prefix; }
        }

        //TODO: CHECK that Children relationships supports optionality and projects them into potential "Prefix." children. ??
        //      Not sure it is a good idea for container/Children... 
        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _package.Context, _package.Location ) ); }
        }


        IEnumerable<ISetupItem> IDependentItemDiscoverer<ISetupItem>.GetOtherItemsToRegister()
        {
            return new CKReadOnlyListMono<ISetupItem>( _package );
        }
    }


}
