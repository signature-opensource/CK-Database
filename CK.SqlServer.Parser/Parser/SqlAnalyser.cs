#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\SqlAnalyser.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;
using CK.SqlServer;

namespace CK.SqlServer.Parser
{
        public partial class SqlAnalyser
        {
            readonly SqlTokenReader R;

            public class ErrorResult
            {
                public readonly string ErrorMessage;
                public readonly string HeadSource;
                public bool IsError { get { return this != NoError; } }
                public static implicit operator bool( ErrorResult r ) { return r == NoError; }
                
                internal ErrorResult( string errorMessage, string headSource )
                {
                    Debug.Assert( NoError == null || (errorMessage != null && headSource != null) );
                    ErrorMessage = errorMessage;
                    HeadSource = headSource;
                }

                public override string ToString()
                {
                    return IsError ? String.Format( "Error: {0}\r\nText: {1}", ErrorMessage, HeadSource ) : "<success>";
                }

                static internal readonly ErrorResult NoError = new ErrorResult( null, null );

                /// <summary>
                /// Logs the error message if <see cref="IsError"/> is true, otherwise does nothing.
                /// </summary>
                /// <param name="monitor">Monitor to log into.</param>
                /// <param name="asWarning">True to log a warning instead of an error.</param>
                public void LogOnError( IActivityMonitor monitor, bool asWarning = false )
                {
                    if( monitor == null ) throw new ArgumentNullException( "monitor" );
                    if( IsError )
                    {
                        using( asWarning ? monitor.OpenWarn().Send( ErrorMessage ) : monitor.OpenError().Send( ErrorMessage ) )
                        {
                            monitor.Info().Send( HeadSource );
                        }
                    }
                }
            }

            [DebuggerStepThrough]
            public static ErrorResult ParseStatement( out SqlExprBaseSt statement, string text )
            {
                SqlAnalyser a = new SqlAnalyser( new SqlTokenizer(), text );
                if( a.IsStatement( out statement, true ) ) return ErrorResult.NoError;
                return a.CreateErrorResult();
            }

            [DebuggerStepThrough]
            public static ErrorResult ParseStatement<T>( out T statement, string text ) where T : SqlExprBaseSt
            {
                statement = null;

                SqlExprBaseSt st;
                SqlAnalyser a = new SqlAnalyser( new SqlTokenizer(), text );
                if( !a.IsStatement( out st, true ) ) return a.CreateErrorResult();

                statement = st as T;
                if( statement == null )
                {
                    a.R.SetCurrentError( "Expected '{0}' statement but found a '{1}'.", statement.GetType().Name, st.GetType().Name );
                    return a.CreateErrorResult();
                }
                return ErrorResult.NoError;
            }

            [DebuggerStepThrough]
            public static ErrorResult ParseExpression( out SqlExpr expression, string text )
            {
                SqlAnalyser a = new SqlAnalyser( new SqlTokenizer(), text );
                if( a.IsExpression( out expression, 0, true ) ) return ErrorResult.NoError;
                return a.CreateErrorResult();
            }

            SqlAnalyser( SqlTokenizer t, string text )
            {
                R = new SqlTokenReader( t.Parse( text ), t.ToString, t.GetTokenPosition );
                R.MoveNext();
            }

            public override string ToString()
            {
                return R.ToString();
            }

            ErrorResult CreateErrorResult()
            {
                return new ErrorResult( R.GetErrorMessage(), R.ToString() );
            }

