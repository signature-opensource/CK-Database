#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprIsNull.cs) is part of CK-Database. 
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
    /// 
    /// </summary>
    public class SqlExprIsNull : SqlExpr
    {
        public SqlExprIsNull( SqlItem left, SqlTokenIdentifier isT, SqlTokenIdentifier notT, SqlTokenIdentifier nullT )
            : this( Build( left, isT, notT, nullT ) )
        {
        }

        static ISqlItem[] Build( SqlItem left, SqlTokenIdentifier isT, SqlTokenIdentifier notT, SqlTokenIdentifier nullT )
        {
            return notT != null 
                        ? CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, isT, notT, nullT, SqlExprMultiToken<SqlTokenClosePar>.Empty )
                        : CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, isT, nullT, SqlExprMultiToken<SqlTokenClosePar>.Empty );
        }

        internal SqlExprIsNull( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlItem Left { get { return (SqlItem)Slots[1]; } }

        public SqlTokenIdentifier IsT { get { return (SqlTokenIdentifier)Slots[2]; } }

        public bool IsNotNull { get { return Slots.Length == 6; } }

        public SqlTokenIdentifier NotT { get { return IsNotNull ? (SqlTokenIdentifier)Slots[3] : null; } }

        public SqlTokenIdentifier NullT { get { return (SqlTokenIdentifier)Slots[IsNotNull ? 4 : 3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }


    }


}
