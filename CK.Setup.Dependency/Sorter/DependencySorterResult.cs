using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Encapsulates the result of the <see cref="DependencySorter.OrderItems"/> method.
    /// </summary>
    public sealed class DependencySorterResult
    {
        readonly IReadOnlyList<ISortedItem> _cycle;
        IReadOnlyList<CycleExplainedElement> _cycleExplained;
        int _itemIssueWithStructureErrorCount;
        bool _requiredMissingIsError;

        internal DependencySorterResult( List<DependencySorter.Entry> result, List<DependencySorter.Entry> cycle, List<DependentItemIssue> itemIssues )
        {
            Debug.Assert( (result == null) != (cycle == null), "cycle ^ result" );
            if( result == null )
            {
                SortedItems = null;
                _cycle = new ReadOnlyListOnIList<DependencySorter.Entry>( cycle );
                CycleDetected = cycle.ToReadOnlyList( e => e.Item );
            }
            else
            {
                SortedItems = new ReadOnlyListOnIList<DependencySorter.Entry>( result );
                _cycle = null;
                CycleDetected = null;
            }
            ItemIssues = itemIssues != null && itemIssues.Count > 0 ? new ReadOnlyListOnIList<DependentItemIssue>( itemIssues ) : ReadOnlyListEmpty<DependentItemIssue>.Empty;
            _requiredMissingIsError = true;
            _itemIssueWithStructureErrorCount = -1;
        }

        /// <summary>
        /// Non null if a cycle has been detected.
        /// </summary>
        public readonly IReadOnlyList<IDependentItem> CycleDetected;
        
        /// <summary>
        /// Gets the list of <see cref="ISortedItem"/>: null if <see cref="CycleDetected"/> is not null.
        /// </summary>
        public readonly IReadOnlyList<ISortedItem> SortedItems;

        /// <summary>
        /// List of <see cref="DependentItemIssue"/>. Never null.
        /// </summary>
        public readonly IReadOnlyList<DependentItemIssue> ItemIssues;

        /// <summary>
        /// Gets or sets whether any non optional missing requirement or generalization is a structure error (<see cref="HasStructureError"/> 
        /// becomes true).
        /// </summary>
        public bool ConsiderRequiredMissingAsStructureError
        {
            get { return _requiredMissingIsError; }
            set 
            {
                if( _requiredMissingIsError != value )
                {
                    _itemIssueWithStructureErrorCount = -1;
                    _requiredMissingIsError = value;
                }
            }
        }

        /// <summary>
        /// True if at least one non-optional requirement or generalization (a requirement that is not prefixed with '?' when expressed as a string) exists.
        /// (If both this and <see cref="ConsiderRequiredMissingAsStructureError"/> are true then <see cref="HasStructureError"/> is also true 
        /// since a missing dependency is flagged with <see cref="DependentItemStructureError.MissingDependency"/>.)
        /// </summary>
        public bool HasRequiredMissing
        {
            get 
            {
                Debug.Assert( (!ConsiderRequiredMissingAsStructureError || !ItemIssues.Any( m => m.RequiredMissingCount > 0 )) || HasStructureError, "MissingIsError && Exist(Missing) => HasStructureError" );
                return ItemIssues.Any( m => m.RequiredMissingCount > 0 ); 
            }
        }

        /// <summary>
        /// True if at least one relation between an item and its container is invalid (true when <see cref="HasRequiredMissing"/> is 
        /// true if <see cref="ConsiderRequiredMissingAsStructureError"/> is true).
        /// </summary>
        public bool HasStructureError
        {
            get { return StructureErrorCount > 0; }
        }

        /// <summary>
        /// Number of items that have at least one invalid relation between itself and its container, its children, its generalization or its dependencies.
        /// </summary>
        public int StructureErrorCount
        {
            get 
            {
                if( _itemIssueWithStructureErrorCount < 0 )
                {
                    if( _requiredMissingIsError )
                    {
                        _itemIssueWithStructureErrorCount = ItemIssues.Count( m => m.StructureError != DependentItemStructureError.None );
                    }
                    else
                    {
                        _itemIssueWithStructureErrorCount = ItemIssues.Count( m => (m.StructureError != DependentItemStructureError.None 
                            && m.StructureError != DependentItemStructureError.MissingDependency
                            && m.StructureError != DependentItemStructureError.MissingGeneralization) );
                    }
                }
                return _itemIssueWithStructureErrorCount;
            }
        }

        /// <summary>
        /// True only if no cycle has been detected, and no structure error (<see cref="HasStructureError"/>) 
        /// exist: <see cref="SortedItems"/> can be exploited.
        /// When IsComplete is false, <see cref="LogError"/> can be used to have a dump of the errors in a <see cref="IActivityLogger"/>.
        /// </summary>
        public bool IsComplete
        {
            get { return CycleDetected == null && HasStructureError == false; }
        }

        /// <summary>
        /// Gets a list of <see cref="CycleExplainedElement"/>. Null if <see cref="CycleDetected"/> is null.
        /// </summary>
        public IReadOnlyList<CycleExplainedElement> CycleExplained
        {
            get
            {
                if( _cycleExplained == null && _cycle != null )
                {
                    int num = _cycle.Count;
                    CycleExplainedElement[] ac = new CycleExplainedElement[ num-- ];
                    ac[0] = new CycleExplainedElement( CycleExplainedElement.Start, _cycle[0].Item );
                    int i = 0;
                    while( i < num )
                    {
                        // From & To are normalized to the Container object if they are heads.
                        ISortedItem from = _cycle[i++];
                        if( from.IsGroupHead ) from = from.ContainerForHead;
                        ISortedItem to = _cycle[i];
                        if( to.IsGroupHead ) to = to.ContainerForHead;

                        // First relations are searched differently: 
                        char rel;
                        if( i > 3 )
                        {
                            if( IsContainedBy( from, to ) ) rel = CycleExplainedElement.ContainedBy;
                            else if( IsContains( from, to ) ) rel = CycleExplainedElement.Contains;
                            else if( IsRequiredBy(from, to) ) rel = CycleExplainedElement.RequiredByRequires;
                            else if( IsGeneralizedBy( from, to ) ) rel = CycleExplainedElement.GeneralizedBy;
                            else rel = CycleExplainedElement.Requires;
                        }
                        else
                        {
                            if( IsGeneralizedBy( from, to ) ) rel = CycleExplainedElement.GeneralizedBy;
                            else if( IsRequiredBy( from, to ) ) rel = CycleExplainedElement.RequiredByRequires;
                            else if( IsRequires( from, to ) ) rel = CycleExplainedElement.Requires;
                            else if( IsContains( from, to ) ) rel = CycleExplainedElement.Contains;
                            else rel = CycleExplainedElement.ContainedBy;
                        }
                        ac[i] = new CycleExplainedElement( rel, to.Item );
                    }
                    _cycleExplained = new ReadOnlyListOnIList<CycleExplainedElement>( ac );
                }
                return _cycleExplained;
            }
        }

        /// <summary>
        /// Gets a description of the detected cycle. Null if <see cref="CycleDetected"/> is null.
        /// </summary>
        public string CycleExplainedString
        {
            get { return CycleExplained != null ? String.Join( " ", CycleExplained ) : null; }
        }

        /// <summary>
        /// Gets a description of the required missing dependencies. 
        /// Null if no missing required dependency exists.
        /// </summary>
        public string RequiredMissingDependenciesExplained
        {
            get 
            { 
                string s = String.Join( "', '", ItemIssues.Where( d => d.RequiredMissingCount > 0 ).Select( d => "'" + d.Item.FullName + "' => {'" + String.Join( "', '", d.RequiredMissingDependencies ) + "'}" ) );
                return s.Length == 0 ? null : s; 
            }
        }

        /// <summary>
        /// Logs <see cref="CycleExplainedString"/> and any structure errors. Does nothing if <see cref="IsComplete"/> is true.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        public void LogError( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( HasStructureError )
            {
                foreach( var bug in ItemIssues.Where( d => d.StructureError != DependentItemStructureError.None ) )
                {
                    bug.LogError( logger );
                }
            }
            if( CycleDetected != null )
            {
                logger.Error( "Cycle detected: {0}.", CycleExplainedString );
            }
        }

        private static bool IsGeneralizedBy( ISortedItem from, ISortedItem to )
        {
            return from.Generalization == to;
        }

        private static bool IsRequires( ISortedItem from, ISortedItem to )
        {
            // We want to know here if the Requires relation is defined at the DependentItem level.
            // If we challenge the from.Requires (HashSet of IDependentItemRef which can not be used efficiently here), 
            // we'll be able to say that from requires to or to is required by from.
            // It is simpler and quite as efficient to challenge the original list.
            return from.Item.Requires != null && from.Item.Requires.Where( r => r != null && !r.Optional ).Any( r => r == to.Item || r.FullName == to.FullName );
        }

        private static bool IsRequiredBy( ISortedItem from, ISortedItem to )
        {
            // See comment above.
            return to.Item.RequiredBy != null && to.Item.RequiredBy.Any( r => r != null && (r == from.Item || r.FullName == from.FullName) );
        }

        private static bool IsContains( ISortedItem from, ISortedItem to )
        {
            return from.Container == to;
        }

        private static bool IsContainedBy( ISortedItem from, ISortedItem to )
        {
            return to.Container == from;
        }

    }

}
