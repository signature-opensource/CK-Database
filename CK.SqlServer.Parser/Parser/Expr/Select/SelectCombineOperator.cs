using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Combination of two select through Union, Except or Intersect.
    /// </summary>
    public class SelectCombineOperator : SqlExpr, ISelectSpecification
    {
        public SelectCombineOperator( ISelectSpecification left, SqlTokenIdentifier exceptUnionOrIntercept, ISelectSpecification right, SelectOrderBy orderBy = null, SelectFor forPart = null )
            : this( Build( left, exceptUnionOrIntercept, null, right, orderBy, forPart ) )
        {
            if( !IsValidOperator( exceptUnionOrIntercept.TokenType ) ) throw new ArgumentException();
        }

        public SelectCombineOperator( ISelectSpecification left, SqlTokenIdentifier union, SqlTokenIdentifier all, ISelectSpecification right, SelectOrderBy orderBy = null, SelectFor forPart = null )
            : this( Build( left, union, all, right, orderBy, forPart ) )
        {
            if( union.TokenType == SqlTokenType.Union && all != null && !all.NameEquals( "all" ) ) throw new ArgumentException();
        }

        static ISqlItem[] Build( ISelectSpecification left, SqlTokenIdentifier op, SqlTokenIdentifier all, ISelectSpecification right, SelectOrderBy orderBy, SelectFor forPart )
        {
            Debug.Assert( left != null && op != null && right != null );
            ISqlItem o = all != null ? (ISqlItem)new SqlExprMultiToken<SqlTokenIdentifier>( op, all ) : op;
            return Build( SqlToken.EmptyOpenPar, left, o, right, orderBy, forPart, SqlToken.EmptyClosePar );
        }

        static ISqlItem[] Build( SqlExprMultiToken<SqlTokenOpenPar> opener, ISelectSpecification left, ISqlItem op, ISelectSpecification right, SelectOrderBy orderBy, SelectFor forPart, SqlExprMultiToken<SqlTokenClosePar> closer )
        {
            Debug.Assert( opener != null && left != null && op != null && right != null && closer != null );
            if( orderBy != null )
            {
                if( forPart != null )
                {
                    return CreateArray( opener, left, op, right, orderBy, forPart, closer );
                }
                return CreateArray( opener, left, op, right, orderBy, closer );
            }
            else if( forPart != null )
            {
                return CreateArray( opener, left, op, right, forPart, closer );
            }
            return CreateArray( opener, left, op, right, closer );
        }

        internal SelectCombineOperator( ISqlItem[] slots )
            : base( slots )
        {
            Debug.Assert( Slots.Length >= 5 && Slots.Length <= 7 );
            Debug.Assert( Slots[1] is ISelectSpecification && Slots[3] is ISelectSpecification );
            Debug.Assert( Slots.Length != 6 || (Slots[4] is SelectOrderBy || Slots[4] is SelectFor) );
            Debug.Assert( Slots.Length < 7 || (Slots[4] is SelectOrderBy && Slots[5] is SelectFor) );
            Debug.Assert( IsValidOperator( OperatorTok.TokenType ) 
                                && (UnionAll == null
                                    || (UnionAll != null
                                        && UnionAll[0].TokenType == SqlTokenType.Union
                                        && UnionAll[1] is SqlTokenIdentifier
                                        && ((SqlTokenIdentifier)UnionAll[1]).NameEquals( "all" ))) );
        }

        static public bool IsValidOperator( SqlTokenType op )
        {
            return op == SqlTokenType.Union || op == SqlTokenType.Except || op == SqlTokenType.Intersect;
        }

        public SelectColumnList Columns { get { return LeftSelect.Columns; } }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        public ISelectSpecification LeftSelect { get { return (ISelectSpecification)Slots[1]; } }

        SqlExprMultiToken<SqlToken> UnionAll { get { return Slots[2] as SqlExprMultiToken<SqlToken>; } }

        SqlTokenIdentifier OperatorTok { get { return Slots[2] is SqlTokenIdentifier ? (SqlTokenIdentifier)Slots[2] : ((SqlExprMultiToken<SqlTokenIdentifier>)Slots[2])[0]; } }

        /// <summary>
        /// Gets the operator token type: it can be: <see cref="SqlTokenType.Union"/>, <see cref="SqlTokenType.Except"/>, <see cref="SqlTokenType.Intersect"/>.
        /// </summary>
        public SqlTokenType CombinationKind { get { return OperatorTok.TokenType; } }

        public ISqlItem Operator { get { return Slots[2]; } }

        public bool IsUnionDistinct { get { return UnionAll == null && OperatorTok.TokenType == SqlTokenType.Union; } }

        public bool IsUnionAll { get { return UnionAll != null; } }

        public bool IsExcept { get { return OperatorTok.TokenType == SqlTokenType.Except; } }

        public bool IsIntersect { get { return OperatorTok.TokenType == SqlTokenType.Intersect; } }

        public SqlExpr Right { get { return (SqlExpr)Slots[3]; } }

        public ISelectSpecification RightSelect { get { return (ISelectSpecification)Slots[3]; } }

        public SelectOrderBy OrderByClause { get { return Slots.Length == 6 ? Slots[4] as SelectOrderBy : (Slots.Length == 7 ? (SelectOrderBy)Slots[4] : null); } }

        public SelectFor ForClause { get { return Slots.Length == 6 ? Slots[4] as SelectFor : (Slots.Length == 7 ? (SelectFor)Slots[5] : null); } }

        public ISelectSpecification SetExtensions( SelectOrderBy orderBy, SelectFor forPart )
        {
            SelectOrderBy o = OrderByClause;
            SelectFor f = ForClause;
            if( orderBy == o && forPart == f ) return this;
            return new SelectCombineOperator( Build( Opener, LeftSelect, Operator, RightSelect, orderBy, forPart, Closer ) );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
