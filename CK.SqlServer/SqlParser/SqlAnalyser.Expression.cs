using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.SqlServer
{
        public partial class SqlAnalyser
        {

            bool IsExpression( out SqlExpr e, int rightBindingPower, bool expected )
            {
                e = null;
                if( R.IsErrorOrEndOfInput || !IsExpressionNud( ref e ) )
                {
                    if( expected && !R.IsError ) R.SetCurrentError( "Expected expression." );
                    return false;
                }
                // Not (as a left denotation) is the same as a between or a like (since it introduces them).
                // This could have been handled with a left and right binding power instead of only one power per operator.
                while( !R.IsErrorOrEndOfInput 
                        && ((R.Current.TokenType == SqlTokenType.Not && SqlTokenizer.PrecedenceLevel( SqlTokenType.OpNotRightLevel) > rightBindingPower ) 
                            || 
                            (R.Current.TokenType != SqlTokenType.Not && R.CurrentPrecedenceLevel > rightBindingPower)) )
                {
                    if( !ExpressionCombineLed( ref e ) ) break;
                }
                return !R.IsError;
            }

            /// <summary>
            /// Handles NUD (NUll left Denotation): the token has nothing to its left (it is a prefix).
            /// </summary>
            /// <param name="e"></param>
            /// <returns></returns>
            bool IsExpressionNud( ref SqlExpr e )
            {
                Debug.Assert( e == null );
                Debug.Assert( !R.IsErrorOrEndOfInput );
                // Handles strings and numbers.
                if( (R.Current.TokenType & SqlTokenType.LitteralMask) != 0 )
                {
                    e = new SqlExprLiteral( R.Read<SqlTokenBaseLiteral>() );
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.IdentifierReservedKeyword )
                {
                    SqlTokenIdentifier id = R.Read<SqlTokenIdentifier>();
                    if( id.NameEquals( "null" ) ) e = new SqlExprNull( id );
                    else e = new SqlExprIdentifier( id );
                    return true;
                }
                if( (R.Current.TokenType & SqlTokenType.IsIdentifier) != 0 )
                {
                    // This shortcuts the nud/led mechanism by directly handling 
                    // the . as a top precedence level operator.
                    return IsMonoOrMultiIdentifier( out e );
                }
                if( R.Current.TokenType == SqlTokenType.Minus 
                    || R.Current.TokenType == SqlTokenType.Plus 
                    || R.Current.TokenType == SqlTokenType.BitwiseNot
                    || R.Current.TokenType == SqlTokenType.Not )
                {
                    int precedenceLevel = R.CurrentPrecedenceLevel;
                    SqlToken op = R.Read<SqlToken>();
                    SqlExpr right;
                    if( !IsExpression( out right, precedenceLevel, true ) ) return false;
                    e = new SqlExprUnaryOperator( op, right );
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.OpenPar )
                {
                    SqlTokenOpenPar openPar = R.Read<SqlTokenOpenPar>();
                    //if( IsSelect
                    if( IsGenericBlock( out e, openPar, false ) ) return true;
                    return R.SetCurrentError( "Expected )." );
                }
                return false;
            }

            /// <summary>
            /// Combines the LED (LEft Denotation): The token has something to left (postfix or infix).
            /// </summary>
            bool ExpressionCombineLed( ref SqlExpr left )
            {
                int precedenceLevel = R.CurrentPrecedenceLevel;
                if( R.Current.TokenType == SqlTokenType.OpenPar )
                {
                    SqlExprList parenthesis;
                    if( !IsGenericBlockList( out parenthesis, true ) ) return false;
                    left = new SqlExprKoCall( left, parenthesis );
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.Comma )
                {
                    Debug.Assert( !(left is SqlExprList) );
                    var items = new List<IAbstractExpr>();
                    items.Add( left );
                    items.Add( R.Read<SqlTokenTerminal>() );
                    for( ; ; )
                    {
                        SqlExpr next;
                        if( !IsExpression( out next, SqlTokenizer.PrecedenceLevel( SqlTokenType.Comma ), true ) ) return false;
                        items.Add( next );
                        SqlTokenTerminal comma;
                        if( !R.IsToken<SqlTokenTerminal>( out comma, SqlTokenType.Comma, false ) ) break;
                        items.Add( comma );
                    }
                    left = new SqlExprList( items );
                }
                if( (R.Current.TokenType & SqlTokenType.IsAssignOperator) != 0 )
                {
                    if( !(left is ISqlIdentifier) ) return false;
                    else
                    {
                        SqlTokenTerminal assign = R.Read<SqlTokenTerminal>();
                        SqlExpr right;
                        R.AssignmentContext = false;
                        try
                        {
                            if( !IsExpression( out right, precedenceLevel, true ) ) return false;
                            left = new SqlExprAssign( (ISqlIdentifier)left, assign, right );
                        }
                        finally
                        {
                            R.AssignmentContext = true;
                        }
                    }
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.Is )
                {
                    SqlTokenTerminal isToken = R.Read<SqlTokenTerminal>();
                    SqlTokenTerminal notToken;
                    R.IsToken( out notToken, SqlTokenType.Not, false );
                    SqlTokenIdentifier nullToken;
                    if( !R.IsUnquotedKeyword( out nullToken, "null", true ) ) return false;
                    left = new SqlExprIsNull( left, isToken, notToken, nullToken );
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.Not )
                {
                    SqlTokenTerminal notToken = R.Read<SqlTokenTerminal>();
                    if( R.Current.TokenType == SqlTokenType.Like ) return IsExprLike( ref left, notToken );
                    if( R.Current.TokenType == SqlTokenType.Between ) return IsExprBetween( ref left, notToken );
                    if( R.Current.TokenType == SqlTokenType.In ) return IsExprIn( ref left, notToken );
                    return R.SetCurrentError( "Like, between or in expected." );
                }
                if( R.Current.TokenType == SqlTokenType.Like ) return IsExprLike( ref left, null );
                if( R.Current.TokenType == SqlTokenType.Between ) return IsExprBetween( ref left, null );
                if( R.Current.TokenType == SqlTokenType.In ) return IsExprIn( ref left, null );
                if( SqlExprBinaryOperator.IsValidOperator( R.Current.TokenType ) )
                {
                    SqlTokenTerminal cmp = R.Read<SqlTokenTerminal>();
                    SqlExpr right;
                    if( !IsExpression( out right, precedenceLevel, true ) ) return false;
                    left = new SqlExprBinaryOperator( left, cmp, right );
                    return true;
                }
                return false;
            }

            bool IsExprBetween( ref SqlExpr left, SqlTokenTerminal notToken )
            {
                Debug.Assert( R.Current.TokenType == SqlTokenType.Between );
                SqlTokenTerminal betweenToken = R.Read<SqlTokenTerminal>();
                SqlExpr start;
                if( !IsExpression( out start, SqlTokenizer.PrecedenceLevel( SqlTokenType.OpComparisonLevel ), true ) ) return false;
                SqlTokenTerminal andToken;
                if( !R.IsToken( out andToken, SqlTokenType.And, true ) ) return false;
                SqlExpr stop;
                if( !IsExpression( out stop, SqlTokenizer.PrecedenceLevel( SqlTokenType.OpComparisonLevel ), true ) ) return false;

                left = new SqlExprBetween( left, notToken, betweenToken, start, andToken, stop );
                return true;
            }

            bool IsExprLike( ref SqlExpr left, SqlTokenTerminal notToken )
            {
                Debug.Assert( R.Current.TokenType == SqlTokenType.Like );
                SqlTokenTerminal likeToken = R.Read<SqlTokenTerminal>();
                SqlExpr pattern;
                if( !IsExpression( out pattern, SqlTokenizer.PrecedenceLevel( SqlTokenType.OpComparisonLevel ), true ) ) return false;
                SqlTokenIdentifier escapeToken;
                SqlTokenLiteralString escapeChar = null;
                if( R.IsUnquotedKeyword( out escapeToken, "escape", false ) )
                {
                    if( !R.IsToken( out escapeChar, true ) ) return false;
                }
                left = new SqlExprLike( left, notToken, likeToken, pattern, escapeToken, escapeChar );
                return true;
            }

            bool IsExprIn( ref SqlExpr left, SqlTokenTerminal notToken )
            {
                Debug.Assert( R.Current.TokenType == SqlTokenType.In );
                SqlTokenTerminal inToken = R.Read<SqlTokenTerminal>();
                SqlExprList values;
                if( !IsGenericBlockList( out values, true ) ) return false;
                left = new SqlExprIn( left, notToken, inToken, values );
                return true;
            }

            /// <summary>
            /// Reads a comma separated list of expressions.
            /// </summary>
            /// <param name="e">The list.</param>
            /// <param name="expectParenthesis">True to set an error if the current token is not an opening parenthesis.</param>
            /// <returns>True on success.</returns>
            bool IsGenericBlockList( out SqlExprList e, bool expectParenthesis )
            {
                e = null;
                SqlTokenOpenPar openPar;
                SqlTokenClosePar closePar;
                List<IAbstractExpr> items;
                if( !IsCommaList<SqlExpr>( out openPar, out items, out closePar, expectParenthesis, MatchGenericBlockInList ) ) return false;
                if( items.Count == 1 && items[0] is SqlExprList )
                {
                    e = (SqlExprList)items[0];
                    if( openPar != null ) e = (SqlExprList)e.Enclose( openPar, closePar );
                }
                else e = openPar != null ? new SqlExprList( openPar, items, closePar ) : new SqlExprList( items );
                return true;
            }

            bool MatchGenericBlockInList( out SqlExpr e, bool expected )
            {
                return IsGenericBlock( out e, SqlExpr.IsCommaOrCloseParenthesisOrTerminator, expected );
            }

            /// <summary>
            /// Reads a <see cref="SqlExprGenericBlock"/> (a list of expressions or tokens) up to a specific token.
            /// </summary>
            /// <param name="block">Read block.</param>
            /// <param name="closer">Predicate that detects the stopper (will NOT be added to the expression).</param>
            /// <param name="expectAtLeastOne">True to set an error if the block is empty (no expressions in it).</param>
            /// <returns>True if a block has sucessfully been found.</returns>
            bool IsGenericBlock( out SqlExpr block, Predicate<SqlToken> stopper, bool expectAtLeastOne )
            {
                if( stopper == null ) throw new ArgumentNullException( "stopper" );
                return IsGenericBlockInternal( out block, null, stopper, expectAtLeastOne );
            }

            /// <summary>
            /// Reads a <see cref="SqlExprGenericBlock"/> (a list of expressions or tokens) enclosed in parenthesis: the stopper is the closing parenthesis. 
            /// </summary>
            /// <param name="block">Read expression.</param>
            /// <param name="openPar">Opening parenthesis (will be the very first token).</param>
            /// <param name="expectAtLeastOne">True to set an error if the block is empty (no expressions in it).</param>
            /// <returns>True if a block has sucessfully been found.</returns>
            bool IsGenericBlock( out SqlExpr block, SqlTokenOpenPar openPar, bool expectAtLeastOne )
            {
                if( openPar == null ) throw new ArgumentNullException( "opener" );
                return IsGenericBlockInternal( out block, openPar,  t => t is SqlTokenClosePar, expectAtLeastOne );
            }

            bool IsGenericBlockInternal( out SqlExpr block, SqlTokenOpenPar openPar, Predicate<SqlToken> closer, bool setErrorIfEmpty )
            {
                block = null;
                List<IAbstractExpr> exprs = new List<IAbstractExpr>();
                while( !(R.IsErrorOrEndOfInput || closer( R.Current )) )
                {
                    // If it is not the closer nor the end, it must be a valid expression: we expect it.
                    SqlExpr e;
                    if( !IsExpression( out e, SqlTokenizer.PrecedenceLevel( SqlTokenType.Comma ), expected: true ) ) break;
                    exprs.Add( e );
                }
                // If we expect something and nothing was found and no error was previously set, we set an error.
                if( setErrorIfEmpty && exprs.Count == 0 && !R.IsError ) return R.SetCurrentError( "Expected expression." );
                // If no error occured, the block is built:
                // - if the opener is not null, with the the given opener and the found closer.
                // - if the opener is null, without any opener/closer and the closer is not consumed.
                if( !R.IsErrorOrEndOfInput )
                {
                    Debug.Assert( closer( R.Current ), "We are on the Closer token." );
                    if( openPar != null )
                    {
                        // If an opener exists, we always create the block.
                        Debug.Assert( R.Current is SqlTokenClosePar );
                        SqlTokenClosePar closePar = R.Read<SqlTokenClosePar>();
                        if( exprs.Count == 1 )
                        {
                            ISqlExprEnclosable enc = exprs[0] as ISqlExprEnclosable;
                            if( enc != null && enc.CanEnclose )
                            {
                                block = (SqlExpr)enc.Enclose( openPar, closePar );
                                return true;
                            }
                        }
                        block = new SqlExprGenericBlock( openPar, exprs, closePar );
                        return true;
                    }
                    // When no opener/closer exist and the block is empty, we do not instanciate it.
                    if( exprs.Count > 0 )
                    {
                        if( exprs.Count == 1 && exprs[0] is SqlExpr) block = (SqlExpr)exprs[0];
                        else block = new SqlExprGenericBlock( exprs );
                    }
                    return true;
                }
                // An error occured or the end of the input has been reached: closer was not found.
                // We let the block null... (we may here build a block with exprs and a kind of SqlExprSyntaxError at the end).
                return false;
            }

        }

    
}

