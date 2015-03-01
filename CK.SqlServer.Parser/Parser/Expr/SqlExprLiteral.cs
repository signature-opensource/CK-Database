#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprLiteral.cs) is part of CK-Database. 
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
    /// Literal numbers (including 0x... literal binary values) and strings (either N'unicode' or 'one-byte-char').
    /// See <see cref="SqlTokenBaseLiteral"/>.
    /// </summary>
    public class SqlExprLiteral : SqlExprBaseMonoToken<SqlTokenBaseLiteral>
    {
        public SqlExprLiteral( SqlTokenBaseLiteral t )
            : base( t )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }


    }


}