            bool IsStatement( out SqlExprBaseSt statement, bool expected = true )
            {
                statement = null;
                SqlTokenIdentifier id = R.Current as SqlTokenIdentifier;
                // A statement starts with an identifier that must be non quoted and not a variable.
                if( id == null || id.IsQuoted || id.IsVariable )
                {
                    if( R.Current.TokenType == SqlTokenType.SemiColon )
                    {
                        statement = new SqlExprStEmpty( R.Read<SqlTokenTerminal>() );
                        return true;
                    }
                    if( R.Current.TokenType == SqlTokenType.OpenPar )
                    {
                        SqlExpr e;
                        if( !IsExpression( out e, 0, true ) ) return false;
                        statement = new SqlExprStUnmodeled( e, GetOptionalTerminator() );
                        return true;
                    }
                    if( expected ) R.SetCurrentError( "Statement expected." );
                    return false;
                }
                // End Conversation ... is a statement.
                // Otherwise, we handle it: this is the end of a block above.
                if( id.TokenType == SqlTokenType.End && R.RawLookup.TokenType != SqlTokenType.Conversation )
                {
                    if( expected ) R.SetCurrentError( "Statement expected." );
                    return false;
                }
                // Begin Dialog ... or Begin Conversation are statement.
                // Otherwise we handle it as a:
                // - Begin transaction
                // - Begin Try ... End Catch block
                // - or a Begin ... End block.
                if( id.TokenType == SqlTokenType.Begin && R.RawLookup.TokenType != SqlTokenType.Conversation && R.RawLookup.TokenType != SqlTokenType.Dialog )
                {
                    R.MoveNext();
                    SqlTokenIdentifier tranOrTry;
                    // "tran" and "transaction" both map to SqlTokenType.Transaction.
                    if( R.IsToken( out tranOrTry, SqlTokenType.Transaction, false ) )
                    {
                        SqlTokenIdentifier tranNameOrVariable;
                        SqlTokenIdentifier withToken = null;
                        SqlTokenIdentifier markToken = null;
                        SqlTokenLiteralString description = null;
                        if( R.IsToken( out tranNameOrVariable, false ) )
                        {
                            if( R.IsToken( out withToken, SqlTokenType.With, false ) )
                            {
                                if( !R.IsUnquotedIdentifier( out markToken, "mark", true ) ) return false;
                                R.IsToken( out description, false );
                            }
                        }
                        statement = new SqlExprStBeginTran( id, tranOrTry, tranNameOrVariable, withToken, markToken, description, GetOptionalTerminator() );
                        return true;
                    }
                    R.IsToken( out tranOrTry, SqlTokenType.Try, false );
                    SqlExprStatementList body;
                    if( !IsStatementList( out body, true ) ) return false;
                    SqlTokenIdentifier end;
                    if( !R.IsToken( out end, SqlTokenType.End, true ) ) return false;
                    if( tranOrTry == null )
                    {
                        statement = new SqlExprStBlock( id, body, end );
                        return true;
                    }
                    // Begin Try ... End Try Begin Catch ... End Catch.
                    SqlTokenIdentifier endTry;
                    if( !R.IsToken( out endTry, SqlTokenType.Try, true ) ) return false;
                    SqlTokenIdentifier begCatch, begCatchToken;
                    if( !R.IsToken( out begCatch, SqlTokenType.Begin, true ) || !R.IsToken( out begCatchToken, SqlTokenType.Catch, true ) ) return false;
                    SqlExprStatementList bodyCatch;
                    if( !IsStatementList( out bodyCatch, true ) ) return false;
                    SqlTokenIdentifier endCatch, endCatchToken;
                    if( !R.IsToken( out endCatch, SqlTokenType.End, true ) || !R.IsToken( out endCatchToken, SqlTokenType.Catch, true ) ) return false;
                    statement = new SqlExprStTryCatch( new SqlExprMultiToken<SqlTokenIdentifier>( id, tranOrTry ), 
                                                       body, 
                                                       new SqlExprMultiToken<SqlTokenIdentifier>( end, endTry, begCatch, begCatchToken), 
                                                       bodyCatch, 
                                                       new SqlExprMultiToken<SqlTokenIdentifier>( endCatch, endCatchToken ), 
                                                       GetOptionalTerminator() );
                    return true;
                }
                if( id.TokenType == SqlTokenType.Create || id.TokenType == SqlTokenType.Alter )
                {
                    R.MoveNext();
                    SqlTokenIdentifier type;
                    if( !R.IsToken( out type, true ) ) return false;
                    if( type.TokenType == SqlTokenType.Procedure )
                    {
                        SqlExprStStoredProc sp;
                        if( !IsStoredProcedure( out sp, id, type ) ) return false;
                        statement = sp;
                        return true;
                    }
                    if( type.TokenType == SqlTokenType.View )
                    {
                        SqlExprStView view;
                        if( !IsView( out view, id, type ) ) return false;
                        statement = view;
                        return true;
                    }
                    if( type.TokenType == SqlTokenType.Function )
                    {
                        SqlExprStFunction func;
                        if( !IsFunction( out func, id, type ) ) return false;
                        statement = func;
                        return true;
                    }
                }
                if( id.TokenType == SqlTokenType.Break || id.TokenType == SqlTokenType.Continue )
                {
                    R.MoveNext();
                    statement = new SqlExprStMonoStatement( id, GetOptionalTerminator() );
                    return true;
                }
                if( id.TokenType == SqlTokenType.If )
                {
                    R.MoveNext();
                    SqlExpr expr;
                    if( !IsExpression( out expr, 0, true ) ) return false;
                    SqlExprBaseSt thenSt;
                    if( !IsStatement( out thenSt, true ) ) return false;
                    SqlTokenIdentifier elseToken;
                    SqlExprBaseSt elseSt = null;
                    if( R.IsToken( out elseToken, SqlTokenType.Else, false ) )
                    {
                        if( !IsStatement( out elseSt, true ) ) return false;
                    }
                    statement = new SqlExprStIf( id, expr, thenSt, elseToken, elseSt, GetOptionalTerminator() );
                    return true;
                }
                if( id.TokenType == SqlTokenType.Return )
                {
                    R.MoveNext();
                    SqlExpr expr;
                    IsExpression( out expr, 0, false );
                    statement = new SqlExprStReturn( id, expr, GetOptionalTerminator() );
                    return true;
                }
                if( id.TokenType == SqlTokenType.Goto )
                {
                    R.MoveNext();
                    SqlTokenIdentifier target;
                    if( !R.IsToken( out target, true ) ) return false;
                    statement = new SqlExprStGoto( id, target, GetOptionalTerminator() );
                    return true;
                }
                if( id.TokenType == SqlTokenType.Set )
                {
                    R.MoveNext();
                    using( R.SetAssignmentContext( true ) )
                    {
                        if( R.Current.IsToken( SqlTokenType.IdentifierVariable ) )
                        {
                            SqlTokenIdentifier left = R.Read<SqlTokenIdentifier>();
                            SqlTokenTerminal assignT;
                            SqlExpr right;
                            if( !R.IsToken( out assignT, t => (t.TokenType & SqlTokenType.IsAssignOperator) != 0, expected: true ) ) return false;
                            SqlTokenIdentifier cursorToken;
                            if( R.IsToken( out cursorToken, SqlTokenType.Cursor, false ) )
                            {
                                ISqlExprCursor c;
                                if( !MatchCursorDefinition( cursorToken, out c ) )
                                {
                                    Debug.Assert( R.IsError );
                                    return false;
                                }
                                right = (SqlExpr)c;
                            }
                            else if( !IsExpression( out right, 0, true ) ) return false;
                            statement = new SqlExprStSetVar( id, left, assignT, right, GetOptionalTerminator() );
                            return true;
                        }
                        SqlExpr list;
                        if( !IsExpressionOrRawList( out list, SqlToken.IsTerminatorOrPossibleStartStatement, blindlyAcceptCurrentToken: true, expectAtLeastOne: true ) ) return false;
                        statement = new SqlExprStSetOpt( id, list, GetOptionalTerminator() );
                    }
                    return true;
                }
                if( id.TokenType == SqlTokenType.Declare )
                {
                    R.MoveNext();
                    // Syntax: declare cursorName cursor ...
                    //   - cursorName can not be a @Variable.
                    //   - cursorName can be a quoted identifier.
                    //   - no 'as' between cursorName and 'cursor'.
                    if( R.Current.TokenType != SqlTokenType.IdentifierVariable )
                    {
                        SqlTokenIdentifier name;
                        SqlTokenIdentifier cursorToken;
                        if( !R.IsToken( out name, true ) || !R.IsToken( out cursorToken, SqlTokenType.Cursor, true ) ) return false;
                        ISqlExprCursor cursorExpr;
                        if( !MatchCursorDefinition( cursorToken, out cursorExpr ) )
                        {
                            Debug.Assert( R.IsError );
                            return false;
                        }
                        statement = new SqlExprStDeclareCursor( id, name, cursorExpr, GetOptionalTerminator() );
                        return true;
                    }
                    SqlTokenOpenPar openPar;
                    SqlTokenClosePar closePar;
                    List<ISqlItem> items;
                    if( !IsCommaList<SqlExprDeclare>( out openPar, out items, out closePar, false, IsVariableDeclare ) ) return false;
                    if( openPar != null || closePar != null ) return R.SetCurrentError( "Unexpected parenthesis in Declare statement." );
                    if( items.Count == 0 ) return R.SetCurrentError( "Declare expect at least on variable." );
                    SqlExprDeclareList declarations = new SqlExprDeclareList( items );
                    statement = new SqlExprStDeclare( id, declarations, GetOptionalTerminator() );
                    return true;
                }
                bool canBeAStatement = id.IsStartStatement || id.TokenType == SqlTokenType.With;
                if( !canBeAStatement )
                {
                    // If it is not a reserved keyword, it can only be 
                    // a label definition.
                    SqlTokenTerminal colon;
                    if( id.TrailingTrivia.Count > 0
                        || (colon = R.RawLookup as SqlTokenTerminal) == null
                        || colon.TokenType != SqlTokenType.Colon
                        || colon.LeadingTrivia.Count > 0 )
                    {
                        if( expected ) R.SetCurrentError( "Statement expected." );
                        return false;
                    }
                    R.MoveNext();
                    R.MoveNext();
                    statement = new SqlExprStLabelDef( id, colon, GetOptionalTerminator() );
                    return true;
                }
                SqlExpr unmodeled;
                if( !IsExpressionOrRawList( out unmodeled, SqlToken.IsTerminatorOrPossibleStartStatement, true, true ) ) return false;
                statement = new SqlExprStUnmodeled( unmodeled, GetOptionalTerminator() );
                return true;
            }

