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
    public interface ISetupHandler
    {
        bool Init( DependentItemSetupDriver d );

        bool Install( DependentItemSetupDriver d );

        bool Settle( DependentItemSetupDriver d );
        
        bool InitContent( DependentItemSetupDriver d );

        bool InstallContent( DependentItemSetupDriver d );

        bool SettleContent( DependentItemSetupDriver d );
    }
}
