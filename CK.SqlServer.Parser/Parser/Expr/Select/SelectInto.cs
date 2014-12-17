#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\Select\SelectInto.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Captures the optional "INTO table".
    /// </summary>
    public class SelectInto : SqlNoExpr
    {

        public SelectInto( SqlTokenIdentifier intoToken, SqlExprMultiIdentifier tableName )
            : this( CreateArray( intoToken, tableName ) )
        {
        }

        internal SelectInto( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExprMultiIdentifier TableName { get { return (SqlExprMultiIdentifier)Slots[1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
