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
        IReadOnlyList<CycleExplainedElement> _cycleExplained;
        int _itemIssueWithStructureErrorCount;

        internal DependencySorterResult( List<DependencySorter.Entry> result, List<IDependentItem> cycle, List<DependentItemIssue> itemIssues )
        {
            Debug.Assert( (result == null) != (cycle == null), "cycle ^ result" );
            if( result == null )
            {
                SortedItems = null;
                CycleDetected = new ReadOnlyListOnIList<IDependentItem>( cycle );
            }
            else
            {
                SortedItems = new ReadOnlyListOnIList<ISortedItem, DependencySorter.Entry>( result );
                CycleDetected = null;
            }
            ItemIssues = itemIssues != null && itemIssues.Count > 0 ? new ReadOnlyListOnIList<DependentItemIssue>( itemIssues ) : ReadOnlyListEmpty<DependentItemIssue>.Empty;
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
        /// True if at least one non-optional requirement (a requirement that is not prefixed with '?') exists.
        /// (If this is true then <see cref="HasStructureError"/> is also true since a missing dependency is 
        /// flagged with <see cref="DependentItemStructureError.MissingDependency"/>.)
        /// </summary>
        public bool HasRequiredMissing
        {
            get 
            {
                Debug.Assert( !ItemIssues.Any( m => m.RequiredMissingCount > 0 ) || HasStructureError );
                return ItemIssues.Any( m => m.RequiredMissingCount > 0 ); 
            }
        }

        /// <summary>
        /// True if at least one relation between an item and its container is invalid (true when <see cref="HasRequiredMissing"/> is true).
        /// </summary>
        public bool HasStructureError
        {
            get { return StructureErrorCount > 0; }
        }

        /// <summary>
        /// Number of items that have at least one invalid relation between itself and its container, its children or its dependencies.
        /// </summary>
        public int StructureErrorCount
        {
            get 
            {
                if( _itemIssueWithStructureErrorCount < 0 )
                {
                    _itemIssueWithStructureErrorCount = ItemIssues.Count( m => m.StructureError != DependentItemStructureError.None );
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
                if( _cycleExplained == null && CycleDetected != null )
                {
                    int num = CycleDetected.Count;
                    CycleExplainedElement[] ac = new CycleExplainedElement[ num-- ];
                    ac[0] = new CycleExplainedElement( CycleExplainedElement.Start, CycleDetected[0] );
                    int i = 0;
                    while( i < num )
                    {
                        IDependentItem from = CycleDetected[i++];
                        IDependentItem to = CycleDetected[i];

                        bool normal = num > 3;

                        char rel;
                        if( normal )
                        {
                            if( IsContainedBy(from, to) ) rel = CycleExplainedElement.ContainedBy;
                            else if( IsContains(from, to) ) rel = CycleExplainedElement.Contains;
                            else if( IsRequiredBy(from, to) ) rel = CycleExplainedElement.RequiredByRequires;
                            else rel = CycleExplainedElement.Requires;
                        }
                        else
                        {
                            if( IsRequires( from, to ) ) rel = CycleExplainedElement.Requires;
                            else if( IsRequiredBy( from, to ) ) rel = CycleExplainedElement.RequiredByRequires;
                            else if( IsContains( from, to ) ) rel = CycleExplainedElement.Contains;
                            else rel = CycleExplainedElement.ContainedBy;
                        }
                        ac[i] = new CycleExplainedElement( rel, to );
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
        /// Gets a description of the required missing dependencies. Null if no missing required dependency exists.
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
            if( CycleDetected != null )
            {
                logger.Error( "Cycle detected: {0}.", CycleExplainedString );
            }
            if( HasStructureError )
            {
                foreach( var bug in ItemIssues.Where( d => d.StructureError != DependentItemStructureError.None ) )
                {
                    bug.LogError( logger );
                }
            }
        }

        private static bool IsRequires( IDependentItem from, IDependentItem to )
        {
            return from.Requires != null && from.Requires.Where( r => !r.Optional ).Any( r => r.FullName == to.FullName );
        }

        private static bool IsRequiredBy( IDependentItem from, IDependentItem to )
        {
            return to.RequiredBy != null && to.RequiredBy.Any( r => r.FullName == from.FullName );
        }

        private static bool IsContains( IDependentItem from, IDependentItem to )
        {
            return from.Container == to || (from.Container != null && from.Container.FullName == to.FullName);
        }

        private static bool IsContainedBy( IDependentItem from, IDependentItem to )
        {
            return to.Container == from || (to.Container != null && to.Container.FullName == from.FullName);
        }

    }

}
