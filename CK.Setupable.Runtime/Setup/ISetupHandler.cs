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
        bool Init( GenericItemSetupDriver d );

        bool Install( GenericItemSetupDriver d );

        bool Settle( GenericItemSetupDriver d );
        
        bool InitContent( GenericItemSetupDriver d );

        bool InstallContent( GenericItemSetupDriver d );

        bool SettleContent( GenericItemSetupDriver d );
    }
}
