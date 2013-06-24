using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// An identifier (a <see cref="SqlTokenIdentifier"/>, typically a variable name) followed by a type declaration (<see cref="SqlExprTypeDecl"/>).
    /// </summary>
    public class SqlExprTypedIdentifier : SqlNoExpr
    {
        public SqlExprTypedIdentifier( SqlTokenIdentifier identifier, SqlExprTypeDecl type )
            : base( Build( identifier, type ) )
        {
        }

        private static ISqlItem[] Build( SqlTokenIdentifier identifier, SqlExprTypeDecl type )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( type == null ) throw new ArgumentNullException( "type" );
            return CreateArray( identifier, type );
        }

        internal SqlExprTypedIdentifier( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier Identifier { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlExprTypeDecl TypeDecl { get { return (SqlExprTypeDecl)Slots[1]; } }

        public string ToStringClean()
        {
            string s = Identifier.Name;
            s += " " + TypeDecl.Tokens.ToStringWithoutTrivias( String.Empty );
            return s;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