            bool IsStatementList( out SqlExprStatementList l, bool atLeastOneStatement )
            {
                l = null;
                List<SqlExprBaseSt> statements = new List<SqlExprBaseSt>();
                SqlExprBaseSt st;
                while( IsStatement( out st, false ) )
                {
                    statements.Add( st );
                }
                if( statements.Count == 0 )
                {
                    if( atLeastOneStatement && !R.IsError ) R.SetCurrentError( "At least one statement expected." );
                    return false;
                }
                l = new SqlExprStatementList( statements );
                return !R.IsError;
            }

            bool IsView( out SqlExprStView view, SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type )
            {
                view = null;

                SqlExprMultiIdentifier name;
                if( !IsMultiIdentifier( out name, true ) ) return false;

                SqlExprColumnList columns;
                IsColumnList( out columns );

                SqlExprUnmodeledItems options;
                SqlTokenIdentifier asToken;
                if( !IsUnmodeledUntil( out options, out asToken, t => t.TokenType == SqlTokenType.As ) ) return false;

                SqlExprUnmodeledItems body;
                SqlTokenTerminal term;
                if( !IsUnmodeledUntil( out body, out term, t => t.TokenType == SqlTokenType.SemiColon ) ) return false;
                term = GetOptionalTerminator();
                view = new SqlExprStView( alterOrCreate, type, name, columns, options, asToken, body, term );
                return true;
            }

