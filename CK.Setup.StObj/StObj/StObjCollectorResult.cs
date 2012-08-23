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
        AmbiantContractCollectorResult _contractResult;
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

        public int TotalItemCount
        {
            get 
            {
                int c = 0;
                Foreach( r => c += r.StObjMapper.Count );
                return c;
            }
        }

        internal void SetFatal()
        {
            _fatal = true;
        }

        internal IEnumerable<MutableItem> AllMutableItems
        {
            get { return this.SelectMany( r => r.MutableItems ); }
        }

        internal IEnumerable<MutableItem> FindMutableItemsFor( Type t )
        {
            return this.Select( r => r.Find( t ) ).Where( m => m != null );
        }

    }
}
