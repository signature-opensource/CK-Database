#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprTypedIdentifier.cs) is part of CK-Database. 
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
    /// An identifier (a <see cref="SqlTokenIdentifier"/>, typically a variable name) followed by a type declaration (<see cref="SqlExprTypeDecl"/>).
    /// </summary>
    public class SqlExprTypedIdentifier : SqlNoExpr
    {
        public SqlExprTypedIdentifier( SqlTokenIdentifier identifier, SqlToken optAsToken,  SqlExprTypeDecl type )
            : base( Build( identifier, optAsToken, type ) )
        {
        }

        private static ISqlItem[] Build( SqlTokenIdentifier identifier, SqlToken optAsToken, SqlExprTypeDecl type )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( type == null ) throw new ArgumentNullException( "type" );
            return optAsToken != null ? CreateArray( identifier, optAsToken, type ) : CreateArray( identifier, type );
        }

        internal SqlExprTypedIdentifier( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier Identifier { get { return (SqlTokenIdentifier)Slots[0]; } }

        /// <summary>
        /// Gets the optional AS token that may appear in function parameters between the parameter name
        /// and the type.
        /// </summary>
        public SqlToken AsToken { get { return Slots.Length == 2 ? null : (SqlToken)Slots[1]; } }

        public SqlExprTypeDecl TypeDecl { get { return (SqlExprTypeDecl)Slots[Slots.Length-1]; } }

        public string ToStringClean()
        {
            string s = Identifier.Name;
            s += " " + TypeDecl.Tokens.ToStringWithoutTrivias( String.Empty );
            return s;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
