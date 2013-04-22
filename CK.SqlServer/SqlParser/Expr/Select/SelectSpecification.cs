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
        // Extensions allowed (under certain conditions) in a select specification.
        // Order by can be specified if Top is specified in the header.
        readonly SelectOrderBy _orderBy;
        // For xml can appear in a sub query.
        readonly SelectFor _forPart;

        public SelectSpecification( SelectHeader header, SelectColumnList columns, SelectInto into = null, SelectFrom from = null, SelectWhere where = null, SelectGroupBy groupBy = null, SelectOrderBy orderBy = null, SelectFor forPart = null )
            : this( Build( SqlToken.EmptyOpenPar, header, columns, into, from, where, groupBy, orderBy, forPart, SqlToken.EmptyClosePar ) )
        {
        }

        static ISqlItem[] Build( SqlExprMultiToken<SqlTokenOpenPar> opener, SelectHeader header, SelectColumnList columns, SelectInto into, SelectFrom from, SelectWhere where, SelectGroupBy groupBy, SelectOrderBy orderBy, SelectFor forPart, SqlExprMultiToken<SqlTokenClosePar> closer )
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
            if( orderBy != null ) c.Add( orderBy );
            if( forPart != null ) c.Add( forPart );
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
            _orderBy = Slots.OfType<SelectOrderBy>().FirstOrDefault();
            _forPart = Slots.OfType<SelectFor>().FirstOrDefault();
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

        public SelectOrderBy OrderByClause { get { return _orderBy; } }
        
        public SelectFor ForClause { get { return _forPart; } }

        public bool ExtractExtensions( out SelectOrderBy orderBy, out SelectFor forPart, out ISelectSpecification cleaned )
        {
            cleaned = null;
            orderBy = _orderBy;
            forPart = _forPart;
            if( orderBy == null && forPart == null ) return false;
            cleaned = new SelectSpecification( Build( Opener, Header, Columns, IntoClause, FromClause, WhereClause, GroupByClause, OrderByClause, ForClause, Closer ) );
            return true;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
