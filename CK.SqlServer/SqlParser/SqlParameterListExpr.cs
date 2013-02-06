using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlParameterListExpr : SqlExpr, IReadOnlyList<SqlParameterExpr>
    {
        IReadOnlyList<SqlParameterExpr> _parameters;

        public SqlParameterListExpr( SourceLocation location, IEnumerable<SqlParameterExpr> parameters )
            : base( location )
        {
            if( parameters == null ) throw new ArgumentNullException( "parameters" );
            _parameters = parameters is IReadOnlyList<SqlParameterExpr> ? (IReadOnlyList<SqlParameterExpr>)parameters : parameters.ToReadOnlyList();
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            return String.Join( ", ", _parameters.Select( p => p.ToString() ) );
        }

        #region IReadOnlyList<SqlParameterExpr> Members

        public int IndexOf( object item )
        {
            return _parameters.IndexOf( item );
        }

        public SqlParameterExpr this[int index]
        {
            get { return _parameters[index]; }
        }

        public bool Contains( object item )
        {
            return _parameters.Contains( item );
        }

        public int Count
        {
            get { return _parameters.Count; }
        }

        public IEnumerator<SqlParameterExpr> GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

}
