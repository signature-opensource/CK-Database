using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprSelectSpec : SqlExpr
    {
        readonly IAbstractExpr[] _components;
        readonly SqlExprSelectHeader _header;
        readonly SqlExprSelectColumnList _columns;
        readonly SqlExprSelectInto _into;
        readonly SqlExprSelectFrom _from;
        readonly SqlExprSelectWhere _where;
        readonly SqlExprSelectGroupBy _groupBy;

        public SqlExprSelectSpec( SqlExprSelectHeader header, SqlExprSelectColumnList columns, SqlExprSelectInto into = null, SqlExprSelectFrom from = null, SqlExprSelectWhere where = null, SqlExprSelectGroupBy groupBy = null )
        {
            var c = new List<IAbstractExpr>();
            _header = header;
            c.Add( header );
            _columns = columns;
            c.Add( columns );
            if( (_into = into) != null ) c.Add( into );
            if( (_from = from) != null ) c.Add( from );
            if( (_where = where) != null ) c.Add( where );
            if( (_groupBy = groupBy) != null ) c.Add( groupBy );
            _components = c.ToArray();
        }

        internal SqlExprSelectSpec( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
            _header = _components.OfType<SqlExprSelectHeader>().First();
            _columns = _components.OfType<SqlExprSelectColumnList>().First();
            _into = _components.OfType<SqlExprSelectInto>().FirstOrDefault();
            _from = _components.OfType<SqlExprSelectFrom>().FirstOrDefault();
            _where = _components.OfType<SqlExprSelectWhere>().FirstOrDefault();
            _groupBy = _components.OfType<SqlExprSelectGroupBy>().FirstOrDefault();
        }

        public SqlExprSelectHeader Header { get { return _header; } }

        public SqlExprSelectColumnList Columns { get { return _columns; } }
        
        public SqlExprSelectInto IntoClause { get { return _into; } }

        public SqlExprSelectFrom FromClause { get { return _from; } }

        public SqlExprSelectWhere WhereClause { get { return _where; } }

        public SqlExprSelectGroupBy GroupByClause { get { return _groupBy; } }

        public override IEnumerable<IAbstractExpr> Components
        {
            get { return _components; }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
