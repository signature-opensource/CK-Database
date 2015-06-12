#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprParameter.cs) is part of CK-Database. 
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
    public class SqlExprDeclare : SqlNoExpr
    {
        public SqlExprDeclare( SqlExprTypedIdentifier declVar, SqlTokenTerminal assignToken = null, SqlExpr initialValue = null )
            : this( Build( declVar, assignToken, initialValue ) )
        {
        }

        static ISqlItem[] Build( SqlExprTypedIdentifier declVar, SqlTokenTerminal assignToken = null, SqlExpr initialValue = null )
        {
            if( declVar == null ) throw new ArgumentNullException( "declVar" );
            if( !declVar.Identifier.IsVariable ) throw new ArgumentException( "Must be a @VariableName", "variable" );
            if( assignToken != null )
            {
                if( assignToken.TokenType != SqlTokenType.Assign ) throw new ArgumentException( "Must be '='.", "assignToken" );
                if( initialValue == null ) throw new ArgumentNullException( "initialValue" );
            }
            else if( initialValue != null ) throw new ArgumentNullException( "assignToken" );
            
            if( assignToken == null )
            {
                return CreateArray( declVar );
            }
            else
            {
                return CreateArray( declVar, assignToken, initialValue );
            }
        }

        internal SqlExprDeclare( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExprTypedIdentifier Variable { get { return (SqlExprTypedIdentifier)Slots[0]; } }

        public SqlTokenTerminal AssignT { get { return Slots.Length > 1 ? Slots[1] as SqlTokenTerminal : null; } }

        public SqlExpr InitialValue { get { return Slots.Length > 1 ? Slots[2] as SqlExpr : null; } }
        
        public bool HasInitialValue { get { return Slots.Length > 1; } }


        public string ToStringClean()
        {
            string s = Variable.ToStringClean();
            if( HasInitialValue ) s += " = " + InitialValue.Tokens.ToStringWithoutTrivias( " " );
            return s;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
