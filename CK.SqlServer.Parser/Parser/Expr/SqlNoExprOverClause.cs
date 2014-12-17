#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlNoExprOverClause.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Captures a select column definition. 
    /// </summary>
    public class SqlNoExprOverClause : SqlNoExpr
    {
        public SqlNoExprOverClause( SqlTokenIdentifier overT, SqlExpr overExpression )
            : this( CreateArray( overT, overExpression ) )
        {
        }

        internal SqlNoExprOverClause( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier OverT { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlExpr OverExpression { get { return (SqlExpr)Slots[1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
