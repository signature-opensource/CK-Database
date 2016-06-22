#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Engine\SqlSetupAspect.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    class SqlItemParser : ISetupItemParser
    {
        readonly ISqlServerParser _parser;

        public SqlItemParser( ISqlServerParser parser )
        {
            _parser = parser;
        }

        public ISetupItem Create( IActivityMonitor monitor, IContextLocNaming itemName, string text, string fileName, IDependentItemContainer package = null, IEnumerable<string> expectedItemTypes = null )
        {
            var name = itemName as SqlContextLocName ?? new SqlContextLocName( itemName.Context, itemName.Location, itemName.Name );
            return SqlBaseItem.Parse( monitor, name, _parser, text, fileName, package, expectedItemTypes );
        }
    }
}
