using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;
using CK.SqlServer;

namespace CK.SqlServer
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
                /// <param name="logLevel">Log level to use.</param>
                /// <param name="logger">Logger to log into.</param>
                public void LogOnError( LogLevel logLevel, IActivityLogger logger )
                {
                    if( logger == null ) throw new ArgumentNullException( "logger" );
                    if( IsError )
                    {
                        using( logger.OpenGroup( logLevel, ErrorMessage ) )
                        {
                            logger.Info( HeadSource );
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
                R = new SqlTokenReader( t.Parse( text ), t.ToString );
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
                    if( expected ) R.SetCurrentError( "Statement expected." );
                    return false;
                }
                if( !id.IsKeywordName )
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
                    statement = new SqlExprStLabelDef( id, colon );
                }
                if( id.NameEquals( "end" ) )
                {
                    if( expected ) R.SetCurrentError( "Statement expected." );
                    return false;
                }
                if( id.NameEquals( "begin" ) )
                {
                    R.MoveNext();
                    SqlExprStatementList body;
                    if( !IsStatementList( out body, true ) ) return false;
                    SqlTokenIdentifier end;
                    if( !R.IsUnquotedReservedKeyword( out end, "end", true ) ) return false;
                    statement = new SqlExprStBlock( id, body, end );
                    return true;
                }
                if( id.NameEquals( "create" ) || id.NameEquals( "alter" ) )
                {
                    R.MoveNext();
                    SqlTokenIdentifier type;
                    if( !R.IsUnquotedReservedKeyword( out type, true ) ) return false;
                    if( type.NameEquals( "procedure" ) || type.NameEquals( "proc" ) )
                    {
                        SqlExprStStoredProc sp;
                        if( !IsStoredProcedure( out sp, id, type ) ) return false;
                        statement = sp;
                        return true;
                    }
                    if( type.NameEquals( "view" ) )
                    {
                        SqlExprStView view;
                        if( !IsView( out view, id, type ) ) return false;
                        statement = view;
                        return true;
                    }
                }
                if( id.NameEquals( "break" ) || id.NameEquals( "continue" ) )
                {
                    R.MoveNext();
                    statement = new SqlExprStMonoStatement( id, GetOptionalTerminator() );
                    return true;
                }

                if( id.NameEquals( "if" ) )
                {
                    R.MoveNext();
                    SqlExpr expr;
                    if( !IsExpression( out expr, 0, true ) ) return false;
                    SqlExprBaseSt thenSt;
                    if( !IsStatement( out thenSt, true ) ) return false;
                    SqlTokenIdentifier elseToken;
                    SqlExprBaseSt elseSt = null;
                    if( R.IsUnquotedReservedKeyword( out elseToken, "else", false ) )
                    {
                        if( !IsStatement( out elseSt, true ) ) return false;
                    }
                    statement = new SqlExprStIf( id, expr, thenSt, elseToken, elseSt, GetOptionalTerminator() );
                    return true;
                }
                R.MoveNext();
                SqlExprStUnmodeled st;
                if( !IsUnmodeledStatement( out st, id ) ) return false;
                statement = st;
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

            /// <summary>
            /// Matches a statement up to the next statement terminator ';'.
            /// </summary>
            bool IsUnmodeledStatement( out SqlExprStUnmodeled st, SqlTokenIdentifier id )
            {
                st = null;
                SqlExprCommaList content;
                if( !IsCommaList( out content, false ) ) return false;
                st = new SqlExprStUnmodeled( id, content, GetOptionalTerminator() );
                return true;
            }

            bool IsView( out SqlExprStView view, SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type )
            {
                view = null;

                SqlExprMultiIdentifier name;
                if( !IsMultiIdentifier( out name, true ) ) return false;

                SqlExprColumnList columns;
                IsColumnList( out columns );

                SqlExprUnmodeledTokens options;
                SqlTokenIdentifier asToken;
                if( !IsUnmodeledUntil( out options, out asToken, t => t.IsUnquotedKeyword && t.NameEquals( "as" ) ) ) return false;

                SqlExprBaseSt selectStatement;
                SqlExprStUnmodeled select;
                if( !IsStatement( out selectStatement, true ) 
                    || (select = selectStatement as SqlExprStUnmodeled) == null 
                    || select.Identifier.NameEquals( "select" ) )
                {
                    return R.SetCurrentError( "Select statement expected." ); 
                }
                view = new SqlExprStView( alterOrCreate, type, name, columns, options, asToken, select, GetOptionalTerminator() );
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

            bool IsStoredProcedure( out SqlExprStStoredProc sp, SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type )
            {
                sp = null;

                SqlExprMultiIdentifier name;
                if( !IsMultiIdentifier( out name, true ) ) return false;

                SqlExprParameterList parameters;
                if( !IsParameterList( out parameters, requiresParenthesis: false ) ) return false;

                SqlExprUnmodeledTokens options;
                SqlTokenIdentifier asToken;
                if( !IsUnmodeledUntil( out options, out asToken, t => t.IsUnquotedKeyword && t.NameEquals( "as" ) ) ) return false;

                SqlTokenIdentifier begin, end = null;
                R.IsUnquotedReservedKeyword( out begin, "begin", false );

                SqlExprStatementList bodyStatements;
                if( !IsStatementList( out bodyStatements, true ) ) return false;

                if( begin != null && !R.IsUnquotedReservedKeyword( out end, "end", true ) ) return false;

                SqlTokenTerminal term = GetOptionalTerminator();
                
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
                        if( R.IsToken( out variable, SqlTokenType.IdentifierVariable, false ) )
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
                R.IsUnquotedIdentifier( out outputClause, "out" , "output", false );

                SqlTokenIdentifier readonlyClause;
                R.IsUnquotedIdentifier( out readonlyClause, "readonly", false );

                parameter = new SqlExprParameter( declVar, defValue, outputClause, readonlyClause );
                return true;
            }

            bool IsTypedIdentifer( out SqlExprTypedIdentifier declVar, Predicate<SqlTokenIdentifier> idFilter, bool expected = true )
            {
                declVar = null;
                SqlTokenIdentifier identifier;
                if( !R.IsToken( out identifier, idFilter, expected ) ) return false;

                SqlExprTypeDecl typeDecl;
                if( !IsTypeDecl( out typeDecl, true ) ) return false;

                declVar = new SqlExprTypedIdentifier( identifier, typeDecl );
                return true;
            }

            bool IsTypeDecl( out SqlExprTypeDecl typeDecl, bool expected = true )
            {
                typeDecl = null;
                SqlTokenIdentifier id;
                if( R.IsToken( out id, t => t.IsTypeName, false ) )
                {
                    Debug.Assert( SqlReservedKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ).HasValue, "TokenType has been mapped to a SqlDbType." );

                    #region Type mapped to SqlDbType.
                    SqlDbType dbType = SqlReservedKeyword.FromSqlTokenTypeToSqlDbType( id.TokenType ).Value;
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
                            {
                                SqlTokenTerminal openPar, closePar;
                                if( R.IsToken( out openPar, SqlTokenType.OpenPar, false ) )
                                {
                                    SqlTokenIdentifier sizeMax;
                                    SqlTokenLiteralInteger size = null;
                                    if( !R.IsUnquotedIdentifier( out sizeMax, "max", false ) && !R.IsToken( out size, true ) ) return false;
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

            bool IsTypeDeclUserDefined( out SqlExprTypeDeclUserDefined udt, bool expected = true )
            {
                udt = null;
                ISqlItem[] multi;
                if( !IsMultipleIdentifierArray( out multi, expected ) ) return false;
                udt = new SqlExprTypeDeclUserDefined( multi );
                return true;
            }

            bool IsMultiIdentifier( out SqlExprMultiIdentifier id, bool expected = true )
            {
                id = null;
                ISqlItem[] multi;
                if( !IsMultipleIdentifierArray( out multi, expected ) ) return false;
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
            bool IsMonoOrMultiIdentifier( out SqlExpr id, bool expected = true )
            {
                id = null;
                ISqlItem[] multi;
                if( !IsMultipleIdentifierArray( out multi, expected ) ) return false;
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

            bool IsMultipleIdentifierArray( out ISqlItem[] multi, bool expected = true )
            {
                multi = null;
                if( (R.Current.TokenType & SqlTokenType.IsIdentifier) != 0 || R.Current.TokenType == SqlTokenType.Mult )
                {
                    string error = SqlExprMultiIdentifier.BuildArray( new StarTransformer( R ), out multi );
                    if( error != null ) return R.SetCurrentError( error );
                    return true;
                }
                if( expected ) R.SetCurrentError( "Expected identifier." );
                return false;
            }

            /// <summary>
            /// Collects tokens in an <see cref="SqlExprUnmodeledTokens"/> until a given token is found.
            /// </summary>
            /// <typeparam name="T">Type of the stopper token.</typeparam>
            /// <param name="tokens">An unmodeled list of tokens. Null if the stopper occurs immediately or an error occured.</param>
            /// <param name="stopper">Stopper eventually found.</param>
            /// <param name="stopperDefinition">Predicate that defines the stop.</param>
            /// <returns>True if a stopper has been found. False if an error or the end of input has been encountered.</returns>
            bool IsUnmodeledUntil<T>( out SqlExprUnmodeledTokens tokens, out T stopper, Predicate<T> stopperDefinition ) where T : SqlToken
            {
                Debug.Assert( stopperDefinition != null );
                tokens = null;
               
                List<SqlToken> all;
                if( !R.IsTokenList( out all, out stopper, stopperDefinition, false ) ) return false;

                if( all != null && all.Count > 0 ) tokens = new SqlExprUnmodeledTokens( all );
                return true;
            }

        }

    
}

