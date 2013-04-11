using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprUnaryOperator : SqlExpr, ISqlExprEnclosable
    {
        readonly IAbstractExpr[] _components;
        
        public SqlExprUnaryOperator( SqlToken op, SqlExpr rightExpr )
        {
            if( op == null ) throw new ArgumentNullException( "op" );
            if( rightExpr == null ) throw new ArgumentNullException( "rightExpr" );
            _components = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, op, rightExpr, SqlExprMultiToken<SqlTokenClosePar>.Empty );
        }

        internal SqlExprUnaryOperator( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExprMultiToken<SqlTokenOpenPar> Opener { get { return (SqlExprMultiToken<SqlTokenOpenPar>)_components[0]; } }


        public SqlToken Operator { get { return (SqlToken)_components[1]; } }

        public SqlExpr Expression { get { return (SqlExpr)_components[2]; } }

        public SqlExprMultiToken<SqlTokenClosePar> Closer { get { return (SqlExprMultiToken<SqlTokenClosePar>)_components[3]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public bool CanEnclose
        {
            get { return true; }
        }

        public ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            return new SqlExprUnaryOperator( CreateArray( openPar, _components, closePar ) );
        }

        public IEnumerable<IAbstractExpr> ComponentsWithoutParenthesis
        {
            get { throw new NotImplementedException(); }
        }

    }
}
