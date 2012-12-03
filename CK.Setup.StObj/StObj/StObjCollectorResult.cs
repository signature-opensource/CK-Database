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
        readonly AmbientContractCollectorResult<StObjTypeInfo> _contractResult;
        IReadOnlyCollection<IStObj> _rootStObjs;
        IReadOnlyList<IStObj> _orderedStObjs;
        bool _fatal;

        internal StObjCollectorResult( StObjMapper owner, AmbientContractCollectorResult<StObjTypeInfo> contractResult )
        {
            Debug.Assert( contractResult != null );
            _contractResult = contractResult;
            foreach( AmbientContractCollectorContextualResult<StObjTypeInfo> r in contractResult )
            {
                Add( new StObjCollectorContextualResult( r, new StObjContextualMapper( owner, r.Mappings ) ) );
            }
        }

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
        
        /// <summary>
        /// Gets all the <see cref="IStObj"/> that have no <see cref="IStObj.Generalization"/>.
        /// Empty if <see cref="HasFatalError"/> is true.
        /// </summary>
        public IReadOnlyCollection<IStObj> RootStObjs
        {
            get 
            {
                if( _rootStObjs == null )
                {
                    List<IStObj> heads = new List<IStObj>();
                    foreach( var ctx in this )
                    {
                        heads.AddRange( ctx.MutableItems.Where( m => m.Generalization == null ) );
                    }
                    _rootStObjs = heads.ToReadOnlyCollection();
                }
                return _rootStObjs; 
            }
        }

        internal IEnumerable<MutableItem> AllMutableItems
        {
            get { return this.SelectMany( r => r.MutableItems ); }
        }

        internal IEnumerable<MutableItem> FindMutableItemsFor( Type t )
        {
            return this.Select( r => r.Find( t ) ).Where( m => m != null );
        }

        internal void SetFatal()
        {
            _fatal = true;
            _rootStObjs = ReadOnlyListEmpty<IStObj>.Empty;
            _orderedStObjs = ReadOnlyListEmpty<IStObj>.Empty;
        }

        internal void SetSuccess( IReadOnlyList<IStObj> ordered )
        {
            Debug.Assert( !HasFatalError && _orderedStObjs == null );
            _orderedStObjs = ordered;
        }
    }
}
