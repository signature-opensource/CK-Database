#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\StructuralConfiguratorHelper.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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

        public void Configure( IActivityMonitor monitor, IStObjMutableItem o )
        {
            _conf( o );
        }
    }
}
