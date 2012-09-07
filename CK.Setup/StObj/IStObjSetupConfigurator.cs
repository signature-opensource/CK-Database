using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public interface IStObjSetupConfigurator
    {
        void ConfigureDependentItem( IActivityLogger logger, StObjSetupData data );
    }

}
