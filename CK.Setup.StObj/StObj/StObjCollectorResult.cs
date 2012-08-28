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
        readonly AmbiantContractCollectorResult _contractResult;
        IReadOnlyCollection<IStObj> _rootStObjs;
        int _totalItemCount;

        bool _fatal;

        internal StObjCollectorResult( StObjMapper owner, AmbiantContractCollectorResult contractResult )
        {
            Debug.Assert( contractResult != null );
            _contractResult = contractResult;
            foreach( AmbiantContractCollectorContextualResult r in contractResult )
            {
                Add( new StObjCollectorContextualResult( r, new StObjContextualMapper( owner, r.Mappings ) ) );
            }
        }

        public override bool HasFatalError
        {
            get
            {
                return _fatal || _contractResult.HasFatalError || base.HasFatalError;
            }
        }

        /// <summary>
        /// Gets the total number of <see cref="IStObj"/> available. 
        /// Zero if <see cref="HasFatalError"/> is true.
        /// </summary>
        public int TotalItemCount
        {
            get { return _totalItemCount; }
        }

        /// <summary>
        /// Gets all the <see cref="IStObj"/> that have no <see cref="IStObj.Generalization"/>.
        /// Empty if <see cref="HasFatalError"/> is true.
        /// </summary>
        public IReadOnlyCollection<IStObj> RootStObjs
        {
            get { return _rootStObjs; }
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
        }

        internal void SetSuccess()
        {
            Debug.Assert( !HasFatalError );
            Debug.Assert( _totalItemCount == 0 );
            List<IStObj> heads = new List<IStObj>();
            foreach( var ctx in this )
            {
                _totalItemCount += ctx.MutableItems.Count;
                heads.AddRange( ctx.MutableItems.Where( m => m.Generalization == null ) );
            }
            _rootStObjs = heads.ToReadOnlyCollection();
        }
    }
}
