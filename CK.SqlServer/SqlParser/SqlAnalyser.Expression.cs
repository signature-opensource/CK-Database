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
                        && ((R.Current.TokenType == SqlTokenType.Not && SqlTokenizer.PrecedenceLevel( SqlTokenType.OpNotRightLevel ) > rightBindingPower)
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
                    SelectSpecification select;
                    if( IsSelectSpecification( out select, true, false ) )
                    {
                        e = select;
                        return true;
                    }
                    if( R.IsError ) return false; 
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
                if( R.Current.TokenType == SqlTokenType.Mult )
                {
                    e = new SqlExprIdentifier( StarTransformer.FromMultToken( R.Read<SqlToken>() ) );
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.OpenPar )
                {
                    SqlTokenOpenPar openPar = R.Read<SqlTokenOpenPar>();
                    if( IsExpressionOrRawList( out e, openPar, false ) ) return true;
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
                    SqlExprCommaList parenthesis;
                    if( !IsCommaList( out parenthesis, true ) ) return false;
                    left = new SqlExprKoCall( left, parenthesis );
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.Comma )
                {
                    Debug.Assert( !(left is SqlExprCommaList) );
                    var items = new List<ISqlItem>();
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
                    left = new SqlExprCommaList( items );
                }
                if( (R.Current.TokenType & SqlTokenType.IsAssignOperator) != 0 )
                {
                    if( !(left is ISqlIdentifier) ) return R.SetCurrentError( "Unexpected '='. Assignement must follow an identifier." );
                    else
                    {
                        SqlTokenTerminal assign = R.Read<SqlTokenTerminal>();
                        SqlExpr right;
                        using( R.SetAssignmentContext( false ) )
                        {
                            if( !IsExpression( out right, precedenceLevel, true ) ) return false;
                            left = new SqlExprAssign( (ISqlIdentifier)left, assign, right );
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
                if( (R.Current.TokenType & SqlTokenType.IsSelectPart) != 0 )
                {
                    ISelectSpecification lSelect = left as ISelectSpecification;
                    if( lSelect == null ) return false;
                    SqlTokenTerminal op = R.Read<SqlTokenTerminal>();
                    if( op.TokenType == SqlTokenType.Order )
                    {
                        SqlTokenIdentifier by;
                        SqlExpr content;
                        if( !R.IsUnquotedKeyword( out by, "by", true ) ) return false;
                        if( !IsExpressionOrRawList( out content, SelectPartStopper, true ) ) return false;
                        left = new SelectOrderBy( lSelect, op, by, content );
                        return true;
                    }
                    if( op.TokenType == SqlTokenType.For )
                    {
                        SqlExpr content;
                        if( !IsExpressionOrRawList( out content, SelectPartStopper, true ) ) return false;
                        left = new SelectFor( lSelect, op, content );
                        return true;
                    }
                    Debug.Assert( SelectCombineOperator.IsValidOperator( op.TokenType ) );
                    SqlTokenIdentifier all = null;
                    if( op.TokenType == SqlTokenType.Union ) R.IsUnquotedKeyword( out all, "all", false );
                    SqlExpr right;
                    if( !IsExpression( out right, precedenceLevel, true ) ) return false;
                    ISelectSpecification rSelect = right as ISelectSpecification;
                    if( rSelect == null ) return R.SetCurrentError( "Expected select expression." );
                    left = new SelectCombineOperator( lSelect, op, all, rSelect );
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
                SqlExprCommaList values;
                if( !IsCommaList( out values, true ) ) return false;
                left = new SqlExprIn( left, notToken, inToken, values );
                return true;
            }


            /// <summary>
            /// Reads one and only one expression (comma stops it).
            /// </summary>
            /// <param name="e">The read expression.</param>
            /// <param name="parenthesisRequired">True to raise an error if no opening parenthesis exists.</param>
            /// <returns>True on success.</returns>
            bool IsOneExpression( out SqlExpr e, bool parenthesisRequired )
            {
                e = null;
                if( parenthesisRequired && R.Current.TokenType != SqlTokenType.OpenPar )
                {
                    return R.SetCurrentError( "Expected expression between parenthesis." );
                }
                return IsExpression( out e, SqlTokenizer.PrecedenceLevel( SqlTokenType.Comma ), true );
            }
            
            /// <summary>
            /// Reads a comma separated list of expressions.
            /// </summary>
            /// <param name="e">The list.</param>
            /// <param name="expectParenthesis">True to set an error if the current token is not an opening parenthesis.</param>
            /// <returns>True on success.</returns>
            bool IsCommaList( out SqlExprCommaList e, bool expectParenthesis )
            {
                e = null;
                SqlTokenOpenPar openPar;
                SqlTokenClosePar closePar;
                List<ISqlItem> items;
                if( !IsCommaList<SqlExpr>( out openPar, out items, out closePar, expectParenthesis, MatchInList ) ) return false;
                if( items.Count == 1 && items[0] is SqlExprCommaList )
                {
                    e = (SqlExprCommaList)items[0];
                    if( openPar != null ) e.MutableEnclose( openPar, closePar );
                }
                else e = openPar != null ? new SqlExprCommaList( openPar, items, closePar ) : new SqlExprCommaList( items );
                return true;
            }

            bool MatchInList( out SqlExpr e, bool expected )
            {
                return IsExpressionOrRawList( out e, SqlToken.IsCommaOrCloseParenthesisOrTerminator, expected );
            }

            /// <summary>
            /// Reads a potential <see cref="SqlExprRawItemList"/> (a list of expressions) up to a specific token
            /// or a known <see cref="SqlExpr"/> if possible.
            /// </summary>
            /// <param name="e">Read expression.</param>
            /// <param name="closer">Predicate that detects the stopper (will NOT be added to the expression).</param>
            /// <param name="expectAtLeastOne">True to set an error if the expression is empty (no expressions in it).</param>
            /// <returns>True if an expression has sucessfully been found (it may be a <see cref="SqlExprRawItemList"/>).</returns>
            bool IsExpressionOrRawList( out SqlExpr e, Predicate<SqlToken> stopper, bool expectAtLeastOne )
            {
                if( stopper == null ) throw new ArgumentNullException( "stopper" );
                return IsExpressionOrRawListInternal( out e, null, stopper, expectAtLeastOne );
            }

            /// <summary>
            /// Reads a potential <see cref="SqlExprRawItemList"/> (a list of expressions) up to a specific token
            /// or a known <see cref="SqlExpr"/> enclosed in parenthesis: the stopper is the closing parenthesis. 
            /// </summary>
            /// <param name="e">Read expression.</param>
            /// <param name="openPar">Opening parenthesis (will be the very first token).</param>
            /// <param name="expectAtLeastOne">True to set an error if the block is empty (no expressions in it).</param>
            /// <returns>True if an expression has sucessfully been found.</returns>
            bool IsExpressionOrRawList( out SqlExpr e, SqlTokenOpenPar openPar, bool expectAtLeastOne )
            {
                if( openPar == null ) throw new ArgumentNullException( "opener" );
                return IsExpressionOrRawListInternal( out e, openPar,  t => t is SqlTokenClosePar, expectAtLeastOne );
            }

            bool IsExpressionOrRawListInternal( out SqlExpr e, SqlTokenOpenPar openPar, Predicate<SqlToken> closer, bool setErrorIfEmpty )
            {
                e = null;
                List<ISqlItem> exprs = new List<ISqlItem>();
                SqlExpr lastExpr = null;
                while( !(R.IsErrorOrEndOfInput || closer( R.Current )) )
                {
                    // If it is not the closer nor the end, it may be a valid expression.
                    if( IsExpression( out lastExpr, SqlTokenizer.PrecedenceLevel( SqlTokenType.Comma ), expected: false ) ) exprs.Add( lastExpr );
                    else
                    {
                        if( R.IsErrorOrEndOfInput ) break;
                        exprs.Add( R.Read<SqlToken>() );
                    }
                }
                // If we expect something and nothing was found and no error was previously set, we set an error.
                if( setErrorIfEmpty && exprs.Count == 0 && !R.IsError ) return R.SetCurrentError( "Expected expression." );
                // If no error occured, the block is built:
                // - if the opener is not null, with the the given opener and the found closer.
                // - if the opener is null, without any opener/closer and the closer is not consumed.
                if( !R.IsError )
                {
                    Debug.Assert( closer( R.Current ), "We are on the Closer token." );
                    if( openPar != null )
                    {
                        // If an opener exists, we always create the block.
                        Debug.Assert( R.Current is SqlTokenClosePar );
                        SqlTokenClosePar closePar = R.Read<SqlTokenClosePar>();
                        if( exprs.Count == 1 )
                        {
                            lastExpr.MutableEnclose( openPar, closePar );
                            e = lastExpr;
                            return true;
                        }
                        e = new SqlExprRawItemList( openPar, exprs, closePar );
                        return true;
                    }
                    // When no opener/closer exist and the block is empty, we do not instanciate it.
                    if( exprs.Count > 0 )
                    {
                        if( exprs.Count == 1 ) e = lastExpr;
                        else e = new SqlExprRawItemList( exprs );
                    }
                    return true;
                }
                // An error occured: closer was not found.
                // We let the block null... (we may here build a block with exprs and a kind of SqlExprSyntaxError at the end).
                return false;
            }

        }

    
}

