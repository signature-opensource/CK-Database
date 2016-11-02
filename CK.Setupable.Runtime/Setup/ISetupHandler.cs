#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\ISetupHandler.cs) is part of CK-Database. 
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
    /// 
    /// </summary>
    public interface ISetupHandler
    {
        bool Init( SetupItemDriver d );

        bool Install( SetupItemDriver d );

        bool Settle( SetupItemDriver d );
        
        bool InitContent( SetupItemDriver d );

        bool InstallContent( SetupItemDriver d );

        bool SettleContent( SetupItemDriver d );

        bool OnStep( SetupItemDriver d, SetupCallGroupStep step );
    }
}
