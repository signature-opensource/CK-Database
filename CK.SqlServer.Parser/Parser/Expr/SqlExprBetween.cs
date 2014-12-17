#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprBetween.cs) is part of CK-Database. 
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
    public class SqlExprBetween : SqlExpr
    {
        public SqlExprBetween( SqlExpr left, SqlTokenIdentifier notT, SqlTokenIdentifier betweenT, SqlExpr start, SqlTokenIdentifier andT, SqlItem stop )
            : this( Build( left, notT, betweenT, start, andT, stop ) )
        {
        }

        internal SqlExprBetween( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        static ISqlItem[] Build( SqlExpr left, SqlTokenIdentifier notT, SqlTokenIdentifier betweenT, SqlExpr start, SqlTokenIdentifier andT, SqlItem stop )
        {
            return notT != null
                            ? CreateArray( SqlToken.EmptyOpenPar, left, notT, betweenT, start, andT, stop, SqlToken.EmptyClosePar )
                            : CreateArray( SqlToken.EmptyOpenPar, left, betweenT, start, andT, stop, SqlToken.EmptyClosePar );
        }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        public bool IsNotBetween { get { return Slots.Length == 8; } }

        public SqlTokenIdentifier NotT { get { return IsNotBetween ? (SqlTokenIdentifier)Slots[2] : null; } }

        public SqlTokenIdentifier BetweenT { get { return (SqlTokenIdentifier)Slots[IsNotBetween ? 3 : 2]; } }

        public SqlExpr Start { get { return (SqlExpr)Slots[IsNotBetween ? 4 : 3]; } }

        public SqlTokenIdentifier AndT { get { return (SqlTokenIdentifier)Slots[IsNotBetween ? 5 : 4]; } }

        public SqlExpr Stop { get { return (SqlExpr)Slots[IsNotBetween ? 6 : 5]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
