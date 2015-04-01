#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\SetupHandlerFuncAdapter.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Internal adapter used by <see cref="DependentItemSetupDriver"/>.
    /// </summary>
    class SetupHandlerFuncAdapter : ISetupHandler
    {
        readonly Func<DependentItemSetupDriver,bool> _func;
        readonly SetupCallGroupStep _step;

        public SetupHandlerFuncAdapter( Func<DependentItemSetupDriver, bool> handler, SetupCallGroupStep step )
        {
            _func = handler;
            _step = step;
        }

        public bool Init( DependentItemSetupDriver d )
        {
            return _step == SetupCallGroupStep.Init ? _func( d ) : true;
        }

        public bool InitContent( DependentItemSetupDriver d )
        {
            return _step == SetupCallGroupStep.InitContent ? _func( d ) : true;
        }

        public bool Install( DependentItemSetupDriver d )
        {
            return _step == SetupCallGroupStep.Install ? _func( d ) : true;
        }

        public bool InstallContent( DependentItemSetupDriver d )
        {
            return _step == SetupCallGroupStep.InstallContent ? _func( d ) : true;
        }

        public bool Settle( DependentItemSetupDriver d )
        {
            return _step == SetupCallGroupStep.Settle ? _func( d ) : true;
        }

        public bool SettleContent( DependentItemSetupDriver d )
        {
            return _step == SetupCallGroupStep.SettleContent ? _func( d ) : true;
        }
    }

}
