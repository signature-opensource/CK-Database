using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Internal adapter used by <see cref="SetupDriver"/>.
    /// </summary>
    class SetupHandlerFuncAdapter : ISetupHandler
    {
        readonly Func<SetupDriver,bool> _func;
        readonly SetupCallGroupStep _step;

        public SetupHandlerFuncAdapter( Func<SetupDriver, bool> handler, SetupCallGroupStep step )
        {
            _func = handler;
            _step = step;
        }

        public bool Init( SetupDriver d )
        {
            return _step == SetupCallGroupStep.Init ? _func( d ) : true;
        }

        public bool InitContent( SetupDriver d )
        {
            return _step == SetupCallGroupStep.InitContent ? _func( d ) : true;
        }

        public bool Install( SetupDriver d )
        {
            return _step == SetupCallGroupStep.Install ? _func( d ) : true;
        }

        public bool InstallContent( SetupDriver d )
        {
            return _step == SetupCallGroupStep.InstallContent ? _func( d ) : true;
        }

        public bool Settle( SetupDriver d )
        {
            return _step == SetupCallGroupStep.Settle ? _func( d ) : true;
        }

        public bool SettleContent( SetupDriver d )
        {
            return _step == SetupCallGroupStep.SettleContent ? _func( d ) : true;
        }
    }

}
