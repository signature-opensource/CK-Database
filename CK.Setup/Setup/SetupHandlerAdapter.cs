using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    class SetupHandlerAdapter : ISetupHandler
    {
        readonly Func<SetupDriver,bool> _func;
        readonly SetupStep _step;

        public SetupHandlerAdapter( Func<SetupDriver, bool> handler, SetupStep step )
        {
            _func = handler;
            _step = step;
        }

        public bool Init( SetupDriver d )
        {
            return _step == SetupStep.Init ? _func( d ) : true;
        }

        public bool Install( SetupDriver d )
        {
            return _step == SetupStep.Install ? _func( d ) : true;
        }

        public bool Settle( SetupDriver d )
        {
            return _step == SetupStep.Settle ? _func( d ) : true;
        }
    }

}
