#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprStReturn.cs) is part of CK-Database. 
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
    public class SqlExprStSetVar : SqlExprBaseSt
    {
        public SqlExprStSetVar( SqlTokenIdentifier setToken, SqlTokenIdentifier variable, SqlTokenTerminal assignT, SqlExpr right, SqlTokenTerminal terminator )
            : base( Build( setToken, variable, assignT, right ),  terminator )
        {
        }

        internal SqlExprStSetVar( ISqlItem[] components )
            : base( components )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier setToken, SqlTokenIdentifier variable, SqlTokenTerminal assignT, SqlExpr right )
        {
            if( setToken == null || setToken.TokenType != SqlTokenType.Set ) throw new ArgumentException( "setToken" );
            if( variable == null ) throw new ArgumentException( "variable" );
            if( assignT == null || (assignT.TokenType & SqlTokenType.IsAssignOperator) == 0 ) throw new ArgumentException( "variable" );
            if( right == null ) throw new ArgumentException( "right" );
            return CreateArray( setToken, variable, assignT, right );
        }

        public SqlTokenIdentifier SetT { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlTokenIdentifier Variable { get { return (SqlTokenIdentifier)Slots[1]; } }

        public SqlTokenTerminal AssigntT { get { return (SqlTokenTerminal)Slots[2]; } }
        
        public SqlExpr Value { get { return (SqlExpr)Slots[3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