            bool IsColumnList( out SqlExprColumnList columns )
            {
                columns = null;
                SqlTokenOpenPar openPar;
                SqlTokenClosePar closePar;
                List<ISqlItem> items;
                if( R.Current.TokenType != SqlTokenType.OpenPar ) return false;
                if( !IsCommaList<SqlExprIdentifier>( out openPar, out items, out closePar, true, IsMonoIdentifier ) ) return false;
                columns = new SqlExprColumnList( openPar, items, closePar );
                return true;
            }

            bool IsFunction( out SqlExprStFunction func, SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type )
            {
                func = null;

                /*
                CREATE FUNCTION [ schema_name. ] function_name 
                    ( [ { @parameter_name [ AS ][ type_schema_name. ] parameter_data_type 
                        [ = default ] [ READONLY ] } 
                        [ ,...n ]
                        ]
                    )
                RETURNS 
                 */
                SqlExprMultiIdentifier name;
                if( !IsMultiIdentifier( out name, true ) ) return false;

                SqlExprParameterList parameters;
                if( !IsParameterList( out parameters, requiresParenthesis: true ) ) return false;
                
                SqlTokenIdentifier returns;
                if( !R.IsToken( out returns, SqlTokenType.Returns, true ) ) return false;

                SqlTokenIdentifier table;
                SqlTokenIdentifier tableVariableNameToken;
                if( R.IsToken( out table, SqlTokenType.Table, false ) )
                {
                    // Inline Table-Valued Function Syntax
                    // CREATE FUNCTION [ schema_name. ] function_name 
                    //    ( [ { @parameter_name [ AS ] [ type_schema_name. ] parameter_data_type 
                    //        [ = default ] [ READONLY ] } 
                    //        [ ,...n ]
                    //      ]
                    //    )
                    // RETURNS TABLE
                    //    [ WITH <function_option> [ ,...n ] ]
                    //    [ AS ]
                    //    RETURN [ ( ] select_stmt [ ) ]
                    // [ ; ]
                    SqlExprUnmodeledItems options;
                    SqlTokenIdentifier endOptionToken;
                    SqlTokenIdentifier asToken;
                    SqlTokenIdentifier returnToken;
                    if( !IsFunctionOptionsAsAndBeginOrReturn( out options, out endOptionToken, out asToken, out returnToken, isBegin: false ) ) return false;
                    SqlExpr e;
                    if( !IsExpression( out e, 0, true ) ) return false;
                    SqlTokenTerminal term = GetOptionalTerminator();
                    SelectSpecification q = e as SelectSpecification;
                    if( q == null ) return R.SetCurrentError( "Expected select statement." );

                    func = new SqlExprStFunctionInlineTable(
                                    alterOrCreate,
                                    type,
                                    name,
                                    parameters,
                                    returns,
                                    table,
                                    options,
                                    asToken,
                                    returnToken,
                                    q,
                                    term );
                }
                else if( R.IsToken( out tableVariableNameToken, t => t.IsVariable, false ) )
                {
                    // Multistatement Table-valued Function Syntax
                    // CREATE FUNCTION [ schema_name. ] function_name 
                    //   ( [ { @parameter_name [ AS ] [ type_schema_name. ] parameter_data_type 
                    //         [ = default ] [READONLY] } 
                    //       [ ,...n ]
                    //     ]
                    //   )
                    // RETURNS @return_variable TABLE <table_type_definition>
                    //    [ WITH <function_option> [ ,...n ] ]
                    //    [ AS ]
                    //    BEGIN 
                    //        function_body 
                    //        RETURN
                    //    END
                    // [ ; ]
                    throw new NotSupportedException( "Multistatement Table-valued Function Syntax" );
                }
                else 
                {
                     // Scalar Function Syntax
                     // CREATE FUNCTION [ schema_name. ] function_name 
                     //   ( [ { @parameter_name [ AS ][ type_schema_name. ] parameter_data_type 
                     //       [ = default ] [ READONLY ] } 
                     //       [ ,...n ]
                     //     ]
                     //   )
                     //   RETURNS return_data_type
                     //       [ WITH <function_option> [ ,...n ] ]
                     //       [ AS ]
                     //       BEGIN 
                     //           function_body 
                     //           RETURN scalar_expression
                     //       END
                     //  [ ; ]
                    SqlExprTypeDecl returrnScalarType;
                    if( !IsTypeDecl( out returrnScalarType ) ) return false;
                    // Scalar Function Syntax
                    SqlExprUnmodeledItems options;
                    SqlTokenIdentifier endOptionToken;
                    SqlTokenIdentifier asToken;
                    SqlTokenIdentifier begin;
                    if( !IsFunctionOptionsAsAndBeginOrReturn( out options, out endOptionToken, out asToken, out begin ) ) return false;
                    SqlExprStatementList bodyStatements;
                    SqlTokenIdentifier end;
                    SqlTokenTerminal term;
                    if( !IsBodyStatementListSafe( out bodyStatements, ref begin, out end, out term ) ) return false;
                    func = new SqlExprStFunctionScalar(
                                    alterOrCreate, 
                                    type, 
                                    name, 
                                    parameters,
                                    returns,
                                    returrnScalarType,
                                    options,
                                    asToken,
                                    begin,
                                    bodyStatements, 
                                    end, 
                                    term );
                }
                return true;
            }

