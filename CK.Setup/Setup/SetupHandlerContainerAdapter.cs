using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    class SetupHandlerContainerAdapter : ISetupHandlerContainer
    {
        readonly Func<SetupDriverContainer,bool> _func;
        readonly SetupCallContainerStep _step;

        public SetupHandlerContainerAdapter( Func<SetupDriverContainer, bool> handler, SetupCallContainerStep step )
        {
            _func = handler;
            _step = step;
        }

        public bool Init( SetupDriverContainer d )
        {
            return _step == SetupCallContainerStep.Init ? _func( d ) : true;
        }

        public bool InitContent( SetupDriverContainer d )
        {
            return _step == SetupCallContainerStep.InitContent ? _func( d ) : true;
        }

        public bool Install( SetupDriverContainer d )
        {
            return _step == SetupCallContainerStep.Install ? _func( d ) : true;
        }

        public bool InstallContent( SetupDriverContainer d )
        {
            return _step == SetupCallContainerStep.InstallContent ? _func( d ) : true;
        }

        public bool Settle( SetupDriverContainer d )
        {
            return _step == SetupCallContainerStep.Settle ? _func( d ) : true;
        }

        public bool SettleContent( SetupDriverContainer d )
        {
            return _step == SetupCallContainerStep.SettleContent ? _func( d ) : true;
        }
    }

}
