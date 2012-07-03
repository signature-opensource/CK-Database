using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Internal adapter used by <see cref="ContainerDriver"/>.
    /// </summary>
    class ContainerHandlerFuncAdapter : IContainerHandler
    {
        readonly Func<ContainerDriver,bool> _func;
        readonly SetupCallContainerStep _step;

        public ContainerHandlerFuncAdapter( Func<ContainerDriver, bool> handler, SetupCallContainerStep step )
        {
            _func = handler;
            _step = step;
        }

        public bool Init( ContainerDriver d )
        {
            return _step == SetupCallContainerStep.Init ? _func( d ) : true;
        }

        public bool InitContent( ContainerDriver d )
        {
            return _step == SetupCallContainerStep.InitContent ? _func( d ) : true;
        }

        public bool Install( ContainerDriver d )
        {
            return _step == SetupCallContainerStep.Install ? _func( d ) : true;
        }

        public bool InstallContent( ContainerDriver d )
        {
            return _step == SetupCallContainerStep.InstallContent ? _func( d ) : true;
        }

        public bool Settle( ContainerDriver d )
        {
            return _step == SetupCallContainerStep.Settle ? _func( d ) : true;
        }

        public bool SettleContent( ContainerDriver d )
        {
            return _step == SetupCallContainerStep.SettleContent ? _func( d ) : true;
        }
    }

}
