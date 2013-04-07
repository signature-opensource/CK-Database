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
    /// Captures SELECT [ ALL | DISTINCT ] [TOP ( expression ) [PERCENT] [ WITH TIES ] ] 
    /// </summary>
    public class SqlExprSelectHeader : SqlExpr
    {
        readonly IAbstractExpr[] _components;
        readonly SqlTokenIdentifier _allOrDistinct;
        readonly SqlTokenIdentifier _top;
        readonly SqlExpr _topExpression;
        readonly SqlTokenIdentifier _percent;
        readonly bool _withTies;

        public SqlExprSelectHeader( SqlTokenIdentifier select, SqlTokenIdentifier allOrDistinct = null, SqlTokenIdentifier top = null, SqlTokenTerminal openTopPar = null, SqlExpr topExpression = null, SqlTokenTerminal closeTopPar = null, SqlTokenIdentifier percent = null, SqlTokenIdentifier with = null, SqlTokenIdentifier ties = null )
        {
            List<IAbstractExpr> exprs = new List<IAbstractExpr>( 9 );
            if( select == null ) throw new ArgumentNullException( "select" );
            exprs.Add( select );
            if( (_allOrDistinct = allOrDistinct) != null ) exprs.Add( allOrDistinct );
            if( (_top = top) != null )
            {
                if( (_topExpression = topExpression) == null ) throw new ArgumentNullException( "topExpression" );
                // Automatically corrects any missing parenthesis.
                exprs.Add( top );
                exprs.Add( openTopPar ?? SqlTokenTerminal.OpenPar );
                exprs.Add( topExpression );
                exprs.Add( closeTopPar ?? SqlTokenTerminal.ClosePar );
            }
            if( (_percent = percent) != null ) exprs.Add( percent );
            if( with != null )
            {
                if( ties == null ) throw new ArgumentNullException( "ties" );
                _withTies = true;
                exprs.Add( with );
                exprs.Add( ties );
            }
            _components = exprs.ToArray();
        }

        internal SqlExprSelectHeader( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
            _allOrDistinct = (SqlTokenIdentifier)newComponents.FirstOrDefault( t => IsUnquotedIdentifier( t, "all", "distinct" ) );
            _top = (SqlTokenIdentifier)newComponents.FirstOrDefault( t => IsUnquotedIdentifier( t, "top" ) );
            _topExpression = (SqlExpr)newComponents.FirstOrDefault( t => t is SqlExpr );
            _percent = (SqlTokenIdentifier)newComponents.FirstOrDefault( t => IsUnquotedIdentifier( t, "percent" ) );
            _withTies = newComponents.Any( t => IsUnquotedIdentifier( t, "with" ) );
        }

        public SqlTokenIdentifier Select { get { return (SqlTokenIdentifier)_components[0]; } }
        public SqlTokenIdentifier AllOrDistinct { get { return _allOrDistinct; } }
        public SqlTokenIdentifier Top { get { return _top; } }
        public SqlExpr TopExpression { get { return _topExpression; } }
        public SqlTokenIdentifier Percent { get { return _percent; } }
        public bool WithTies { get { return _withTies; } }

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
