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
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <param name="discoverers">An optional set of <see cref="IDependentItemDiscoverer"/> (can be null).</param>
        /// <param name="reverseName">True to reverse lexicographic order for items.</param>
        /// <returns>A <see cref="DependencySorterResult"/>.</returns>
        public static DependencySorterResult OrderItems( IEnumerable<IDependentItem> items, IEnumerable<IDependentItemDiscoverer> discoverers, bool reverseName = false )
        {
            var computer = new RankComputer( items, discoverers, reverseName );
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
            return OrderItems( items, null, false );
        }

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="reverseName">True to reverse lexicographic order for items.</param>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <returns>A <see cref="DependencySorterResult"/>.</returns>
        public static DependencySorterResult OrderItems( bool reverseName, params IDependentItem[] items )
        {
            return OrderItems( items, null, reverseName );
        }

        internal class Entry : ISortedItem
        {
            public Entry( IDependentItem e, string fullName )
                : this( fullName )
            {
                Item = e;
            }
            public Entry( string fullName )
            {
                FullName = fullName;
                Rank = -1;
            }

            public readonly string FullName;
            public IDependentItem Item;
            public HashSet<string> Requires;
            public int Rank;
            // Index is computed at the end of the process.
            public int Index;
            // For heads, it is the container associated to the head.
            // Use Container.Container for heads.
            public Entry Container;
            // The HeadIfContainer is null for normal items and heads.
            // It is not null only for container.
            public Entry ContainerIfHead;
            public Entry HeadIfContainer;
            public Entry FirstChildIfContainer;
            public Entry NextChildInContainer;

            internal void AddRequiredByRequires( string fullName )
            {
                if( Requires == null ) Requires = new HashSet<string>();
                Requires.Add( fullName );
            }

            internal void AddToContainer( Entry child )
            {
                Debug.Assert( child.Container == null, "Already existing Container must have been handled before." );
                child.Container = this;
                if( child.HeadIfContainer != null ) child.HeadIfContainer.Container = this;
                child.NextChildInContainer = FirstChildIfContainer;
                FirstChildIfContainer = child;
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

            ISortedItem ISortedItem.Container
            {
                get { return Container; }
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

            IEnumerable<string> ISortedItem.Requires 
            {
                get { return Requires == null ? (IEnumerable<string>)Util.EmptyStringArray : Requires.Where( d => d[0] != '?' ); }
            }

            #endregion
        }

        class RankComputer
        {

            Dictionary<string, Entry> _byName;
            List<Entry> _result;
            List<DependentItemIssue> _itemIssues;
            List<IDependentItem> _cycle;
            Comparison<Entry> _comparer;

            public RankComputer( IEnumerable<IDependentItem> items, IEnumerable<IDependentItemDiscoverer> discoverers, bool reverseName )
            {
                _byName = new Dictionary<string, Entry>( StringComparer.OrdinalIgnoreCase );
                _result = new List<Entry>();
                _itemIssues = new List<DependentItemIssue>();
                if( reverseName ) _comparer = ReverseComparer;
                else _comparer =  NormalComparer;

                if( items != null ) Register( items );
                if( discoverers != null )
                {
                    foreach( var d in discoverers )
                    {
                        items = d.GetOtherItemsToRegister();
                        if( items != null ) Register( items );
                    }
                }
            }

            void Register( IEnumerable<IDependentItem> items )
            {
                var namedContainersToBind = new List<Entry>();
                var childrenToBind = new List<Tuple<Entry, string>>(); 
                foreach( IDependentItem e in items )
                {
                    RegisterEntry( e, null, null, namedContainersToBind, childrenToBind );
                }
                foreach( Entry nc in namedContainersToBind )
                {
                    Debug.Assert( nc.Item.Container != null && !(nc.Item.Container is IDependentItemContainer) );
                    // Container has been set by the first container that claims to own the item in its Children collection.
                    if( nc.Container != null )
                    {
                        // If it is the good one, it is perfect (we avoided a lookup in the dictionary :-)).
                        // Else... it is a multiple containment.
                        if( nc.Container.FullName != nc.Item.Container.FullName )
                        {
                            SetStructureError( nc, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( nc.Item.Container.FullName );
                        }
                    }
                    else
                    {
                        Entry c;
                        if( _byName.TryGetValue( nc.Item.Container.FullName, out c ) )
                        {
                            if( c.HeadIfContainer != null )
                            {
                                // The named container exists and it is a Container.
                                c.AddToContainer( nc );
                            }
                            else
                            {
                                // The named item exists but is not a container.
                                SetStructureError( nc, DependentItemStructureError.ExistingItemIsNotAContainer );
                            }
                        }
                        else
                        {
                            // The named container does not exist.
                            SetStructureError( nc, DependentItemStructureError.MissingNamedContainer );
                        }
                    }
                }
                foreach( var eC in childrenToBind )
                {
                    Debug.Assert( eC.Item1.HeadIfContainer != null, "The entry is a Container." );
                    Entry child;
                    if( _byName.TryGetValue( eC.Item2, out child ) )
                    {
                        // Child found: is it already bound to a Container?
                        if( child.Container != null )
                        {
                            if( child.Container != eC.Item1 )
                            {
                                // The child is bound to a container with another name: the late-bound name is an extraneous container.
                                SetStructureError( child, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( eC.Item1.FullName );
                            }
                        }
                        else
                        {
                            // The container must be set.
                            eC.Item1.AddToContainer( child );
                        }
                    }
                    else
                    {
                        // The child has not been registered.
                        SetStructureError( eC.Item1, DependentItemStructureError.MissingNamedChild ).AddMissingChild( eC.Item2 );
                    }
                }
            }

            private Entry RegisterEntry( IDependentItem e, Entry alreadyRegisteredContainer, Entry alreadyRegisteredChild, List<Entry> namedContainersToBind, List<Tuple<Entry,string>> childrenToBind )
            {
                IDependentItemContainer c = e as IDependentItemContainer;
                Entry entry;
                if( _byName.TryGetValue( e.FullName, out entry ) )
                {
                    if( entry.Item != null )
                    {
                        // We allow duplicated item instances in the original
                        // items enumeration (this also support our automatic 
                        // discovery for Container/Children).
                        if( entry.Item == e )
                        {
                            if( alreadyRegisteredContainer != null )
                            {
                                // We are coming from the registration of the Children below.
                                // Since this item is already registered and we skip the child from which we are 
                                // coming (alreadyRegisteredChild that is beeing processed - its Container is null), 
                                // the container must be the same.
                                if( entry.Container != alreadyRegisteredContainer )
                                {
                                    // entry.Container can be null for 2 reasons: the item declares no container (null), 
                                    // or the item declares a name and the entry has been added to namedContainersToBind.
                                    Debug.Assert( entry.Container != null || (entry.Item.Container == null || (!(entry.Item.Container is IDependentItemContainer) && namedContainersToBind.Contains( entry ))) );
                                    // In both case, we set the entry.Container to the alreadyRegisteredContainer (the first container that claims to hold the item).
                                    // When Item.Container is null, this is because we consider a null container as a "free" resource for a container.
                                    // When the container is declared by name, we let the binding in Register handle the case.
                                    if( entry.Container == null )
                                    {
                                        entry.Container = alreadyRegisteredContainer;
                                    }
                                    else
                                    {
                                        // This entry has a problem: it has more than one container that 
                                        // claim to own it.
                                        SetStructureError( entry, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( alreadyRegisteredContainer.FullName );
                                    }
                                }
                            }
                        }
                        else
                        {
                            // But if 2 items share the same full name, this is a structure error.
                            SetStructureError( entry, DependentItemStructureError.Homonym ).AddHomonym( e );
                        }
                        return entry;
                    }
                    Debug.Assert( entry.Requires != null && entry.Requires.Count > 0, "Already created by a RequiredBy." );

                    if( c != null ) CreateContainerEntry( ref entry, c );
                    else entry.Item = e;
                }
                else
                {
                    if( c != null ) CreateContainerEntry( ref entry, c );
                    else entry = new Entry( e, e.FullName );
                    _byName.Add( e.FullName, entry );
                }
                // Here we did create the OrderedItem from the IDependentItem:
                // we register it in the _result list (if it has been previously created
                // to handle RequiredBy, it has not been registered).
                _result.Add( entry );
                // Now that the element has been registered, we can handle RequiredBy if they exist...
                var requiredBy = e.RequiredBy;
                if( requiredBy != null )
                {
                    foreach( string reqBy in requiredBy )
                    {
                        string clean = RemoveOptional( reqBy, entry, "RequiredBy" );
                        Entry eReq = FindOrCreateEntry( clean );
                        eReq.AddRequiredByRequires( entry.FullName );
                    }
                }
                // ...and safely automatically discover its bound items: its container (if not by name) and its children.
                if( alreadyRegisteredContainer != null )
                {
                    if( entry.Item.Container != null && entry.Item.Container.FullName != alreadyRegisteredContainer.FullName )
                    {
                        SetStructureError( entry, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( entry.Item.Container.FullName );
                    }
                    entry.Container = alreadyRegisteredContainer;
                }
                else
                {
                    if( e.Container != null )
                    {
                        IDependentItemContainer father = e.Container as IDependentItemContainer;
                        if( father != null )
                        {
                            entry.Container = RegisterEntry( father, null, entry, namedContainersToBind, childrenToBind );
                        }
                        else namedContainersToBind.Add( entry );
                    }
                }
                // If it is a container, handle its children.
                if( c != null )
                {
                    Debug.Assert( entry.HeadIfContainer != null );
                    entry.HeadIfContainer.Container = entry.Container;
                    if( c.Children != null )
                    {
                        IDependentItem knownChild = alreadyRegisteredChild != null ? alreadyRegisteredChild.Item : null;
                        Entry lastChild = null;
                        foreach( IDependentItemRef childRef in c.Children )
                        {
                            if( childRef != null )
                            {
                                IDependentItem child = childRef as IDependentItem;
                                if( child != null )
                                {
                                    Entry eC;
                                    if( child != knownChild ) eC = RegisterEntry( child, entry, null, namedContainersToBind, childrenToBind );
                                    else eC = alreadyRegisteredChild;
                                    if( lastChild == null ) entry.FirstChildIfContainer = eC;
                                    else lastChild.NextChildInContainer = eC;
                                    lastChild = eC;
                                }
                                else
                                {
                                    if( knownChild != null && childRef.FullName == knownChild.FullName )
                                    {
                                        if( lastChild == null ) entry.FirstChildIfContainer = alreadyRegisteredChild;
                                        else lastChild.NextChildInContainer = alreadyRegisteredChild;
                                        lastChild = alreadyRegisteredChild;
                                    }
                                    else
                                    {
                                        // The child must be late-bound.
                                        childrenToBind.Add( new Tuple<Entry, string>( entry, childRef.FullName ) );
                                    }
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
                            RegisterEntry( eR, null, null, namedContainersToBind, childrenToBind );
                        }
                    }
                }
                return entry;
            }

            private DependentItemIssue SetStructureError( Entry nc, DependentItemStructureError status )
            {
                DependentItemIssue issues = _itemIssues.Where( m => m.Item == nc.Item ).FirstOrDefault();
                if( issues == null ) _itemIssues.Add( (issues = new DependentItemIssue( nc.Item, status )) );
                else issues.StructureError |= status;
                return issues;
            }

            private Entry FindOrCreateEntry( string fullName )
            {
                Entry entry;
                if( !_byName.TryGetValue( fullName, out entry ) )
                {
                    // When an entry is created only to handle
                    // RequiredBy, we do not add it in _result.
                    entry = new Entry( fullName );
                    _byName.Add( fullName, entry );
                }
                return entry;
            }

            private void CreateContainerEntry( ref Entry entry, IDependentItemContainer c )
            {
                if( entry == null ) entry = new Entry( c, c.FullName );
                else entry.Item = c;
                string fullName = c.FullName + ".Head";
                Entry head = new Entry( c, fullName );
                entry.HeadIfContainer = head;
                head.ContainerIfHead = entry;
                _byName.Add( fullName, head );
                _result.Add( head );
            }

            public void Process()
            {
                // Note: Since we can NOT support dynamic resolution of a missing dependency
                // (through a function like ResolveMissing( fullName ) because of
                // the RequiredBy: if a newly added item has a RequiredBy, we should
                // reprocess all the items since it may change (a lot of) already computed rank),
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
                            // has necessarily a length of at least 2. Autoreference are cycles like a=>a.
                            int head2 = _cycle.IndexOf( _cycle[0], 1 ) + 1;
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
                IDependentItem item = e.Item;
                // Starts with reverse requirements (RequiredBy) since during the registeration phasis,
                // the Requires HashSet has been populated only with RequiredBy from others.
                if( e.ContainerIfHead == null )
                {
                    // Reverse requirements (RequiredBy) can be ignored for Head since "Required By" 
                    // for a Container must apply to each and every items contained in a container.
                    var requirements = e.Requires;
                    if( requirements != null )
                    {
                        // Do this for Container and Item (but not for Head).
                        Debug.Assert( e.Requires.Count > 0 );
                        foreach( string dep in requirements )
                        {
                            Debug.Assert( _byName.ContainsKey( dep ) && _byName[dep].Item != null, "Since the requirement has been added by an item, it exists." );
                            HandleDependency( ref rank, item, _byName[dep], false );
                            if( _cycle != null ) return;
                        }
                    }
                }
                // Handle direct requirements: this can be ignored for Containers: the Head handles them.
                if( e.HeadIfContainer == null )
                {
                    // Do this for Heads and Items (but not for Container).
                    var requirements = item.Requires;
                    if( requirements != null )
                    {
                        if( e.Requires == null ) e.Requires = new HashSet<string>();
                        foreach( string dep in requirements )
                        {
                            string clean = RemoveOptional( dep, e, "Requires" );
                            // Adds the string to the HashSet: if it already exists, this requirement
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
                            if( !e.Requires.Contains( clean ) && e.Requires.Add( dep ) )
                            {
                                bool isStrong = ReferenceEquals( dep, clean );
                                Entry oeDep;
                                if( !_byName.TryGetValue( clean, out oeDep ) || oeDep.Item == null )
                                {
                                    SetStructureError( e, isStrong ? DependentItemStructureError.MissingDependency : DependentItemStructureError.None ).AddMissing( dep, isStrong );
                                    if( isStrong ) e.Requires.Add( '?' + dep );
                                }
                                else
                                {
                                    if( !isStrong ) e.Requires.Add( clean );
                                    HandleDependency( ref rank, item, oeDep, false );
                                    if( _cycle != null ) return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Handles Container => Head (useful for empty containers).
                    Debug.Assert( e.HeadIfContainer != null );
                    HandleDependency( ref rank, item, e.HeadIfContainer, true );
                    if( _cycle != null ) return;
                }
                // Handles the Container: its head is required by this item (be it a head, a container or an item).
                if( e.Container != null )
                {
                    Debug.Assert( e.Container.HeadIfContainer != null );
                    HandleDependency( ref rank, item, e.Container.HeadIfContainer, false );
                    if( _cycle != null ) return;
                }
                // Handles children if any.
                Entry child = e.FirstChildIfContainer;
                while( child != null )
                {
                    HandleDependency( ref rank, item, child, false );
                    if( _cycle != null ) return;
                    child = child.NextChildInContainer;
                }
                e.Rank = rank + 1;
            }

            void HandleDependency( ref int rank, IDependentItem item, Entry oeDep, bool isContainerHeadDependency )
            {
                // Cycle detection.
                if( oeDep.Rank == -2 )
                {
                    _cycle = new List<IDependentItem>() { oeDep.Item, item };
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
                    if( _cycle != null && !isContainerHeadDependency ) _cycle.Add( item );
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

            static string RemoveOptional( string dep, Entry holder, string source )
            {
                if( dep != null && dep.Length > 0 && dep[0] == '?' ) dep = dep.Substring( 1 );
                if( String.IsNullOrWhiteSpace( dep ) )
                {
                    throw new InvalidOperationException( String.Format( "Invalid empty {1} on '{0}'.", holder.FullName, source ) );
                }
                return dep;
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

        }

    }
}
