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
            if( itemName.TransformArg != null )
            {
                throw new NotImplementedException( "Transformer can not be loaded by file." );
                // Actually, all file based should be removed...
                // File based items MUST be handled like the stobj.
                // A first toplogical sort must organize the packages (the objects)
                // Then the Dynamic initialization phase must take place.
                // File based resources must be some kind of FileStObj objects.
                // In the future, this file based approach could be participate to a "full model 
                // based approach" from which C# implementations could be generated...
            }
            var name = itemName as SqlContextLocName ?? new SqlContextLocName( itemName.Context, itemName.Location, itemName.Name );
            var item = SqlBaseItem.Parse( monitor, name, _parser, text, fileName, package, expectedItemTypes );
            return item;
        }
    }
}