            private bool IsFunctionOptionsAsAndBeginOrReturn( out SqlExprUnmodeledItems options, out SqlTokenIdentifier endOptionToken, out SqlTokenIdentifier asToken, out SqlTokenIdentifier beginOrReturn, bool isBegin = true )
            {
                asToken = beginOrReturn = null;
                if( !IsUnmodeledUntil( out options, out endOptionToken, t => t.TokenType == SqlTokenType.As || t.TokenType == SqlTokenType.Begin ) ) return false;
                asToken = null;
                beginOrReturn = null;
                if( endOptionToken.TokenType == SqlTokenType.As )
                {
                    asToken = endOptionToken;
                    if( !R.IsToken( out beginOrReturn, isBegin ? SqlTokenType.Begin : SqlTokenType.Return, true ) ) return false;
                }
                else
                {
                    beginOrReturn = endOptionToken;
                }
                return true;
            }

            bool IsStoredProcedure( out SqlExprStStoredProc sp, SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type )
            {
                sp = null;

                SqlExprMultiIdentifier name;
                if( !IsMultiIdentifier( out name, true ) ) return false;

                SqlExprParameterList parameters;
                if( !IsParameterList( out parameters, requiresParenthesis: false ) ) return false;

                SqlExprUnmodeledItems options;
                SqlTokenIdentifier asToken;
                if( !IsUnmodeledUntil( out options, out asToken, t => t.TokenType == SqlTokenType.As, EatExecuteAs ) ) return false;

                SqlTokenIdentifier begin = null;
                SqlExprStatementList bodyStatements;
                SqlTokenIdentifier end;
                SqlTokenTerminal term;
                if( !IsBodyStatementListSafe( out bodyStatements, ref begin, out end, out term ) ) return false;
                if( begin == null )
                {
                    sp = new SqlExprStStoredProc( alterOrCreate, type, name, parameters, options, asToken, bodyStatements, term );
                }
                else
                {
                    sp = new SqlExprStStoredProc( alterOrCreate, type, name, parameters, options, asToken, begin, bodyStatements, end, term );
                }
                return true;
            }

            bool IsBodyStatementListSafe( out SqlExprStatementList bodyStatements, ref SqlTokenIdentifier begin, out SqlTokenIdentifier end, out SqlTokenTerminal term )
            {
                end = null;
                term = null;
                using( var collector = R.OpenCollector() )
                {
                    if( begin == null ) R.IsToken( out begin, SqlTokenType.Begin, false );

                    // Attempts to read a statement list. If it fails, reads the whole stream as an unmodeled list of tokens.
                    if( IsStatementList( out bodyStatements, true ) )
                    {
                        if( begin != null && !R.IsToken( out end, SqlTokenType.End, true ) ) return false;
                        term = GetOptionalTerminator();
                    }
                    else
                    {
                        // Collects all tokens and generates a Statement list with one unmodeled list of tokens.
                        // Saves the begin/end and semi colon terminator if possible.
                        term = collector.ReadToEnd();
                        if( begin != null )
                        {
                            if( collector.Count > 0 && collector[collector.Count - 1].TokenType == SqlTokenType.End )
                            {
                                end = (SqlTokenIdentifier)collector[collector.Count - 1];
                            }
                            else
                            {
                                return R.SetCurrentError( "Missing END." );
                            }
                        }
                        var t = new SqlExprStUnmodeled( new SqlExprUnmodeledItems( begin != null ? collector.Skip( 1 ).Take( collector.Count - 2 ) : collector ) );
                        bodyStatements = new SqlExprStatementList( new[] { t } );
                    }
                }
                return true;
            }

            bool IsParameterList( out SqlExprParameterList parameters, bool requiresParenthesis )
            {
                parameters = null;
                SqlTokenOpenPar openPar;
                SqlTokenClosePar closePar;
                List<ISqlItem> items;
                if( !IsCommaList<SqlExprParameter>( out openPar, out items, out closePar, requiresParenthesis, IsParameter ) ) return false;
                parameters = openPar != null ? new SqlExprParameterList( openPar, items, closePar ) : new SqlExprParameterList( items );
                return true;
            }

