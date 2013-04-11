using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseBinary : SqlExpr, ISqlExprEnclosable
    {
        readonly IAbstractExpr[] _components;

        protected SqlExprBaseBinary( SqlExpr left, IAbstractExpr middle, SqlExpr right )
        {
            if( left == null ) throw new ArgumentNullException( "left" );
            if( middle == null ) throw new ArgumentNullException( "middle" );
            if( right == null ) throw new ArgumentNullException( "right" );
            _components = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, middle, right, SqlExprMultiToken<SqlTokenClosePar>.Empty );
        }

        protected SqlExprBaseBinary( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExprMultiToken<SqlTokenOpenPar> Opener { get { return (SqlExprMultiToken<SqlTokenOpenPar>)_components[0]; } }

        public SqlExpr Left { get { return (SqlExpr)_components[1]; } }

        protected IAbstractExpr Middle { get { return _components[2]; } }

        public SqlExpr Right { get { return (SqlExpr)_components[3]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        public SqlExprMultiToken<SqlTokenClosePar> Closer { get { return (SqlExprMultiToken<SqlTokenClosePar>)_components[4]; } }

        public virtual bool CanEnclose
        {
            get { return true; }
        }

        internal IAbstractExpr[] EncloseComponents( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            return CreateArray( openPar, _components, closePar );
        }

        public abstract ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar );

        public IEnumerable<IAbstractExpr> ComponentsWithoutParenthesis
        {
            get { throw new NotImplementedException(); }
        }
    }

}
