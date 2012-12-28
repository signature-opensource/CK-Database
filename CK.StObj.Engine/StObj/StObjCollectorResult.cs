using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjCollectorResult : MultiContextualResult<StObjCollectorContextualResult>
    {
        readonly AmbientContractCollectorResult<StObjTypeInfo,MutableItem> _contractResult;
        IReadOnlyList<MutableItem> _orderedStObjs;
        bool _fatal;

        internal StObjCollectorResult( StObjMapper owner, AmbientContractCollectorResult<StObjTypeInfo, MutableItem> contractResult )
        {
            Debug.Assert( contractResult != null );
            _contractResult = contractResult;
            foreach( var r in contractResult.Contexts )
            {
                Add( new StObjCollectorContextualResult( r, new StObjContextualMapper( owner, r.Mappings ) ) );
            }
        }

        /// <summary>
        /// True if a fatal error occured. Result should be discarded.
        /// </summary>
        public override bool HasFatalError
        {
            get { return _fatal || _contractResult.HasFatalError || base.HasFatalError; }
        }
        
        /// <summary>
        /// Gets all the <see cref="IStObj"/> ordered by their dependencies.
        /// Empty if <see cref="HasFatalError"/> is true.
        /// </summary>
        public IReadOnlyList<IStObj> OrderedStObjs
        {
            get { return _orderedStObjs; }
        }

        public void GenerateFinalAssembly( IActivityLogger logger )
        {
            if( HasFatalError ) throw new InvalidOperationException();
            DynamicAssembly a = new DynamicAssembly( DynamicAssembly.DefaultAssemblyName );
            foreach( var m in _orderedStObjs )
            {
                if( m.Specialization == null ) m.CreateFinalType( logger, a );
            }

        }

        internal IEnumerable<MutableItem> AllMutableItems
        {
            get { return Contexts.SelectMany( r => r.MutableItems ); }
        }

        internal IEnumerable<MutableItem> FindMutableItemsFor( Type t )
        {
            return Contexts.Select( r => r.Find( t ) ).Where( m => m != null );
        }

        internal void SetFatal()
        {
            _fatal = true;
            _orderedStObjs = ReadOnlyListEmpty<MutableItem>.Empty;
        }

        internal void SetSuccess( IReadOnlyList<MutableItem> ordered )
        {
            Debug.Assert( !HasFatalError && _orderedStObjs == null );
            _orderedStObjs = ordered;
        }
    }
}
