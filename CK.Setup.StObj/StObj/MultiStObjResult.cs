using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class MultiStObjResult : MultiContextResult<StObjResult>, IDependentItemDiscoverer
    {
        MultiAmbiantContractResult _contractResult;

        internal MultiStObjResult( MultiAmbiantContractResult contractResult )
        {
            Debug.Assert( contractResult != null );
            _contractResult = contractResult;
        }

        public override bool HasFatalError
        {
            get
            {
                return _contractResult.HasFatalError || base.HasFatalError;
            }
        }

        public int TotalDependentItemCount
        {
            get 
            {
                int c = 0;
                Foreach( r => c += r.StObjMapper.Count );
                return c;
            }
        }

        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            IEnumerable<IDependentItem> e = Default.GetOtherItemsToRegister();
            Foreach( r => e = e.Concat( r.GetOtherItemsToRegister() ) );
            return e;
        }

    }
}
