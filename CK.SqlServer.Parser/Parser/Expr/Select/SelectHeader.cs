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
    /// Captures SELECT [ ALL | DISTINCT ] [TOP ( expression ) [PERCENT] [ WITH TIES ] ] 
    /// </summary>
    public class SelectHeader : SqlNoExpr
    {
        readonly SqlTokenIdentifier _allOrDistinct;
        readonly SqlTokenIdentifier _top;
        readonly SqlExpr _topExpression;
        readonly SqlTokenIdentifier _percent;
        readonly bool _withTies;

        public SelectHeader( SqlTokenIdentifier select, SqlTokenIdentifier allOrDistinct = null, SqlTokenIdentifier top = null, SqlExpr topExpression = null, SqlTokenIdentifier percent = null, SqlTokenIdentifier with = null, SqlTokenIdentifier ties = null )
            : this( Build( select, allOrDistinct, top, topExpression, percent, with, ties ) )
        {
        }

        internal SelectHeader( ISqlItem[] items )
            : base( items )
        {
            _allOrDistinct = (SqlTokenIdentifier)Slots.FirstOrDefault( t => SqlToken.IsUnquotedIdentifier( t, "all", "distinct" ) );
            _top = (SqlTokenIdentifier)Slots.FirstOrDefault( t => SqlToken.IsUnquotedIdentifier( t, "top" ) );
            _topExpression = (SqlExpr)Slots.FirstOrDefault( t => t is SqlExpr );
            _percent = (SqlTokenIdentifier)Slots.FirstOrDefault( t => SqlToken.IsUnquotedIdentifier( t, "percent" ) );
            _withTies = Slots.Any( t => SqlToken.IsUnquotedIdentifier( t, "with" ) );
        }

        static ISqlItem[] Build( SqlTokenIdentifier select, SqlTokenIdentifier allOrDistinct, SqlTokenIdentifier top, SqlExpr topExpression, SqlTokenIdentifier percent, SqlTokenIdentifier with, SqlTokenIdentifier ties )
        {
            List<ISqlItem> exprs = new List<ISqlItem>( 9 );
            if( select == null ) throw new ArgumentNullException( "select" );
            exprs.Add( select );
            if( allOrDistinct != null ) exprs.Add( allOrDistinct );
            if( top != null )
            {
                if( topExpression == null ) throw new ArgumentNullException( "topExpression" );
                exprs.Add( top );
                exprs.Add( topExpression );
            }
            if( percent != null ) exprs.Add( percent );
            if( with != null )
            {
                if( ties == null ) throw new ArgumentNullException( "ties" );
                exprs.Add( with );
                exprs.Add( ties );
            }
            return exprs.ToArray();
        }

        public SqlTokenIdentifier Select { get { return (SqlTokenIdentifier)Slots[0]; } }
        public SqlTokenIdentifier AllOrDistinct { get { return _allOrDistinct; } }
        public SqlTokenIdentifier Top { get { return _top; } }
        public SqlExpr TopExpression { get { return _topExpression; } }
        public SqlTokenIdentifier Percent { get { return _percent; } }
        public bool WithTies { get { return _withTies; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