            bool IsParameter( out SqlExprParameter parameter, bool expected = true )
            {
                parameter = null;
                SqlExprTypedIdentifier declVar;
                SqlExprParameterDefaultValue defValue = null;
                using( R.SetAssignmentContext( true ) )
                {
                    if( !IsTypedIdentifer( out declVar, t => t.IsVariable, expected ) ) return false;
                    SqlTokenTerminal assign;
                    if( R.IsToken( out assign, SqlTokenType.Assign, false ) )
                    {
                        SqlTokenIdentifier variable;
                        if( R.IsToken( out variable, SqlTokenType.Null, false ) 
                            || R.IsToken( out variable, SqlTokenType.IdentifierVariable, false ) )
                        {
                            defValue = new SqlExprParameterDefaultValue( assign, variable );
                        }
                        else
                        {
                            SqlTokenTerminal minusSign;
                            R.IsToken( out minusSign, false );
                            SqlTokenBaseLiteral value;
                            if( !R.IsToken( out value, true ) ) return false;
                            defValue = new SqlExprParameterDefaultValue( assign, minusSign, value );
                        }
                    }
                }
                SqlTokenIdentifier outputClause;
                R.IsToken( out outputClause, SqlTokenType.Output, false );

                SqlTokenIdentifier readonlyClause;
                R.IsToken( out readonlyClause, SqlTokenType.Readonly, false );

                parameter = new SqlExprParameter( declVar, defValue, outputClause, readonlyClause );
                return true;
            }

            bool IsVariableDeclare( out SqlExprDeclare declare, bool expected = true )
            {
                declare = null;
                SqlExprTypedIdentifier declVar;
                SqlTokenTerminal assignToken = null;
                SqlExpr initialValue = null;
                // Syntax: declare @name [as] type
                using( R.SetAssignmentContext( true ) )
                {
                    if( !IsTypedIdentifer( out declVar, t => t.IsVariable, expected ) ) return false;
                    if( R.IsToken( out assignToken, SqlTokenType.Assign, false ) )
                    {
                        if( !IsExpression( out initialValue, SqlTokenizer.PrecedenceLevel( SqlTokenType.Comma ), true ) ) return false;
                    }
                }
                declare = new SqlExprDeclare( declVar, assignToken, initialValue );
                return true;
            }

            bool MatchCursorDefinition( SqlTokenIdentifier cursorToken, out ISqlExprCursor cursor )
            {
                cursor = null;
                SqlExprUnmodeledItems options;
                SqlTokenIdentifier forToken;
                if( !IsUnmodeledUntil( out options, out forToken, t => t.TokenType == SqlTokenType.For ) ) return false;
                SqlExpr eSelect;
                if( !IsExpression( out eSelect, 0, true ) ) return false;
                ISelectSpecification select;
                if( (select = eSelect as ISelectSpecification) == null ) return R.SetCurrentError( "Select statement expected." );
                SqlTokenIdentifier forOptionsToken;
                SqlTokenIdentifier readTokenSql92 = null;
                SqlTokenIdentifier onlyTokenSql92 = null;
                SqlTokenIdentifier updateToken = null;
                SqlTokenIdentifier ofToken = null;
                SqlNoExprIdentifierList updateColumns = null;
                if( R.IsToken( out forOptionsToken, SqlTokenType.For, false ) )
                {
                    if( R.IsUnquotedIdentifier( out readTokenSql92, "read", false ) )
                    {
                        if( !R.IsToken( out onlyTokenSql92, SqlTokenType.Only, true ) ) return false;
                    }
                    else
                    {
                        if( !R.IsToken( out updateToken, SqlTokenType.Update, true ) ) return false;
                        if( R.IsToken( out ofToken, SqlTokenType.Of, false ) )
                        {
                            List<ISqlItem> columns = null;
                            if( !IsCommaListNonEnclosed<SqlExprIdentifier>( out columns, IsMonoIdentifier, true ) ) return false;
                            updateColumns = new SqlNoExprIdentifierList( columns );
                        }
                    }
                }
                if( readTokenSql92 != null )
                {
                    if( options != null ) return R.SetCurrentError( "Sql92: There can be no options in 'cursor [Options] for <select> for read only;'." );
                    cursor = new SqlExprCursorSql92( null, null, cursorToken, forToken, select, forOptionsToken, readTokenSql92, onlyTokenSql92, updateToken, ofToken, updateColumns );
                }
                else
                {
                    cursor = new SqlExprCursor( cursorToken, options, forToken, select, forOptionsToken, updateToken, ofToken, updateColumns );
                }
                return true;
            }

            bool IsTypedIdentifer( out SqlExprTypedIdentifier declVar, Predicate<SqlTokenIdentifier> idFilter, bool expected = true )
            {
                declVar = null;
                SqlTokenIdentifier identifier;
                if( !R.IsToken( out identifier, idFilter, expected ) ) return false;

                SqlToken asToken;
                R.IsToken( out asToken, SqlTokenType.As, false );

                SqlExprTypeDecl typeDecl;
                if( !IsTypeDecl( out typeDecl, true ) ) return false;

                declVar = new SqlExprTypedIdentifier( identifier, asToken, typeDecl );
                return true;
            }

