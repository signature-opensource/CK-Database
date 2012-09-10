using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{

    /// <summary>
    /// Static class that offers <see cref="IDependentItem"/> ordering functionnality thanks to <see cref="OrderItems"/>;
    /// </summary>
    public static class DependencySorter
    {
        /// <summary>
        /// Parametrizes the way <see cref="DependencySorter.OrderItems"/> works.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// Gets or sets whether to reverse the lexicographic order for items that share the same rank.
            /// Defaults to false.
            /// </summary>
            public bool ReverseName { get; set; }

            /// <summary>
            /// Gets or sets whether dependencies to any Container the item belongs to should be ignored.
            /// Defaults to false.
            /// </summary>
            public bool SkipDependencyToContainer { get; set; }
        }

        static readonly Options _defaultOptions = new Options();

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <param name="discoverers">An optional set of <see cref="IDependentItemDiscoverer"/> (can be null).</param>
        /// <param name="options">Options for advanced uses.</param>
        /// <returns>A <see cref="DependencySorterResult"/>.</returns>
        public static DependencySorterResult OrderItems( IEnumerable<IDependentItem> items, IEnumerable<IDependentItemDiscoverer> discoverers, Options options = null )
        {
            var computer = new RankComputer( items, discoverers, options ?? _defaultOptions );
            computer.Process();
            return computer.GetResult();
        }

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <returns>A <see cref="DependencySorterResult"/>.</returns>
        public static DependencySorterResult OrderItems( params IDependentItem[] items )
        {
            return OrderItems( items, null, null );
        }

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="reverseName">True to reverse lexicographic order for items that share the same rank.</param>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <returns>A <see cref="DependencySorterResult"/>.</returns>
        public static DependencySorterResult OrderItems( bool reverseName, params IDependentItem[] items )
        {
            return OrderItems( items, null, new Options() { ReverseName = reverseName } );
        }

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="options">Options for advanced uses.</param>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <returns>A <see cref="DependencySorterResult"/>.</returns>
        public static DependencySorterResult OrderItems( Options options, params IDependentItem[] items )
        {
            return OrderItems( items, null, options );
        }

        internal class Entry : ISortedItem
        {
            // This marker saves one iteration over specialized items to resolve Generalizations.
            internal static readonly Entry GeneralizationMissingMarker = new Entry( String.Empty );

            public Entry( IDependentItem e, object startValue )
            {
                StartValue = startValue;
                Item = e;
                FullName = e.FullName;
                Rank = -1;
            }

            public Entry( string fullName )
            {
                FullName = fullName;
                Rank = -1;
            }
            
            public Entry( Entry container )
            {
                Item = container.Item;
                FullName = container.FullName + ".Head";
                Rank = -1;
                ContainerIfHead = container;
            }
            
            public readonly string FullName;
            public object StartValue;
            /// <summary>
            /// Item is set for all kind of entries: Containers, Heads and normal item reference the dependent item.
            /// </summary>
            public IDependentItem Item;
            /// <summary>
            /// Allocated as soon as this entry Requires another one.
            /// </summary>
            public HashSet<IDependentItemRef> Requires;
            public int Rank;
            // Index is computed at the end of the process.
            public int Index;

            public Entry Generalization;

            public Entry Container { get; private set; }
            /// <summary>
            /// The ContainerIfHead is null for normal items and containers.
            /// It is not null only for heads.
            /// </summary>
            public Entry ContainerIfHead { get; private set; }
            /// <summary>
            /// The HeadIfContainer is null for normal items and heads.
            /// It is not null only for container.
            /// </summary>
            public Entry HeadIfContainer { get; private set; }
            public Entry FirstChildIfContainer { get; private set; }
            public Entry NextChildInContainer { get; private set; }

            internal void AddRequiredByRequires( IDependentItemRef req )
            {
                if( Requires == null ) Requires = new HashSet<IDependentItemRef>();
                Requires.Add( req );
            }

            internal void TransformToContainer()
            {
                Debug.Assert( HeadIfContainer == null, "Only once!" );
                Debug.Assert( FirstChildIfContainer == null );
                HeadIfContainer = new Entry( this );
            }

            internal void AddToContainer( Entry child )
            {
                Debug.Assert( child != null );
                Debug.Assert( child.Container == null, "One and only one Container setting." );
                Debug.Assert( child.ContainerIfHead == null, "Never add a head as a Child." );
                CheckNotContains( child );

                child.Container = this;
                if( child.HeadIfContainer != null ) child.HeadIfContainer.Container = this;
                child.NextChildInContainer = FirstChildIfContainer;
                FirstChildIfContainer = child;
            }

            internal bool AppearInContainerChain( Entry dep )
            {
                Debug.Assert( dep.HeadIfContainer != null, "Called only with a Container." );
                Entry c = Container;
                while( c != null )
                {
                    if( c == dep ) return true;
                    c = c.Container;
                }
                return false;
            }

            [Conditional( "DEBUG" )]
            internal void CheckContains( Entry e )
            {
                if( e.ContainerIfHead != null ) 
                {
                    Debug.Assert( e.ContainerIfHead.Container == this );
                    return;
                }
                Debug.Assert( HeadIfContainer != null, "This is a Container..." );
                Entry c = FirstChildIfContainer;
                while( c != null )
                {
                    if( c == e ) return;
                    c = c.NextChildInContainer;
                }
                Debug.Fail( String.Format( "Container {0} does not contain item {1}.", FullName, e.FullName ) );
            }

            [Conditional( "DEBUG" )]
            internal void CheckNotContains( Entry e )
            {
                Debug.Assert( HeadIfContainer != null, "This is a Container..." );
                Entry c = FirstChildIfContainer;
                while( c != null )
                {
                    if( c == e ) Debug.Fail( String.Format( "Container {0} contains item {1}.", FullName, e.FullName ) ); ;
                    c = c.NextChildInContainer;
                }
            }

            public override string ToString()
            {
                return FullName;
            }

            #region ISortedItem Members

            int ISortedItem.Index
            {
                get { return Index; }
            }

            string ISortedItem.FullName
            {
                get { return FullName; }
            }

            int ISortedItem.Rank
            {
                get { return Rank; }
            }

            object ISortedItem.StartValue
            {
                get { return StartValue; }
            }

            ISortedItem ISortedItem.Container
            {
                get { return Container; }
            }

            // By double checking the full name, we handle any "Optional"ity of the original Container reference.
            ISortedItem ISortedItem.ConfiguredContainer 
            {
                get { return Item.Container != null && ReferenceEquals( Item.Container.FullName, Container.FullName ) ? Container : null; } 
            }

            ISortedItem ISortedItem.Generalization
            {
                get { return Generalization == GeneralizationMissingMarker ? null : Generalization; }
            }

            bool ISortedItem.IsContainerHead
            {
                get { return ContainerIfHead != null; }
            }

            bool ISortedItem.IsContainer
            {
                get { return HeadIfContainer != null; }
            }

            ISortedItem ISortedItem.HeadForContainer
            {
                get { return HeadIfContainer; }
            }

            ISortedItem ISortedItem.ContainerForHead
            {
                get { return ContainerIfHead; }
            }

            IDependentItem ISortedItem.Item
            {
                get { return Item; }
            }

            IEnumerable<IDependentItemRef> ISortedItem.Requires 
            {
                get 
                {
                    var req = HeadIfContainer != null ? HeadIfContainer.Requires : null; 
                    return req == null ? ReadOnlyListEmpty<IDependentItemRef>.Empty : req.Where( d => !d.Optional ); 
                }
            }

            #endregion
        }

        class RankComputer
        {
            readonly Dictionary<object, object> _entries;
            readonly List<Entry> _result;
            readonly List<DependentItemIssue> _itemIssues;
            readonly Comparison<Entry> _comparer;
            readonly Options _options;
            List<Entry> _cycle;

            public RankComputer( IEnumerable<IDependentItem> items, IEnumerable<IDependentItemDiscoverer> discoverers, Options options )
            {
                _entries = new Dictionary<object, object>();
                _result = new List<Entry>();
                _itemIssues = new List<DependentItemIssue>();
                _options = options;
                if( _options.ReverseName ) _comparer = ReverseComparer;
                else _comparer =  NormalComparer;

                Registerer r = new Registerer( this );

                if( items != null ) r.Register( items );
                if( discoverers != null )
                {
                    foreach( var d in discoverers )
                    {
                        items = d.GetOtherItemsToRegister();
                        if( items != null ) r.Register( items );
                    }
                }
                Debug.Assert( _entries.Values.All( o => o is Entry ), "No more start values in dictionary once registered done." );
            }

            class Registerer
            {
                readonly Dictionary<object, object> _entries;
                readonly RankComputer _computer;
                readonly List<Entry> _namedContainersToBind;
                readonly List<Tuple<Entry, string>> _childrenToBind;
                readonly List<Entry> _specializedItems;

                public Registerer( RankComputer computer )
                {
                    _computer = computer;
                    _namedContainersToBind = new List<Entry>();
                    _childrenToBind = new List<Tuple<Entry, string>>();
                    _specializedItems = new List<Entry>();
                    _entries = _computer._entries;
                }

                public void Register( IEnumerable<IDependentItem> items )
                {
                    foreach( IDependentItem e in items )
                    {
                        RegisterEntry( e, null, null );
                    }
                    foreach( Entry nc in _namedContainersToBind )
                    {
                        Debug.Assert( nc.Item.Container != null && !(nc.Item.Container is IDependentItemContainer) );
                        // Container has been set by the first Container that claims to own the item in its Children collection.
                        if( nc.Container != null )
                        {
                            // If it is the good one, it is perfect (we avoided a lookup in the dictionary :-)).
                            // Else... it is a multiple containment.
                            if( nc.Container.FullName != nc.Item.Container.FullName )
                            {
                                _computer.SetStructureError( nc, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( nc.Item.Container.FullName );
                            }
                        }
                        else
                        {
                            // The entry has no associated container yet: we must find it by name.
                            Entry c;
                            if( TryGetEntryValue( nc.Item.Container.FullName, out c ) )
                            {
                                if( c.HeadIfContainer != null )
                                {
                                    // The named container exists and it is a Container.
                                    c.AddToContainer( nc );
                                }
                                else
                                {
                                    // The named item exists but is not a container.
                                    if( c.Item is IDependentItemContainer )
                                        _computer.SetStructureError( nc, DependentItemStructureError.ExistingContainerAskedToNotBeAContainer );
                                    else _computer.SetStructureError( nc, DependentItemStructureError.ExistingItemIsNotAContainer );
                                }
                            }
                            else
                            {
                                // The named container does not exist.
                                _computer.SetStructureError( nc, DependentItemStructureError.MissingNamedContainer );
                            }
                        }
                    }
                    foreach( var eC in _childrenToBind )
                    {
                        Debug.Assert( eC.Item1.HeadIfContainer != null, "The entry is a Container." );
                        Entry child;
                        if( TryGetEntryValue( eC.Item2, out child ) )
                        {
                            // Child found: is it already bound to a Container?
                            if( child.Container != null )
                            {
                                if( child.Container != eC.Item1 )
                                {
                                    // The child is bound to a container with another name: the late-bound name is an extraneous container.
                                    _computer.SetStructureError( child, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( eC.Item1.FullName );
                                }
                            }
                            else
                            {
                                // We set the container.
                                eC.Item1.AddToContainer( child );
                            }
                        }
                        else
                        {
                            // The child has not been registered.
                            _computer.SetStructureError( eC.Item1, DependentItemStructureError.MissingNamedChild ).AddMissingChild( eC.Item2 );
                        }
                    }
                    // Now that named containers and children have been handled, we can follow the Generalization chains
                    // to resolve all of them to their roots and apply Generalization's Container inheritance.
                    // The list contains only container and items, not head.
                    foreach( var sEntry in _specializedItems )
                    {
                        if( sEntry.Generalization == null ) ResolveGeneralization( sEntry );
                    }
                }

                void ResolveGeneralization( Entry sEntry )
                {
                    var s = sEntry.Item;
                    var g = s.Generalization;
                    Debug.Assert( g != null && sEntry.Generalization == null, "The entry is a specialization that does not know its Generalization yet." );
                    // Loop guard & default Generalization if not found by name.
                    sEntry.Generalization = Entry.GeneralizationMissingMarker;
                    Entry gEntry;
                    if( !TryGetEntryValue( g.FullName, out gEntry ) )
                    {
                        // Not found... If it is optional, act as if there
                        // were no generalization.
                        if( !g.Optional )
                        {
                            _computer.SetStructureError( sEntry, DependentItemStructureError.MissingGeneralization );
                        }
                    }
                    else
                    {
                        // Sets the Generalization object also on the head if 
                        // we are on a Container.
                        sEntry.Generalization = gEntry;
                        if( sEntry.HeadIfContainer != null ) sEntry.HeadIfContainer.Generalization = gEntry;

                        if( gEntry.Generalization == null && gEntry.Item.Generalization != null ) ResolveGeneralization( gEntry );
                        // The entry is bound to its Generalization. It is time to inherit Container.
                        if( sEntry.Container == null && gEntry.Container != null )
                        {
                            gEntry.Container.AddToContainer( sEntry );
                            // Checks (debug only).
                            Debug.Assert( sEntry.Container == gEntry.Container );
                            gEntry.Container.CheckContains( sEntry );
                        }
                    }
                }

                /// <summary>
                /// Preregistering allows an object's <see cref="IDependentItem.StartDependencySort"/> to be called after 
                /// its direct dependencies or its container's StartDependencySort.
                /// </summary>
                object PreRegisterObjectDependencies( IDependentItem e, bool memorizeStartValue )
                {
                    object entryOrStartValue;
                    if( !_entries.TryGetValue( e, out entryOrStartValue ) )
                    {
                        // Marks the entry with a null start value to handle cycles.
                        // Dependency cycles are simply ignored at this stage: they will 
                        // be detected and handled during the Process phasis.
                        _entries.Add( e, null );
                        // Container is a direct dependency.
                        IDependentItem container = e.Container as IDependentItem;
                        if( container != null ) PreRegisterObjectDependencies( container, true );
                        // Generalization is a direct dependency.
                        IDependentItem gen = e.Generalization as IDependentItem;
                        if( gen != null ) PreRegisterObjectDependencies( gen, true );
                        // Pre register direct requirements.
                        var req = e.Requires;
                        if( req != null )
                        {
                            foreach( var d in req )
                            {
                                IDependentItem di = d as IDependentItem;
                                if( di != null ) PreRegisterObjectDependencies( di, true ); 
                            }
                        }
                        // Gives the item an opportunity to prepare its data (mainly its FullName).
                        entryOrStartValue = e.StartDependencySort();
                        if( memorizeStartValue ) _entries[e] = entryOrStartValue;
                    }
                    return entryOrStartValue;
                }

                Entry RegisterEntry( IDependentItem e, Entry alreadyRegisteredContainer, Entry alreadyRegisteredChild )
                {
                    // Preregistering: collects Start values by calling StartDependencySort.
                    object startValue = PreRegisterObjectDependencies( e, false );
                    
                    Entry entry = startValue as Entry;
                    if( entry != null )
                    {
                        #region If the Entry exists, we only have to check container/item coherency (for a specific edge case).
                        Debug.Assert( entry.Item == e );
                        // We allow duplicated item instances in the original
                        // items enumeration (this also support our automatic 
                        // discovery for Container/Children).
                        if( alreadyRegisteredContainer != null )
                        {
                            // We are coming from the registration of our Children (below).
                            // Since this item is already registered and we skip the child from which we are 
                            // coming (alreadyRegisteredChild that is beeing processed - its Container is null), 
                            // the container must be the same.
                            if( entry.Container != alreadyRegisteredContainer )
                            {
                                // entry.Container can be null for 2 reasons: the item declares no container (null), 
                                // or the item declares a name and the entry has been added to namedContainersToBind.
                                Debug.Assert( entry.Container != null || (entry.Item.Container == null || (!(entry.Item.Container is IDependentItemContainer) && _namedContainersToBind.Contains( entry ))) );
                                // In both case, we set the entry.Container to the alreadyRegisteredContainer (the first container that claims to hold the item).
                                // When Item.Container is null, this is because we consider a null container as a "free" resource for a container.
                                // When the container is declared by name, we let the binding in Register handle the case.
                                if( entry.Container == null )
                                {
                                    alreadyRegisteredContainer.AddToContainer( entry );
                                }
                                else
                                {
                                    // This entry has a problem: it has more than one container that 
                                    // claim to own it.
                                    _computer.SetStructureError( entry, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( alreadyRegisteredContainer.FullName );
                                }
                            }
                        }
                        #endregion
                        return entry;
                    }
                
                    bool containerAskedToNotBeAContainer = false;
                    IDependentItemContainer c = e as IDependentItemContainer;
                    if( c != null )
                    {
                        IDependentItemContainerAsk ca = e as IDependentItemContainerAsk;
                        if( ca != null && ca.ThisIsNotAContainer )
                        {
                            containerAskedToNotBeAContainer = true;
                            c = null;
                        }
                    }

                    if( TryGetEntryValue( e.FullName, out entry ) )
                    {
                        #region FullName exists (Homonym or RequiredBy registration).
                        // The setupable item name is known, but
                        // is there an item already associated to this name?
                        if( entry.Item != null )
                        {
                            Debug.Assert( entry.Item != e );
                            // If 2 items share the same full name, this is a structure error.
                            _computer.SetStructureError( entry, DependentItemStructureError.Homonym ).AddHomonym( e );
                            // Registers the homonym item to mark the item as processed.
                            _entries[e] = entry;
                            return entry;
                        }
                        // Item name is knwon (the entry exists), but the entry is not bound to the item.
                        // We bind the entry to its item.
                        Debug.Assert( entry.Requires != null && entry.Requires.Count > 0, "Already created by a RequiredBy." );
                        Debug.Assert( _entries[e] == startValue || _entries[e] == null, "The element is associated to the StartValue or to null (pure marker of preregistration)." );

                        // Associates the element to its entry.
                        _entries[e] = entry;

                        if( c != null ) CreateOrTransformToContainerEntry( ref entry, c, startValue );
                        else
                        {
                            entry.Item = e;
                            entry.StartValue = startValue;
                        }
                        #endregion
                    }
                    else
                    {
                        #region The element nor its name has never been seen.
                        if( c != null ) CreateOrTransformToContainerEntry( ref entry, c, startValue );
                        else
                        {
                            entry = new Entry( e, startValue );
                            _entries[e] = entry;
                        }
                        _entries.Add( e.FullName, entry );
                        #endregion
                    }
                    // We now have the Entry associated to its IDependentItem:
                    // we register it in the _result list (if it has been previously created
                    // to handle RequiredBy, it has not been registered).
                    _computer._result.Add( entry );
                    Debug.Assert( entry.Item == e );

                    // Now that the element has been registered, we can handle Requires and RequiredBy if they exist...
                    var requires = e.Requires;
                    if( requires != null )
                    {
                        foreach( IDependentItemRef r in requires )
                        {
                            IDependentItem eR = r as IDependentItem;
                            if( eR != null ) RegisterEntry( eR, null, null );
                        }
                    }
                    var requiredBy = e.RequiredBy;
                    if( requiredBy != null )
                    {
                        foreach( IDependentItemRef reqBy in requiredBy )
                        {
                            Entry eReq = RegisterRequiredByDependency( reqBy );
                            eReq.AddRequiredByRequires( entry.Item.GetReference() );
                        }
                    }
                    // ...and Generalization...
                    var genRef = e.Generalization;
                    if( genRef != null )
                    {
                        // If it is an object (not a named reference), registers it
                        // but do not catch the resulting entry in entry.Generalization
                        // since it has yet to be fully resolved. 
                        // This is done after Container/Child binding.
                        IDependentItem g = e.Generalization as IDependentItem;
                        if( g != null ) RegisterEntry( g, null, null );
                        // SpecializedItems contains items and container (but no heads).
                        _specializedItems.Add( entry );
                    }

                    // ...and safely automatically discover its bound items: its container and its children.
                    //
                    // Starts with its container :
                    // - first handle the case where we are called by a Container that claims to own the current element.
                    if( alreadyRegisteredContainer != null )
                    {
                        // We are coming from our container.
                        // If the item has a container, they must match.
                        if( e.Container != null )
                        {
                            // The current element has a container.
                            IDependentItemContainer father = e.Container as IDependentItemContainer;
                            if( alreadyRegisteredContainer.Item != father )
                            {
                                if( father != null )
                                {
                                    // The container differs from the one that contains it as a child.
                                    // Registers the container associated to the current element...
                                    Entry extraContainer = RegisterEntry( father, null, entry );
                                    // ...and declares an error.
                                    // (Here we forget the fact that the extra container may be a IDependentItemContainerAsk where ThisIsNotAContainer returned false:
                                    // we consider the container mismatch as more important.)
                                    _computer.SetStructureError( entry, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( extraContainer.FullName );
                                }
                                else
                                {
                                    // The container is a name.
                                    var strong = e.Container.GetReference();
                                    if( strong.FullName != alreadyRegisteredContainer.FullName )
                                    {
                                        // If it differs, declares an error.
                                        _computer.SetStructureError( entry, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( strong.FullName );
                                    }
                                }
                            }
                        }
                        // Even if a structure error occured, we set the container.
                        alreadyRegisteredContainer.AddToContainer( entry );
                    }
                    // - else, the current element is not a child (at least for this RegisterEntry call: we are not coming from a container that claims to own it).
                    else
                    {
                        // If it declares a container, we try to bind to it.
                        if( e.Container != null )
                        {
                            IDependentItemContainer father = e.Container as IDependentItemContainer;
                            if( father != null )
                            {
                                var cnt = RegisterEntry( father, null, entry );
                                if( cnt.HeadIfContainer == null )
                                {
                                    // The container refused to be a container.
                                    _computer.SetStructureError( entry, DependentItemStructureError.ExistingContainerAskedToNotBeAContainer );
                                }
                                else
                                {
                                    cnt.AddToContainer( entry );
                                }
                            }
                            else _namedContainersToBind.Add( entry );
                        }
                    }
                    // If it is a container, handle its children. 
                    // If not, check if it is a "refusing" container that nevertheless exposes children: this must be 
                    // considered as an error to stay independant of the registration order.
                    if( c == null )
                    {
                        if( containerAskedToNotBeAContainer )
                        {
                            Debug.Assert( c == null && e is IDependentItemContainer );
                            var children = ((IDependentItemContainer)e).Children;
                            if( children != null && children.Any() )
                            {
                                _computer.SetStructureError( entry, DependentItemStructureError.ContainerAskedToNotBeAContainerButContainsChildren );
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert( entry.HeadIfContainer != null );
                        IEnumerable<IDependentItemRef> children;
                        if( (children = c.Children) != null )
                        {
                            IDependentItem knownChild = alreadyRegisteredChild != null ? alreadyRegisteredChild.Item : null;
                            foreach( IDependentItemRef childRef in children )
                            {
                                // Skips null by security.
                                if( childRef == null ) continue;

                                IDependentItem child = childRef as IDependentItem;
                                if( child != null )
                                {
                                    if( child != knownChild ) RegisterEntry( child, entry, null );
                                }
                                else
                                {
                                    // If the child is a named reference to the exact child that 
                                    // is calling us, we have nothing to do.
                                    var strong = childRef.GetReference();
                                    if( knownChild == null || strong.FullName != knownChild.FullName )
                                    {
                                        // The child must be late-bound.
                                        _childrenToBind.Add( new Tuple<Entry, string>( entry, strong.FullName ) );
                                    }
                                }
                            }
                        }
                    }
                    // The discoverer aspect of the item: its related elements are registered.
                    IDependentItemDiscoverer disco = e as IDependentItemDiscoverer;
                    if( disco != null )
                    {
                        var related = disco.GetOtherItemsToRegister();
                        if( related != null )
                        {
                            foreach( IDependentItem eR in related )
                            {
                                RegisterEntry( eR, null, null );
                            }
                        }
                    }
                    return entry;
                }

                Entry RegisterRequiredByDependency( IDependentItemRef r )
                {
                    IDependentItem e = r as IDependentItem;
                    Entry entry;
                    if( e != null )
                    {
                        entry = RegisterEntry( e, null, null );
                    }
                    else
                    {
                        var strong = r.GetReference();
                        if( !TryGetEntryValue( strong.FullName, out entry ) )
                        {
                            // When an entry is created only to handle
                            // RequiredBy, we do not add it in _result.
                            entry = new Entry( strong.FullName );
                            _entries.Add( strong.FullName, entry );
                        }
                    }
                    return entry;
                }

                void CreateOrTransformToContainerEntry( ref Entry entry, IDependentItemContainer c, object startValue )
                {
                    if( entry == null )
                    {
                        entry = new Entry( c, startValue );
                        _entries[c] = entry;
                    }
                    else
                    {
                        entry.Item = c;
                        entry.StartValue = startValue;
                    }
                    entry.TransformToContainer();
                    _entries.Add( entry.HeadIfContainer.FullName, entry.HeadIfContainer );
                    _computer._result.Add( entry.HeadIfContainer );
                }

                /// <summary>
                /// Adapts the entries that is Dictionary(object,object) to behave like a Dictionary(object,Entry)
                /// by ignoring start values (that can not be an instance of the private Entry).
                /// </summary>
                bool TryGetEntryValue( object key, out Entry e )
                {
                    e = null;
                    object entryOrStartValue;
                    if( _entries.TryGetValue( key, out entryOrStartValue ) )
                    {
                        e = entryOrStartValue as Entry;
                    }
                    return e != null;
                }            
            }

            DependentItemIssue SetStructureError( Entry nc, DependentItemStructureError status )
            {
                DependentItemIssue issues = _itemIssues.Where( m => m.Item == nc.Item ).FirstOrDefault();
                if( issues == null ) _itemIssues.Add( (issues = new DependentItemIssue( nc.Item, status )) );
                else issues.StructureError |= status;
                return issues;
            }

            #region Processing: Rank computing & Cycle detection.

            public void Process()
            {
                // Note: Since we can NOT support dynamic resolution of a missing dependency
                // (through a function like ResolveMissing( fullName ) because of
                // the RequiredBy: if a newly added item has a RequiredBy, we should
                // reprocess all the items since it may change -a lot of- already computed rank),
                // the _result list does not change during process: we can safely foreach on it.
                foreach( var e in _result )
                {
                    if( e.Rank == -1 )
                    {
                        ComputeRank( e );
                        if( _cycle != null )
                        {
                            Debug.Assert( _cycle.Count >= 2, "We started the cycle construction with the 2 first participants." );
                            // Instead of removing head2, we let it in the cycle: this is useless but much more
                            // explicit for users: instead of a=>b=>c, they can see a=>b=>c=>a and a cycle
                            // has necessarily a length of at least 2. Autoreferences are cycles like a=>a.
                            IDependentItem head = _cycle[0].Item;
                            int head2 = _cycle.FindIndex( 1, c => c.Item == head ) + 1;
                            Debug.Assert( head2 >= 2, "We necessarily added the final culprit." );
                            int nbToRemove = _cycle.Count - head2;
                            if( nbToRemove > 0 ) _cycle.RemoveRange( head2, nbToRemove );
                            break;
                        }
                    }
                }
            }

            void ComputeRank( Entry e )
            {
                Debug.Assert( e.Rank == -1 );
                e.Rank = -2;
                int rank = 0;

                List<IDependentItemRef> requiresHiddenByContainerOrGen = null;
                
                // Prepares eGen.
                Entry eGen = e.Generalization;
                if( eGen == Entry.GeneralizationMissingMarker ) eGen = null;

                // Starts with reverse requirements (RequiredBy) since during the registeration phasis,
                // the Requires HashSet has been populated only with RequiredBy from others.
                //
                // Do this for Containers and Items (but not for Heads)
                //
                if( e.ContainerIfHead == null )
                {
                    // Reverse requirements (RequiredBy) can be ignored for Head since "Required By" 
                    // for a Container must apply to each and every items contained in a container.
                    var requirements = e.Requires;
                    if( requirements != null )
                    {
                        Debug.Assert( e.Requires.Count > 0 );
                        foreach( var dep in requirements )
                        {
                            Debug.Assert( _entries.ContainsKey( dep.FullName ) && ((Entry)_entries[dep.FullName]).Item != null, "Since the requirement has been added by an item, it exists." );
                            Entry oeDep = (Entry)_entries[dep.FullName];
                            if( (oeDep == eGen)
                                ||
                                (oeDep.HeadIfContainer != null && _options.SkipDependencyToContainer && e.AppearInContainerChain( oeDep ) ) )
                            {
                                if( requiresHiddenByContainerOrGen == null ) requiresHiddenByContainerOrGen = new List<IDependentItemRef>();
                                requiresHiddenByContainerOrGen.Add( dep );
                            }
                            else
                            {
                                HandleDependency( ref rank, e, oeDep, false );
                                if( _cycle != null ) return;
                            }
                        }
                    }
                }

                // Handle direct requirements: this can be ignored for Containers: the Head handles them.
                //
                // Do this for Heads and Items (but not for Containers).
                //
                if( e.HeadIfContainer == null )
                {
                    // We first handle Generalization as a requirement.
                    if( eGen != null )
                    {
                        HandleDependency( ref rank, e, eGen, false );
                        if( _cycle != null ) return;
                    }
                    var requirements = e.Item.Requires;
                    if( requirements != null )
                    {
                        if( e.Requires == null ) e.Requires = new HashSet<IDependentItemRef>();
                        foreach( IDependentItemRef dep in requirements )
                        {
                            // Security: skips any null entry and Generalization if it exists.
                            if( dep == null || (eGen != null && (eGen == dep || eGen.FullName == dep.FullName)) ) continue;

                            IDependentItemRef strong = dep.GetReference();
                            Debug.Assert( ReferenceEquals( dep, strong ) == !dep.Optional );

                            // Adds the dependency to the HashSet: if it already exists, this requirement
                            // has already been handled either through a RequiredBy or by a previous
                            // duplicate in this requirements.
                            // The trick here is to first consider the non-optional dependency name (dep that does not start with ?).
                            // If the strong requirement has been processed, it is useless to process it again, be it a strong or 
                            // an optional requirement.
                            // Second, we add the raw dependency (can be optional or strong): if the exact form has already been added,
                            // it is useless to continue.
                            //  
                            // ?Dep =>  We add "?Dep" to the Requires and 
                            //          - If the dependency exists, it is fullfilled, we also add the "Dep":
                            //            if "Dep" occurs later, nothing is done.
                            //          - If the dependency is missing, we add "?Dep" to the missing, but we can not 
                            //            consider that "Dep" has been processed since, if it occurs later, we must be 
                            //            able to register it as a "strong" missing, overriding the first optional one.
                            // Dep =>   We add "Dep" to the Requires and
                            //          - If the dependency exists, we do not add "?Dep" since
                            //            if "?Dep" occurs later, it will first test for "Dep" (useless to pollute the Requires).
                            //          - If the dependency is missing, we add the "?Dep" form to the Requires: this 
                            //            avoids us to process "?Dep" and have the (considered positive) side effect of hiding 
                            //            the optionnal requirement in the final Missing collector.
                            if( !e.Requires.Contains( strong ) && e.Requires.Add( dep ) )
                            {
                                object oEntry;
                                Entry oeDep;
                                if( !_entries.TryGetValue( strong.FullName, out oEntry ) || (oeDep = (Entry)oEntry).Item == null )
                                {
                                    SetStructureError( e, dep.Optional ? DependentItemStructureError.None : DependentItemStructureError.MissingDependency ).AddMissing( dep );
                                    if( !dep.Optional ) e.Requires.Add( dep.GetOptionalReference() );
                                }
                                else
                                {
                                    // Adds the strong dependency to mark it as processed even if it will be removed by skipDependencyToContainer.
                                    if( dep.Optional ) e.Requires.Add( strong );
                                    if( oeDep.HeadIfContainer != null
                                        && _options.SkipDependencyToContainer
                                        && e.AppearInContainerChain( oeDep ) )
                                    {
                                        if( requiresHiddenByContainerOrGen == null ) requiresHiddenByContainerOrGen = new List<IDependentItemRef>();
                                        requiresHiddenByContainerOrGen.Add( dep );
                                    }
                                    else
                                    {
                                        HandleDependency( ref rank, e, oeDep, false );
                                        if( _cycle != null ) return;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //
                    // Container only.
                    //
                    // Handles Container => Head (useful for empty containers).
                    Debug.Assert( e.HeadIfContainer != null );
                    HandleDependency( ref rank, e, e.HeadIfContainer, true );
                    if( _cycle != null ) return;
                }

                // Handles the element's Container: its head is required by this item (be it a head, a container or an item).
                if( e.Container != null )
                {
                    // Checks (Debug only) that an element that claims to belong to a Container
                    // is actually in the linked list of its Container's items.
                    e.Container.CheckContains( e );
                   
                    HandleDependency( ref rank, e, e.Container.HeadIfContainer, false );
                    if( _cycle != null ) return;
                }
                // Handles children if any.
                Entry child = e.FirstChildIfContainer;
                while( child != null )
                {
                    HandleDependency( ref rank, e, child, false );
                    if( _cycle != null ) return;
                    child = child.NextChildInContainer;
                }
                // Should the entry.Requires HashSet be cleaned up?
                if( requiresHiddenByContainerOrGen != null )
                {
                    Debug.Assert( requiresHiddenByContainerOrGen.Count > 0 );
                    Debug.Assert( e.Requires != null );
                    foreach( var hiddenDep in requiresHiddenByContainerOrGen )
                    {
                        e.Requires.Remove( hiddenDep );
                    }
                }
                // The rank of the item is known now.
                e.Rank = rank + 1;
            }

            void HandleDependency( ref int rank, Entry e, Entry oeDep, bool isContainerHeadDependency )
            {
                // Cycle detection.
                if( oeDep.Rank == -2 )
                {
                    _cycle = new List<Entry>() { oeDep, e };
                    return;
                }
                if( oeDep.Rank == -1 )
                {
                    ComputeRank( oeDep );
                    // Do not add "Container" => "Head". It is useless and hard 
                    // to understand since the name is the same ("X" => "X").
                    // We do not test that oeDep is the head of the item (either by its name
                    // or by changing the parameter to the entry) in order to not 
                    // mask auto-dependency.
                    if( _cycle != null && !isContainerHeadDependency ) _cycle.Add( e );
                }
                rank = Math.Max( rank, oeDep.Rank );
            }

            public DependencySorterResult GetResult()
            {
                if( _cycle == null )
                {
                    _result.Sort( _comparer );
                    int i = 0;
                    foreach( var e in _result ) e.Index = i++;
                    return new DependencySorterResult( _result, null, _itemIssues );
                }
                _cycle.Reverse();
                return new DependencySorterResult( null, _cycle, null );
            }

            static int NormalComparer( Entry o1, Entry o2 )
            {
                int cmp = o1.Rank - o2.Rank;
                if( cmp == 0 ) cmp = o1.FullName.CompareTo( o2.FullName );
                return cmp;
            }

            static int ReverseComparer( Entry o1, Entry o2 )
            {
                int cmp = o1.Rank - o2.Rank;
                if( cmp == 0 ) cmp = o2.FullName.CompareTo( o1.FullName );
                return cmp;
            }

            #endregion
        }

    }
}
