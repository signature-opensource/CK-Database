using System;
using CK.Setup;
using CK.Core;

namespace CK.StObj.Engine.Tests
{
    class StructuralConfiguratorHelper : IStObjStructuralConfigurator
    {
        readonly Action<IStObjMutableItem> _conf;

        public StructuralConfiguratorHelper( Action<IStObjMutableItem> conf )
        {
            _conf = conf;
        }

        public void Configure( IActivityLogger logger, IStObjMutableItem o )
        {
            _conf( o );
        }
    }
}