            /// <summary>
            /// Is a SqlExprTypeDecl: either a SqlDbType (int, sql_variant) or multiple identifiers that is a user defined type.
            /// </summary>
            /// <returns></returns>
            bool IsTypeDecl( out SqlExprTypeDecl typeDecl, bool expected = true )
            {
                typeDecl = null;
                SqlTokenIdentifier id;
                if( R.IsToken( out id, t => t.IsDbType, false ) )
                {
                    Debug.Assert( SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ).HasValue, "TokenType has been mapped to a SqlDbType." );

                    #region Type mapped to SqlDbType.
                    SqlDbType dbType = SqlKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ).Value;
                    switch( dbType )
                    {
                        case SqlDbType.Date:
                        case SqlDbType.DateTime:
                        case SqlDbType.SmallDateTime:
                            {
                                typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDateAndTime( id, dbType ) );
                                break;
                            }
                        case SqlDbType.Time:
                        case SqlDbType.DateTime2:
                        case SqlDbType.DateTimeOffset:
                            {
                                SqlTokenTerminal openPar, closePar;
                                if( R.IsToken( out openPar, SqlTokenType.OpenPar, false ) )
                                {
                                    SqlTokenLiteralInteger fractSecond;
                                    if( !R.IsToken( out fractSecond, true ) ) return false;
                                    if( fractSecond.Value > 7 )
                                    {
                                        R.SetCurrentError( "Fractional seconds precision must be less or equal to 7." );
                                        return false;
                                    }
                                    if( !R.IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;
                                    typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDateAndTime( id, openPar, fractSecond, closePar, dbType ) );
                                }
                                else typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDateAndTime( id, dbType ) );
                                break;
                            }
                        case SqlDbType.Decimal:
                            {
                                SqlTokenTerminal openPar, comma, closePar;
                                if( R.IsToken( out openPar, SqlTokenType.OpenPar, false ) )
                                {
                                    SqlTokenLiteralInteger precision;
                                    if( !R.IsToken( out precision, true ) ) return false;
                                    if( precision.Value > 38 )
                                    {
                                        R.SetCurrentError( "Precision must be less or equal to 38." );
                                        return false;
                                    }
                                    if( R.IsToken( out comma, SqlTokenType.Comma, false ) )
                                    {
                                        SqlTokenLiteralInteger scale;
                                        if( !R.IsToken( out scale, true ) ) return false;
                                        if( scale.Value > precision.Value )
                                        {
                                            R.SetCurrentError( "Scale must be less or equal to Precision." );
                                            return false;
                                        }
                                        if( !R.IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;
                                        typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDecimal( id, openPar, precision, comma, scale, closePar ) );
                                    }
                                    else
                                    {
                                        if( !R.IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;
                                        typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDecimal( id, openPar, precision, closePar ) );
                                    }
                                }
                                else typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDecimal( id ) );
                                break;
                            }
                        case SqlDbType.Char:
                        case SqlDbType.VarChar:
                        case SqlDbType.NChar:
                        case SqlDbType.NVarChar:
                        case SqlDbType.Binary:
                        case SqlDbType.VarBinary:
                        case SqlDbType.Float:
                            {
                                SqlTokenTerminal openPar, closePar;
                                if( R.IsToken( out openPar, SqlTokenType.OpenPar, false ) )
                                {
                                    SqlTokenIdentifier sizeMax;
                                    SqlTokenLiteralInteger size = null;
                                    if( !R.IsToken( out sizeMax, SqlTokenType.Max, false ) && !R.IsToken( out size, true ) ) return false;
                                    if( size != null && size.Value == 0 )
                                    {
                                        R.SetCurrentError( "Size can not be 0." );
                                        return false;
                                    }
                                    if( !R.IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;
                                    typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclWithSize( id, openPar, (SqlToken)size ?? sizeMax, closePar, dbType ) );
                                }
                                else typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclWithSize( id, dbType ) );
                                break;
                            }
                        default:
                            {
                                typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclSimple( id ) );
                                break;
                            }
                    }
                    #endregion
                    Debug.Assert( typeDecl != null );
                }
                else
                {
                    SqlExprTypeDeclUserDefined udt;
                    if( !IsTypeDeclUserDefined( out udt, expected ) ) return false;
                    typeDecl = new SqlExprTypeDecl( udt );
                }
                return true;
            }

            bool IsMonoIdentifier( out SqlExprIdentifier id, bool expected = true )
            {
                id = null;
                SqlTokenIdentifier token;
                if( !R.IsToken( out token, expected ) ) return false;
                id = new SqlExprIdentifier( token );
                return true;
            }

            /// <summary>
            /// A Userd defined type is simply multiple identifiers.
            /// </summary>
            bool IsTypeDeclUserDefined( out SqlExprTypeDeclUserDefined udt, bool expected = true )
            {
                udt = null;
                ISqlItem[] multi;
                if( !IsMultipleIdentifierArray( out multi, expected ) ) return false;
                udt = new SqlExprTypeDeclUserDefined( multi );
                return true;
            }

            bool IsExecuteAs( out SqlNoExprExecuteAs execAs, bool expected = false )
            {
                execAs = null;
                SqlTokenIdentifier execToken;
                if( !R.IsToken( out execToken, SqlTokenType.Execute, expected ) ) return false;

                SqlTokenIdentifier asToken;
                if( !R.IsToken( out asToken, SqlTokenType.As, true ) ) return false;

                SqlToken right;
                if( !R.IsToken( out right, true ) ) return false;

                execAs = new SqlNoExprExecuteAs( execToken, asToken, right );
                return true;
            }

            ISqlItem EatExecuteAs()
            {
                SqlNoExprExecuteAs execAs;
                if( IsExecuteAs( out execAs, false ) ) return execAs;
                return R.Current;
            }

            bool IsMultiIdentifier( out SqlExprMultiIdentifier id, bool expected, SqlTokenIdentifier firstForLookup = null )
            {
                id = null;
                ISqlItem[] multi;
                if( !IsMultipleIdentifierArray( out multi, expected, firstForLookup ) ) return false;
                id = new SqlExprMultiIdentifier( false, multi );
                return true;
            }

            /// <summary>
            /// Reads a <see cref="SqlExprIdentifier"/> or a <see cref="SqlExprMultiIdentifier"/> depending
            /// on the dots. Both of them supports <see cref="ISqlIdentifier"/>.
            /// </summary>
            /// <param name="id">The expression. Not null on success.</param>
            /// <param name="expected">True to set an error if the current token(s) can not be read as one or more identifiers.</param>
            /// <returns>True on success. False if the current token(s) can not be read as one or more identifiers.</returns>
            bool IsMonoOrMultiIdentifier( out SqlExpr id, bool expected, SqlTokenIdentifier firstForLookup = null )
            {
                id = null;
                ISqlItem[] multi;
                if( !IsMultipleIdentifierArray( out multi, expected, firstForLookup ) ) return false;
                if( multi.Length == 1 ) id = new SqlExprIdentifier( (SqlTokenIdentifier)multi[0] );
                else id = new SqlExprMultiIdentifier( false, multi );
                return true;
            }

            class StarTransformer : IEnumerator<SqlToken>
            {
                readonly IEnumerator<SqlToken> _r;
                SqlToken _current;

                public StarTransformer( IEnumerator<SqlToken> r )
                {
                    _r = r;
                }

                internal static SqlTokenIdentifier FromMultToken( SqlToken mult )
                {
                    Debug.Assert( mult.TokenType == SqlTokenType.Mult );
                    return new SqlTokenIdentifier( SqlTokenType.IdentifierStar, "*", mult.LeadingTrivia, mult.TrailingTrivia );
                }

                public SqlToken Current
                {
                    get { return _current ?? ((_current = _r.Current).TokenType == SqlTokenType.Mult ? _current = FromMultToken( _current ) : _current); }
                }

                public void Dispose()
                {
                    _r.Dispose();
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    _current = null;
                    return _r.MoveNext();
                }

                public void Reset()
                {
                    _r.Reset();
                }
            }

            bool IsMultipleIdentifierArray( out ISqlItem[] multi, bool expected, SqlTokenIdentifier firstForLookup = null )
            {
                multi = null;
                if( firstForLookup != null || ( R.Current.TokenType & SqlTokenType.IsIdentifier ) != 0 || R.Current.TokenType == SqlTokenType.Mult )
                {
                    string error = SqlExprMultiIdentifier.BuildArray( new StarTransformer( R ), out multi, firstForLookup );
                    if( error != null ) return R.SetCurrentError( error );
                    return true;
                }
                if( expected ) R.SetCurrentError( "Expected identifier." );
                return false;
            }

            /// <summary>
            /// Collects tokens in an <see cref="SqlExprUnmodeledItems"/> until a given token is found.
            /// </summary>
            /// <typeparam name="T">Type of the stopper token.</typeparam>
            /// <param name="items">An unmodeled list of tokens. Null if the stopper occurs immediately or an error occurred on the first token.</param>
            /// <param name="stopper">Stopper eventually found. Null if the end of input or an error has been encountered.</param>
            /// <param name="stopperDefinition">Predicate that defines the stop.</param>
            /// <param name="matchers">
            /// Optional functions that can transform the current token (and its followers) to any item. 
            /// Matchers are called up to the first one that returns an item different than the Current token.
            /// When a matcher returns null, the current token is ignored.
            /// </param>
            /// <returns>True if no error occurred. The stopper is null if the end of input has been encountered.</returns>
            bool IsUnmodeledUntil<T>( out SqlExprUnmodeledItems items, out T stopper, Predicate<T> stopperDefinition, params Func<ISqlItem>[] matchers ) where T : SqlToken
            {
                Debug.Assert( stopperDefinition != null );
                items = null;
               
                List<ISqlItem> all;
                if( !R.IsItemList( out all, out stopper, stopperDefinition, false, matchers ) ) return false;

                if( all != null && all.Count > 0 ) items = new SqlExprUnmodeledItems( all );
                return true;
            }

        }

    
}

