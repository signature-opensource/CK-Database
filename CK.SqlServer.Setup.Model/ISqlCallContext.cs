#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\ISqlCallContext.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Marker interface for classes that hold contextual parameters.
    /// </summary>
    public interface ISqlCallContext
    {
        //TODO: use duck typing
        SqlConnectionProvider GetProvider( string connectionString );
    }
}
