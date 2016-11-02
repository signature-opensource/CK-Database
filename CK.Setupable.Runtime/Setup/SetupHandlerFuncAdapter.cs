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
    /// Internal adapter used by <see cref="SetupItemDriver"/>.
    /// </summary>
    class SetupHandlerFuncAdapter : ISetupHandler
    {
        readonly Func<SetupItemDriver,bool> _func;
        readonly SetupCallGroupStep _step;

        public SetupHandlerFuncAdapter( Func<SetupItemDriver, bool> handler, SetupCallGroupStep step )
        {
            _func = handler;
            _step = step;
        }

        public bool OnStep( SetupItemDriver d, SetupCallGroupStep step )
        {
            return _step == step ? _func( d ) : true;
        }

        public bool Init( SetupItemDriver d ) => true;

        public bool InitContent( SetupItemDriver d ) => true;
    
        public bool Install( SetupItemDriver d ) => true;

        public bool InstallContent( SetupItemDriver d ) => true;

        public bool Settle( SetupItemDriver d ) => true;

        public bool SettleContent( SetupItemDriver d ) => true;

    }

}
