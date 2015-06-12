#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\SqlAnalyser.Expression.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.SqlServer.Parser
{
        public partial class SqlAnalyser
        {

            bool IsExpression( out SqlExpr e, int rightBindingPower, bool expected )
            {
                e = null;
                if( R.IsErrorOrEndOfInput || !IsExpressionNud( ref e ) )
                {
                    if( expected ) R.SetCurrentError( "Expected expression." );
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
                if( (R.Current.TokenType & SqlTokenType.IsIdentifier) != 0 )
                {
                    SqlTokenIdentifier id = R.Read<SqlTokenIdentifier>();
                    if( id.TokenType == SqlTokenType.Null )
                    {
                        e = new SqlExprNull( id );
                        return true;
                    }
                    if( id.TokenType == SqlTokenType.Select )
                    {
                        SelectSpecification select;
                        if( !MatchSelectSpecification( out select, id, true ) )
                        {
                            Debug.Assert( R.IsError );
                            return false;
                        }
                        e = select;
                        return true;
                    }
                    if( id.TokenType == SqlTokenType.Case )
                    {
                        SqlExprCase caseExpr;
                        if( !MatchCaseExpression( out caseExpr, id ) )
                        {
                            Debug.Assert( R.IsError );
                            return false;
                        }
                        e = caseExpr;
                        return true;
                    }
                    // This shortcuts the nud/led mechanism by directly handling 
                    // the . or the :: as a top precedence level operator.
                    return IsMonoOrMultiIdentifier( out e, true, id );
                }
                return false;
            }

            /// <summary>
            /// Combines the LED (LEft Denotation): The token has something at its left (postfix or infix).
            /// </summary>
            bool ExpressionCombineLed( ref SqlExpr left )
            {
                int precedenceLevel = R.CurrentPrecedenceLevel;
                if( R.Current.TokenType == SqlTokenType.OpenPar )
                {
                    // This prevents (select a)(select b) multiple statements
                    // to be considered as a call.
                    if( left is ISelectSpecification ) return false;
                    if( left.IsToken( SqlTokenType.Cast ) )
                    {
                        SqlTokenOpenPar openPar;
                        SqlExpr e;
                        SqlTokenIdentifier asToken;
                        SqlExprTypeDecl type;
                        SqlTokenClosePar closePar;
                        if( !R.IsToken( out openPar, true ) 
                            || !IsOneExpression( out e, false ) 
                            || !R.IsToken( out asToken, SqlTokenType.As, true ) 
                            || !IsTypeDecl( out type, true )
                            || !R.IsToken( out closePar, true ) ) return false;
                        left = new SqlExprCast( (SqlTokenIdentifier)left.FirstOrEmptyT, openPar, e, asToken, type, closePar );
                        return true;
                    }
                    SqlExprCommaList parenthesis;
                    if( !IsCommaList( out parenthesis, true ) ) return false;
                    SqlNoExprOverClause over;
                    if( !IsOverClause( out over ) && R.IsError ) return false;
                    left = new SqlExprKoCall( left, parenthesis, over );
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.Collate )
                {
                    SqlTokenIdentifier collate = R.Read<SqlTokenIdentifier>();
                    SqlTokenIdentifier name;
                    if( !R.IsToken( out name, true ) ) return false;
                    left = new SqlExprCollate( left, collate, name );
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
                    if( !(left is ISqlIdentifier) ) return R.SetCurrentError( "Unexpected '='. Assignment must follow an identifier." );
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
                    SqlTokenIdentifier isToken = R.Read<SqlTokenIdentifier>();
                    SqlTokenIdentifier notToken;
                    R.IsToken( out notToken, SqlTokenType.Not, false );
                    SqlTokenIdentifier nullToken;
                    if( !R.IsToken( out nullToken, SqlTokenType.Null, true ) ) return false;
                    left = new SqlExprIsNull( left, isToken, notToken, nullToken );
                    return true;
                }
                if( R.Current.TokenType == SqlTokenType.Not )
                {
                    SqlTokenIdentifier notToken = R.Read<SqlTokenIdentifier>();
                    if( R.Current.TokenType == SqlTokenType.Like ) return IsExprLike( ref left, notToken );
                    if( R.Current.TokenType == SqlTokenType.Between ) return IsExprBetween( ref left, notToken );
                    if( R.Current.TokenType == SqlTokenType.In ) return IsExprIn( ref left, notToken );
                    return R.SetCurrentError( "Expected 'like', 'between' or 'in'." );
                }
                if( R.Current.TokenType == SqlTokenType.Like ) return IsExprLike( ref left, null );
                if( R.Current.TokenType == SqlTokenType.Between ) return IsExprBetween( ref left, null );
                if( R.Current.TokenType == SqlTokenType.In ) return IsExprIn( ref left, null );
                if( SqlExprBinaryOperator.IsValidOperator( R.Current.TokenType ) )
                {
                    SqlToken cmp = R.Read<SqlToken>();
                    SqlExpr right;
                    if( !IsExpression( out right, precedenceLevel, true ) ) return false;
                    left = new SqlExprBinaryOperator( left, cmp, right );
                    return true;
                }
                if( SqlToken.IsSelectOperator( R.Current.TokenType ) )
                {
                    ISelectSpecification lSelect = left as ISelectSpecification;
                    if( lSelect == null ) return false;
                    SqlTokenIdentifier op = R.Read<SqlTokenIdentifier>();
                    if( op.TokenType == SqlTokenType.Order )
                    {
                        SqlTokenIdentifier by;
                        SelectOrderByColumnList columns;
                        if( !R.IsToken( out by, SqlTokenType.By, true ) ) return false;
                        if( !IsSelectOrderByColumnList( out columns ) ) return false;
                        
                        SelectOrderByOffset offsetClause;
                        if( IsSelectOrderByOffset( out offsetClause ) )
                        {
                            left = new SelectOrderBy( lSelect, op, by, columns, offsetClause );
                        }
                        else
                        {
                            if( R.IsError ) return false;
                            left = new SelectOrderBy( lSelect, op, by, columns );
                        }
                        return true;
                    }
                    if( op.TokenType == SqlTokenType.For )
                    {
                        SqlExpr content;
                        if( !IsExpressionOrRawList( out content, SelectPartStopper, false, true ) ) return false;
                        left = new SelectFor( lSelect, op, content );
                        return true;
                    }
                    Debug.Assert( SelectCombineOperator.IsValidOperator( op.TokenType ) );
                    SqlTokenIdentifier all = null;
                    if( op.TokenType == SqlTokenType.Union ) R.IsToken( out all, SqlTokenType.All, false );
                    SqlExpr right;
                    if( !IsExpression( out right, precedenceLevel, true ) ) return false;
                    ISelectSpecification rSelect = right as ISelectSpecification;
                    if( rSelect == null ) return R.SetCurrentError( "Expected select expression." );
                    left = new SelectCombineOperator( lSelect, op, all, rSelect );
                    return true;
                }
                return false;
            }

            bool IsExprBetween( ref SqlExpr left, SqlTokenIdentifier notToken )
            {
                Debug.Assert( R.Current.TokenType == SqlTokenType.Between );
                SqlTokenIdentifier betweenToken = R.Read<SqlTokenIdentifier>();
                SqlExpr start;
                if( !IsExpression( out start, SqlTokenizer.PrecedenceLevel( SqlTokenType.OpComparisonLevel ), true ) ) return false;
                SqlTokenIdentifier andToken;
                if( !R.IsToken( out andToken, SqlTokenType.And, true ) ) return false;
                SqlExpr stop;
                if( !IsExpression( out stop, SqlTokenizer.PrecedenceLevel( SqlTokenType.OpComparisonLevel ), true ) ) return false;

                left = new SqlExprBetween( left, notToken, betweenToken, start, andToken, stop );
                return true;
            }

            bool IsExprLike( ref SqlExpr left, SqlTokenIdentifier notToken )
            {
                Debug.Assert( R.Current.TokenType == SqlTokenType.Like );
                SqlTokenIdentifier likeToken = R.Read<SqlTokenIdentifier>();
                SqlExpr pattern;
                if( !IsExpression( out pattern, SqlTokenizer.PrecedenceLevel( SqlTokenType.OpComparisonLevel ), true ) ) return false;
                SqlTokenIdentifier escapeToken;
                SqlTokenLiteralString escapeChar = null;
                if( R.IsToken( out escapeToken, SqlTokenType.Escape, false ) )
                {
                    if( !R.IsToken( out escapeChar, true ) ) return false;
                }
                left = new SqlExprLike( left, notToken, likeToken, pattern, escapeToken, escapeChar );
                return true;
            }

            bool IsExprIn( ref SqlExpr left, SqlTokenIdentifier notToken )
            {
                Debug.Assert( R.Current.TokenType == SqlTokenType.In );
                SqlTokenIdentifier inToken = R.Read<SqlTokenIdentifier>();
                SqlExprCommaList values;
                if( !IsCommaList( out values, true ) ) return false;
                left = new SqlExprIn( left, notToken, inToken, values );
                return true;
            }

            bool MatchCaseExpression( out SqlExprCase e, SqlTokenIdentifier caseToken )
            {
                e = null;
                SqlExpr exprSimple = null;
                SqlTokenIdentifier whenToken;
                if( !R.IsToken( out whenToken, SqlTokenType.When, false ) )
                {
                    // Simple case.
                    if( !IsExpression( out exprSimple, 0, true ) ) return false;
                    if( !R.IsToken( out whenToken, SqlTokenType.When, true ) ) return false;
                }
                Debug.Assert( whenToken != null );
                List<ISqlItem> whenItems = new List<ISqlItem>();
                do
                {
                    SqlExpr expr;
                    if( !IsExpression( out expr, 0, true ) ) return false;
                    SqlTokenIdentifier thenToken;
                    if( !R.IsToken( out thenToken, SqlTokenType.Then, true ) ) return false;
                    SqlExpr exprValue;
                    if( !IsExpression( out exprValue, 0, true ) ) return false;
                    whenItems.Add( whenToken );
                    whenItems.Add( expr );
                    whenItems.Add( thenToken );
                    whenItems.Add( exprValue );
                }
                while( R.IsToken( out whenToken, SqlTokenType.When, false ) );
                SqlExprCaseWhenSelector whenSelector = new SqlExprCaseWhenSelector( whenItems );
                
                SqlExpr exprElse = null;
                SqlTokenIdentifier elseToken;
                if( R.IsToken( out elseToken, SqlTokenType.Else, false ) )
                {
                    if( !IsExpression( out exprElse, 0, true ) ) return false;
                }
                SqlTokenIdentifier endToken;
                if( !R.IsToken( out endToken, SqlTokenType.End, true ) ) return false;

                e = new SqlExprCase( caseToken, exprSimple, whenSelector, elseToken, exprElse, endToken );
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
            /// Reads a comma separated list of expressions (that can be <see cref="SqlExprRawItemList"/>).
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
                return IsExpressionOrRawList( out e, SqlToken.IsCommaOrCloseParenthesisOrTerminator, false, expected );
            }

            /// <summary>
            /// Reads a potential <see cref="SqlExprRawItemList"/> (a list of expressions) up to a specific token
            /// or a known <see cref="SqlToken"/> if possible.
            /// </summary>
            /// <param name="e">Read expression.</param>
            /// <param name="closer">Predicate that detects the stopper (will NOT be added to the expression).</param>
            /// <param name="blindlyAcceptCurrentToken">True to accept the current token whatever it is.</param>
            /// <param name="expectAtLeastOne">True to set an error if the expression is empty (no expressions in it).</param>
            /// <returns>True if an expression has successfully been found (it may be a <see cref="SqlExprRawItemList"/>).</returns>
            bool IsExpressionOrRawList( out SqlExpr e, Predicate<SqlToken> stopper, bool blindlyAcceptCurrentToken, bool expectAtLeastOne )
            {
                if( stopper == null ) throw new ArgumentNullException( "stopper" );
                return IsExpressionOrRawListInternal( out e, null, stopper, blindlyAcceptCurrentToken, expectAtLeastOne );
            }

            /// <summary>
            /// Reads a single <see cref="SqlExpr"/> or <see cref="SqlExprRawItemList"/> (a list of expressions) up to the closing parenthesis: 
            /// the stopper is the closing parenthesis. 
            /// </summary>
            /// <param name="e">Read expression.</param>
            /// <param name="openPar">Opening parenthesis (will be the very first token).</param>
            /// <param name="expectAtLeastOne">True to set an error if the block is empty (no expressions in it).</param>
            /// <returns>True if an expression has successfully been found.</returns>
            bool IsExpressionOrRawList( out SqlExpr e, SqlTokenOpenPar openPar, bool expectAtLeastOne )
            {
                if( openPar == null ) throw new ArgumentNullException( "opener" );
                return IsExpressionOrRawListInternal( out e, openPar,  t => t is SqlTokenClosePar, false, expectAtLeastOne );
            }

            bool IsExpressionOrRawListInternal( out SqlExpr e, SqlTokenOpenPar openPar, Predicate<SqlToken> closer, bool blindlyAcceptCurrentToken, bool setErrorIfEmpty )
            {
                Debug.Assert( openPar == null || closer( SqlTokenClosePar.ClosePar ), "If we have an open parenthesis, the closer function must detect a closing parenthesis." );
                e = null;
                List<ISqlItem> exprs = new List<ISqlItem>();
                SqlExpr lastExpr = null;
                while( blindlyAcceptCurrentToken || !(R.IsErrorOrEndOfInput || closer( R.Current )) )
                {
                    blindlyAcceptCurrentToken = false;
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
                // If no error occurred, the block is built:
                // - if the opener is not null, with the the given opener and the found closer.
                // - if the opener is null, without any opener/closer and the closer is not consumed.
                if( !R.IsError )
                {
                    Debug.Assert( closer( R.Current ) || R.IsEndOfInput, "We are on the Closer token or at the end." );
                    if( openPar != null )
                    {
                        // If an opener exists, we always create the block.
                        if( R.Current.TokenType == SqlTokenType.ClosePar )
                        {
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
                        else return R.SetCurrentError( "Expected ')'." );
                    }
                    // When no opener/closer exist and the block is empty, we do not instantiate it.
                    if( exprs.Count > 0 )
                    {
                        if( exprs.Count == 1 ) e = lastExpr;
                        else e = new SqlExprRawItemList( exprs );
                    }
                    return true;
                }
                // An error occurred: closer was not found.
                // We let the block null... (we may here build a block with exprs and a kind of SqlExprSyntaxError at the end).
                return false;
            }

        }

    
}

