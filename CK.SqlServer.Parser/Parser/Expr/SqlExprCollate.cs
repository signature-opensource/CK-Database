#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprCollate.cs) is part of CK-Database. 
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
    public class SqlExprCollate : SqlExpr
    {
        public SqlExprCollate( SqlExpr left, SqlTokenIdentifier collateT, SqlTokenIdentifier nameT )
            : this( CreateArray( SqlToken.EmptyOpenPar, left, collateT, nameT, SqlToken.EmptyClosePar ) )
        {
        }

        internal SqlExprCollate( ISqlItem[] items )
            : base( items )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
