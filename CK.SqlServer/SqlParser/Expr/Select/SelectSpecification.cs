using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SelectSpecification : SqlExpr, ISelectSpecification
    {
        readonly SelectInto _into;
        readonly SelectFrom _from;
        readonly SelectWhere _where;
        readonly SelectGroupBy _groupBy;

        public SelectSpecification( SelectHeader header, SelectColumnList columns, SelectInto into = null, SelectFrom from = null, SelectWhere where = null, SelectGroupBy groupBy = null )
            : this( Build( SqlToken.EmptyOpenPar, header, columns, into, from, where, groupBy, SqlToken.EmptyClosePar ) )
        {
        }

        static ISqlItem[] Build( SqlExprMultiToken<SqlTokenOpenPar> opener, SelectHeader header, SelectColumnList columns, SelectInto into, SelectFrom from, SelectWhere where, SelectGroupBy groupBy, SqlExprMultiToken<SqlTokenClosePar> closer )
        {
            if( header == null ) throw new ArgumentNullException( "header" );
            if( columns == null ) throw new ArgumentNullException( "columns" );
            var c = new List<ISqlItem>();
            c.Add( opener );
            c.Add( header );
            c.Add( columns );
            if( into != null ) c.Add( into );
            if( from != null ) c.Add( from );
            if( where != null ) c.Add( where );
            if( groupBy != null ) c.Add( groupBy );
            c.Add( closer );
            return c.ToArray();
        }

        internal SelectSpecification( ISqlItem[] slots )
            : base( slots )
        {
            _into = Slots.OfType<SelectInto>().FirstOrDefault();
            _from = Slots.OfType<SelectFrom>().FirstOrDefault();
            _where = Slots.OfType<SelectWhere>().FirstOrDefault();
            _groupBy = Slots.OfType<SelectGroupBy>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the operator token type: it is <see cref="SqlTokenType.None"/> since this is an actual select specification and not a <see cref="SelectCombineOperator"/>.
        /// </summary>
        public SqlTokenType CombinationKind { get { return SqlTokenType.None; } }

        public SelectHeader Header { get { return (SelectHeader)Slots[1]; } }

        public SelectColumnList Columns { get { return (SelectColumnList)Slots[2]; } }
        
        public SelectInto IntoClause { get { return _into; } }

        public SelectFrom FromClause { get { return _from; } }

        public SelectWhere WhereClause { get { return _where; } }

        public SelectGroupBy GroupByClause { get { return _groupBy; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
