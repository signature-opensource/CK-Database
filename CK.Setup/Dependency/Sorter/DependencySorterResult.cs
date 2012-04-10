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

        internal DependencySorterResult( List<DependencySorter.Entry> result, List<IDependentItem> cycle, List<DependentItemIssue> missing )
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
            ItemIssues = missing != null && missing.Count > 0 ? new ReadOnlyListOnIList<DependentItemIssue>( missing ) : ReadOnlyListEmpty<DependentItemIssue>.Empty;
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
        /// </summary>
        public bool HasRequiredMissing
        {
            get { return ItemIssues.Any( m => m.RequiredMissingCount > 0 ); }
        }

        /// <summary>
        /// True if at least one relation between an item and its container is invalid.
        /// </summary>
        public bool HasStructureError
        {
            get { return ItemIssues.Any( m => m.StructureError != DependentItemStructureError.None ); }
        }

        /// <summary>
        /// True only if no cycle has been detected, no required missing dependencies and no error related 
        /// to container (<see cref="HasStructureError"/>) exist: <see cref="SortedItems"/> can be exploited.
        /// </summary>
        public bool IsComplete
        {
            get { return CycleDetected == null && HasRequiredMissing == false && HasStructureError == false; }
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

        private static bool IsRequires( IDependentItem from, IDependentItem to )
        {
            return from.Requires != null && from.Requires.Where( r => r[0] != '?' ).Any( r => r == to.FullName );
        }

        private static bool IsRequiredBy( IDependentItem from, IDependentItem to )
        {
            return to.RequiredBy != null && to.RequiredBy.Select( r => r.Replace( "?", String.Empty ) ).Any( r => r == from.FullName );
        }

        private static bool IsContains( IDependentItem from, IDependentItem to )
        {
            return from.Container == to || (from.Container != null && from.Container.FullName == to.FullName);
        }

        private static bool IsContainedBy( IDependentItem from, IDependentItem to )
        {
            return to.Container == from || (to.Container != null && to.Container.FullName == from.FullName);
        }

        /// <summary>
        /// Gets a description of the detected cycle. Null if <see cref="CycleDetected"/> is null.
        /// </summary>
        public string CycleExplainedString
        {
            get { return CycleExplained != null ? String.Join( " ", CycleExplained ) : null; }
        }
   
    }

}
