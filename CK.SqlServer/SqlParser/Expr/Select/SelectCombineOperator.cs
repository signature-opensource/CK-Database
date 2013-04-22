using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Combination of two select through Union, Except or Intersect.
    /// </summary>
    public class SelectCombineOperator : SqlExpr, ISelectSpecification
    {
        public SelectCombineOperator( ISelectSpecification left, SqlTokenTerminal exceptUnionOrIntercept, ISelectSpecification right, SelectOrderBy orderBy = null, SelectFor forPart = null )
            : this( Build( left, exceptUnionOrIntercept, null, right, orderBy, forPart ) )
        {
            if( !IsValidOperator( exceptUnionOrIntercept.TokenType ) ) throw new ArgumentException();
        }

        public SelectCombineOperator( ISelectSpecification left, SqlTokenTerminal union, SqlTokenIdentifier all, ISelectSpecification right, SelectOrderBy orderBy = null, SelectFor forPart = null )
            : this( Build( left, union, all, right, orderBy, forPart ) )
        {
            if( union.TokenType != SqlTokenType.Union || !all.NameEquals( "all" ) ) throw new ArgumentException();
        }

        static ISqlItem[] Build( ISelectSpecification left, SqlTokenTerminal op, SqlTokenIdentifier all, ISelectSpecification right, SelectOrderBy orderBy, SelectFor forPart )
        {
            Debug.Assert( left != null && op != null && right != null );
            ISqlItem o = all != null ? (ISqlItem)new SqlExprMultiToken<SqlToken>( op, all ) : op;
            if( orderBy != null )
            {
                if( forPart != null )
                {
                    return CreateArray( SqlToken.EmptyOpenPar, left, o, right, orderBy, forPart, SqlToken.EmptyClosePar );
                }
                return CreateArray( SqlToken.EmptyOpenPar, left, o, right, orderBy, SqlToken.EmptyClosePar );
            }
            else if( forPart != null )
            {
                return CreateArray( SqlToken.EmptyOpenPar, left, o, right, forPart, SqlToken.EmptyClosePar );
            }
            return CreateArray( SqlToken.EmptyOpenPar, left, o, right, SqlToken.EmptyClosePar );
        }

        internal SelectCombineOperator( ISqlItem[] slots )
            : base( slots )
        {
            Debug.Assert( Slots.Length >= 5 && Slots.Length <= 7 );
            Debug.Assert( Slots[1] is ISelectSpecification && Slots[3] is ISelectSpecification );
            Debug.Assert( Slots.Length != 5 || (Slots[4] is SelectOrderBy || Slots[4] is SelectFor) );
            Debug.Assert( Slots.Length < 7 || (Slots[4] is SelectOrderBy && Slots[5] is SelectFor) );
            Debug.Assert( IsValidOperator( OperatorToken.TokenType ) 
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

        SqlTokenTerminal OperatorToken { get { return Slots[2] is SqlTokenTerminal ? (SqlTokenTerminal)Slots[2] : (SqlTokenTerminal)((SqlExprMultiToken<SqlToken>)Slots[2])[0]; } }

        /// <summary>
        /// Gets the operator token type: it can be: <see cref="SqlTokenType.Union"/>, <see cref="SqlTokenType.Except"/>, <see cref="SqlTokenType.Intersect"/>.
        /// </summary>
        public SqlTokenType CombinationKind { get { return OperatorToken.TokenType; } }

        public ISqlItem Operator { get { return Slots[2]; } }

        public bool IsUnionDistinct { get { return UnionAll == null && OperatorToken.TokenType == SqlTokenType.Union; } }

        public bool IsUnionAll { get { return UnionAll != null; } }

        public bool IsExcept { get { return OperatorToken.TokenType == SqlTokenType.Except; } }

        public bool IsIntersect { get { return OperatorToken.TokenType == SqlTokenType.Intersect; } }

        public SqlExpr Right { get { return (SqlExpr)Slots[3]; } }

        public ISelectSpecification RightSelect { get { return (ISelectSpecification)Slots[3]; } }

        public bool HasExtensions { get { return Slots.Length > 5; } }

        public bool ExtractExtensions( out SelectOrderBy orderBy, out SelectFor forPart, out ISelectSpecification cleaned )
        {
            orderBy = null;
            forPart = null;
            cleaned = this;
            if( !HasExtensions ) return false;
            if( Slots.Length == 6 )
                if( Slots[4] is SelectFor ) forPart = (SelectFor)Slots[4];
                else orderBy = (SelectOrderBy)Slots[4];
            else
            {
                orderBy = (SelectOrderBy)Slots[4];
                forPart = (SelectFor)Slots[5];
            }
            cleaned = new SelectCombineOperator( new ISqlItem[5] { Slots[0], Slots[1], Slots[2], Slots[3], Closer } );
            return true;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
