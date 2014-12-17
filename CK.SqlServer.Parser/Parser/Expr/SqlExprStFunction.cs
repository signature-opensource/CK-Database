#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprStFunction.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public abstract class SqlExprStFunction : SqlExprBaseSt
    {
        protected SqlExprStFunction( ISqlItem[] items, SqlTokenTerminal term )
            : base( items, term )
        {
        }

        internal SqlExprStFunction( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier AlterOrCreateT { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlTokenIdentifier ObjectTypeT { get { return (SqlTokenIdentifier)Slots[1]; } }

        /// <summary>
        /// Gets the name of the procedure (may start with the Schema).
        /// </summary>
        public SqlExprMultiIdentifier Name { get { return (SqlExprMultiIdentifier)Slots[2]; } }

        public SqlExprParameterList Parameters { get { return (SqlExprParameterList)Slots[3]; } }

        public SqlTokenIdentifier ReturnsT { get { return (SqlTokenIdentifier)Slots[4]; } }

    }
}
