using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Internal adapter used by <see cref="ItemDriver"/>.
    /// </summary>
    class ItemHandlerFuncAdapter : IItemHandler
    {
        readonly Func<ItemDriver,bool> _func;
        readonly SetupStep _step;

        public ItemHandlerFuncAdapter( Func<ItemDriver, bool> handler, SetupStep step )
        {
            _func = handler;
            _step = step;
        }

        public bool Init( ItemDriver d )
        {
            return _step == SetupStep.Init ? _func( d ) : true;
        }

        public bool Install( ItemDriver d )
        {
            return _step == SetupStep.Install ? _func( d ) : true;
        }

        public bool Settle( ItemDriver d )
        {
            return _step == SetupStep.Settle ? _func( d ) : true;
        }
    }

}
