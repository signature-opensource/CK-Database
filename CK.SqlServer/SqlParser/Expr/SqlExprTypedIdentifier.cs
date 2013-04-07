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
    public class SqlExprTypedIdentifier : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprTypedIdentifier( SqlTokenIdentifier identifier, SqlExprTypeDecl type )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( type == null ) throw new ArgumentNullException( "type" );
            _components = CreateArray( identifier, type );
        }

        public SqlTokenIdentifier Identifier { get { return (SqlTokenIdentifier)_components[0]; } }

        public SqlExprTypeDecl TypeDecl { get { return (SqlExprTypeDecl)_components[1]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
