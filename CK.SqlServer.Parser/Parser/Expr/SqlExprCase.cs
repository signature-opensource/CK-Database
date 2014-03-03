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
    /// 
    /// </summary>
    public class SqlExprCase : SqlExpr
    {
        public SqlExprCase( SqlTokenIdentifier caseToken, SqlExpr expr, SqlExprCaseWhenSelector whenSelector, SqlTokenIdentifier elseToken, SqlExpr elseExpr, SqlTokenIdentifier endToken )
            : this( Build( caseToken, expr, whenSelector, elseToken, elseExpr, endToken ) )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier caseToken, SqlExpr expr, SqlExprCaseWhenSelector whenSelector, SqlTokenIdentifier elseToken, SqlExpr elseExpr, SqlTokenIdentifier endToken )
        {
            if( (elseToken == null) != (elseExpr == null) )
            {
                throw new ArgumentException( "Else token and Else expression must be both defined or both null." );
            }
            if( whenSelector == null ) throw new ArgumentNullException( "whenSelector" );
            if( endToken == null ) throw new ArgumentNullException( "endToken" );

            if( expr != null )
            {
                return elseToken != null
                                ? CreateArray( SqlToken.EmptyOpenPar, caseToken, expr, whenSelector, elseToken, elseExpr, endToken, SqlToken.EmptyClosePar )
                                : CreateArray( SqlToken.EmptyOpenPar, caseToken, expr, whenSelector, endToken, SqlToken.EmptyClosePar );
            }
            else
            {
                return elseToken != null
                                ? CreateArray( SqlToken.EmptyOpenPar, caseToken, whenSelector, elseToken, elseExpr, endToken, SqlToken.EmptyClosePar )
                                : CreateArray( SqlToken.EmptyOpenPar, caseToken, whenSelector, endToken, SqlToken.EmptyClosePar );
            }
        }

        internal SqlExprCase( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        /// <summary>
        /// Gets whether this is a simple case: "case Expression when V0 then C0 when V1 then C1 end".
        /// </summary>
        public bool IsSimpleCase { get { return Slots.Length == 8 || Slots.Length == 6; } }

        /// <summary>
        /// Gets whether this is a search case: "case when E0 = V0 then C0 when E1 = V1 then C1 end". 
        /// </summary>
        public bool IsSearchCase { get { return Slots.Length == 7 || Slots.Length == 5; } }

        /// <summary>
        /// Gets the first case token.
        /// </summary>
        public SqlTokenIdentifier CaseT { get { return (SqlTokenIdentifier)Slots[1]; } }
        
        /// <summary>
        /// Gets the simple case expression. Null if <see cref="IsSearchCase"/> is true.
        /// </summary>
        public SqlExpr Expression { get { return IsSimpleCase ? (SqlExpr)Slots[2] : null; } }

        /// <summary>
        /// Gets the {when E0 = V0 then C0}+ selector.
        /// </summary>
        public SqlExprCaseWhenSelector WhenSelector { get { return (SqlExprCaseWhenSelector)Slots[IsSimpleCase ? 3 : 2]; } }

        /// <summary>
        /// Gets whether the else clause exists.
        /// </summary>
        public bool HasElse { get { return Slots.Length >= 7; } }

        /// <summary>
        /// Gets the else token if it exists.
        /// </summary>
        public SqlTokenIdentifier ElseT { get { return HasElse ? (SqlTokenIdentifier)Slots[Slots.Length - 4] : null; } }

        /// <summary>
        /// Gets the else expression if it exists.
        /// </summary>
        public SqlExpr ElseExpression { get { return HasElse ? (SqlExpr)Slots[Slots.Length - 3] : null; } }

        /// <summary>
        /// Gets the end token.
        /// </summary>
        public SqlTokenIdentifier EndT { get { return (SqlTokenIdentifier)Slots[Slots.Length - 2]; } }


        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
