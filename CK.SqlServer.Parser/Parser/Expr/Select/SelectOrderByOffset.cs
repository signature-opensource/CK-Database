#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\Select\SelectOrderByOffset.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Parser
{
    public class SelectOrderByOffset : SqlNoExpr
    {
        public SelectOrderByOffset( SqlTokenIdentifier offsetToken, SqlExpr offsetExpr, SqlTokenIdentifier rowsToken )
            : base( CreateArray( offsetToken, offsetExpr, rowsToken ) )
        {
        }

        public SelectOrderByOffset( SqlTokenIdentifier offsetToken, SqlExpr offsetExpr, SqlTokenIdentifier rowsToken,
                                    SqlTokenIdentifier fetchToken, SqlTokenIdentifier firstOrNextToken, SqlExpr fetchExpr, SqlTokenIdentifier fetchRowsToken, SqlTokenIdentifier onlyToken )
            : base( CreateArray( offsetToken, offsetExpr, rowsToken, fetchToken, firstOrNextToken, fetchExpr, fetchRowsToken, onlyToken ) )
        {
        }

        internal SelectOrderByOffset( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier OffsetT { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlExpr OffsetExpression { get { return (SqlExpr)Slots[1]; } }

        public SqlTokenIdentifier RowsT { get { return (SqlTokenIdentifier)Slots[2]; } }
    
        public bool HasFetchClause { get { return Slots.Length > 3; } }

        public SqlTokenIdentifier FetchT { get { return HasFetchClause ? (SqlTokenIdentifier)Slots[3] : null; } }

        public SqlTokenIdentifier FetchFirstOrNextT { get { return HasFetchClause ? (SqlTokenIdentifier)Slots[4] : null; } }

        public SqlExpr FetchExpression { get { return HasFetchClause ? (SqlExpr)Slots[5] : null; } }

        public SqlTokenIdentifier FetchRowsT { get { return HasFetchClause ? (SqlTokenIdentifier)Slots[6] : null; } }

        public SqlTokenIdentifier FetchOnlyT { get { return HasFetchClause ? (SqlTokenIdentifier)Slots[7] : null; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}