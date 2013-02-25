using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.SqlServer
{
        public class ExprAnalyser
        {
            readonly TokenReader R;

            ExprAnalyser( IEnumerable<SqlToken> tokens )
            {
                R = new TokenReader( tokens );
            }

            public static SqlExpr Analyse( IEnumerable<SqlToken> tokens )
            {
                if( tokens == null ) throw new ArgumentNullException( "tokens" );
                return new ExprAnalyser( tokens ).Expression( 0 );
            }

            SqlExpr Expression( int rightBindingPower )
            {
                if( R.IsErrorOrEndOfInput )
                {
                    return new SqlExprSyntaxError( (SqlTokenError)R.Current );
                }
                SqlExpr left = HandleNud();
                while( !(left is SqlExprSyntaxError) && rightBindingPower < R.CurrentPrecedenceLevel )
                {
                    left = HandleLed( left );
                }
                return left;
            }

            SqlExpr HandleNud()
            {
                Debug.Assert( !R.IsErrorOrEndOfInput );
                // Handles strings and numbers.
                if( (R.Current.TokenType & SqlTokenType.LitteralMask) != 0 ) return new SqlExprLiteral( R.Read<SqlTokenBaseLiteral>() );
                if( R.Current.TokenType == SqlTokenType.IdentifierReservedKeyword ) return HandleUnquotedKeyword( R.Read<SqlTokenIdentifier>() );
                if( (R.Current.TokenType & SqlTokenType.IsIdentifier) != 0 ) return new SqlExprIdentifier( R.Read<SqlTokenIdentifier>() );

                if( R.Current.TokenType == SqlTokenType.Minus ) return new SqlExprUnaryOperator( R.Read<SqlTokenTerminal>(), Expression( 0 ) );
                if( R.Current.TokenType == SqlTokenType.OpenPar )
                {
                    SqlExpr e = Expression( 0 );
                    return R.Match( SqlTokenType.ClosePar ) ? e : new SqlExprSyntaxError( "Expected )." );
                }
                return new SqlExprSyntaxError( "Unexpected token: " + R.Current.ToString() );
            }

            SqlExpr HandleLed( SqlExpr left )
            {
                if( (R.Current.TokenType & SqlTokenType.IsAssignOperator) != 0 )
                {
                    if( !(left is SqlExprIdentifier) ) return new SqlExprSyntaxError( "Assignment must follow an identifier." );
                    return new SqlAssignExpr( (SqlExprIdentifier)left, R.Read<SqlTokenTerminal>(), Expression( 0 ) );
                }
                return new SqlExprSyntaxError( "Unexpected token: " + left.ToString() );
            }

            SqlExpr HandleUnquotedKeyword( SqlTokenIdentifier id )
            {
                if( id.NameEquals( "null" ) ) return new SqlExprNull( id );

                if( id.NameEquals( "begin" ) )
                {
                    SqlExprStatementList body;
//                    if( !IsStatementList( out body ) ) return R.ExtractError();
                    SqlTokenIdentifier end;
                    if( !IsUnquotedKeyword( out end, "end", true ) ) return R.ExtractError();
//                    return new SqlExprStBlock( id, body, end );
                }
                if( id.NameEquals( "create" ) || id.NameEquals( "alter" ) )
                {
                    SqlTokenIdentifier type;
                    SqlExprMultiIdentifier name;
                    if( !IsUnquotedKeyword( out type ) || !IsMultiIdentifier( out name ) ) return R.ExtractError();
                    if( type.NameEquals( "procedure" ) || type.NameEquals( "proc" ) )
                    {
//                        return HandleAlterOrCreateProcedure( id, type, name );
                    }
                    if( type.NameEquals( "view" ) )
                    {
                        return HandleAlterOrCreateView( id, type, name );
                    }
                    if( type.NameEquals( "function" ) )
                    {
                        return HandleAlterOrCreateFunction( id, type, name );
                    }
//                    return HandleAlterOrCreateSomething( id, type, name );
                }
                return new SqlExprIdentifier( id );
            }

            bool IsStatement( out SqlExprBaseSt statement, bool expected = true )
            {
                statement = null;
                if( R.Current.TokenType != SqlTokenType.IdentifierReservedKeyword )
                {
                    if( expected ) R.SetLastError( "Statement expected." );
                    return false;
                }
                SqlTokenIdentifier id = (SqlTokenIdentifier)R.Current;
                if( id.NameEquals( "begin" ) )
                {
                    R.MoveNext();
                    SqlExprStatementList body;
//                    if( !IsStatementList( out body ) ) return false;
                    SqlTokenIdentifier end;
                    if( !IsUnquotedKeyword( out end, "end", true ) ) return false;
//                    statement = new SqlExprStBlock( id, body, end );
                    return true;
                }
                if( id.NameEquals( "create" ) || id.NameEquals( "alter" ) )
                {
                    R.MoveNext();
                    SqlTokenIdentifier type;
                    if( !IsUnquotedKeyword( out type ) ) return false;
                    if( type.NameEquals( "procedure" ) || type.NameEquals( "proc" ) )
                    {
                        SqlExprStStoredProc sp;
                        if( !IsStoredProcedure( out sp, id, type ) ) return false;
                        statement = sp;
                    }
                    //return HandleAlterOrCreateSomething( id, type );
                }
                if( id.NameEquals( "set" ) )
                {
                    R.MoveNext();
                    SqlExprStUnmodeled st;
//                    if( !IsUnmodeledStatement( out st, id ) ) return false;
                }

                if( expected ) R.SetLastError( "Unknown statement: {0}.", R.Current.ToString() );
                return false;
            }

            //private bool IsUnmodeledStatement( out SqlExprStUnmodeled st, SqlTokenIdentifier id )
            //{
            //    SqlTokenTerminal term;
            //    List<SqlExpr> expressions = new List<SqlExpr>();
            //    while( !IsToken( out term, SqlTokenType.SemiColon, false ) )
            //    {

            //    }

            //    SqlExpr e = Expression( 0 );

            //}

            bool IsStoredProcedure( out SqlExprStStoredProc sp, SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type )
            {
                sp = null;

                SqlExprMultiIdentifier name;
                if( !IsMultiIdentifier( out name, true ) ) return false;

                SqlExprParameterList parameters;
                if( !IsParameterList( out parameters, requiresParenthesis: false ) ) return false;

                SqlExprUnmodeledTokens options;
                SqlTokenIdentifier asToken;
                if( !ReadUntil( out options, out asToken, t => t.IsUnquotedKeyword && t.NameEquals( "as" ) ) ) return false;

                SqlTokenIdentifier begin, end = null;
                IsUnquotedKeyword( out begin, "begin", false );

                SqlExprStatementList bodyStatements;
//                if( !IsStatementList( out bodyStatements ) ) return false;

                if( begin != null ) IsUnquotedKeyword( out end, "end", true );

                SqlTokenTerminal term;
                IsToken( out term, SqlTokenType.SemiColon, false );

                //if( begin == null )
                //{
                //    sp = new SqlExprStStoredProc( alterOrCreate, type, name, parameters, options, asToken, bodyStatements, term );
                //}
                //else
                //{
                //    sp = new SqlExprStStoredProc( alterOrCreate, type, name, parameters, options, asToken, begin, bodyStatements, end, term );
                //}
                return true;
            }

            //private bool IsStatementList( out SqlExprStatementList l )
            //{

            //}

            bool IsParameterList( out SqlExprParameterList parameters, bool requiresParenthesis )
            {
                parameters = null;
                
                SqlTokenTerminal openPar;
                if( !IsToken( out openPar, SqlTokenType.OpenPar, requiresParenthesis ) && requiresParenthesis ) return false;

                var exprs = new List<IAbstractExpr>();
                SqlExprParameter param;
                if( IsParameter( out param, false ) )
                {
                    exprs.Add( param );
                    SqlTokenTerminal comma;
                    while( IsToken( out comma, SqlTokenType.Comma, false ) )
                    {
                        exprs.Add( comma );
                        IsParameter( out param, true );
                        exprs.Add( param );
                    }
                }
                
                SqlTokenTerminal closePar = null;
                if( openPar != null && !IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;

                parameters = new SqlExprParameterList( openPar, closePar, exprs.ToArray() );
                return true;
            }

            private SqlExpr HandleAlterOrCreateFunction( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name )
            {
                throw new NotImplementedException();
            }

            private SqlExpr HandleAlterOrCreateView( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name )
            {
                throw new NotImplementedException();
            }

            bool IsParameter( out SqlExprParameter parameter, bool expected = true )
            {
                parameter = null;
                SqlExprTypedIdentifier declVar;
                if( !IsTypedIdentifer( out declVar, t => t.IsVariable, expected ) ) return false;

                SqlExprParameterDefaultValue defValue = null;
                {
                    SqlTokenTerminal assign;
                    if( IsToken( out assign, SqlTokenType.Assign, false ) )
                    {
                        SqlTokenIdentifier variable;
                        if( IsToken( out variable, SqlTokenType.IdentifierVariable, false ) )
                        {
                            defValue = new SqlExprParameterDefaultValue( assign, variable );
                        }
                        else
                        {
                            SqlTokenTerminal minusSign;
                            IsToken( out minusSign, false );
                            SqlTokenBaseLiteral value;
                            if( !IsToken( out value, true ) ) return false;
                            defValue = new SqlExprParameterDefaultValue( assign, minusSign, value );
                        }
                    }
                }

                SqlTokenIdentifier outputClause;
                IsUnquotedKeyword( out outputClause, t => t.NameEquals( "out" ) || t.NameEquals( "output" ), false );

                SqlTokenIdentifier readonlyClause;
                IsUnquotedKeyword( out readonlyClause, t => t.NameEquals( "readonly" ) );

                parameter = new SqlExprParameter( declVar, defValue, outputClause, readonlyClause );
                return true;
            }

            bool IsTypedIdentifer( out SqlExprTypedIdentifier declVar, Predicate<SqlTokenIdentifier> idFilter, bool expected = true )
            {
                declVar = null;
                SqlTokenIdentifier identifier;
                if( !IsToken( out identifier, idFilter, expected ) ) return false;

                SqlExprTypeDecl typeDecl;
                if( !IsTypeDecl( out typeDecl, true ) ) return false;

                declVar = new SqlExprTypedIdentifier( identifier, typeDecl );
                return true;
            }

            bool IsTypeDecl( out SqlExprTypeDecl typeDecl, bool expected = true )
            {
                typeDecl = null;
                SqlTokenIdentifier id;
                if( IsToken( out id, t => t.IsTypeName, false ) )
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
                                if( IsToken( out openPar, SqlTokenType.OpenPar, false ) )
                                {
                                    SqlTokenLiteralInteger fractSecond;
                                    if( !IsToken( out fractSecond, true ) ) return false;
                                    if( fractSecond.Value > 7 )
                                    {
                                        R.SetLastError( "Fractional seconds precision must be less or equal to 7." );
                                        return false;
                                    }
                                    if( !IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;
                                    typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDateAndTime( id, openPar, fractSecond, closePar, dbType ) );
                                }
                                else typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDateAndTime( id, dbType ) );
                                break;
                            }
                        case SqlDbType.Decimal:
                            {
                                SqlTokenTerminal openPar, comma, closePar;
                                if( IsToken( out openPar, SqlTokenType.OpenPar, false ) )
                                {
                                    SqlTokenLiteralInteger precision;
                                    if( !IsToken( out precision, true ) ) return false;
                                    if( precision.Value > 38 )
                                    {
                                        R.SetLastError( "Precision must be less or equal to 38." );
                                        return false;
                                    }
                                    if( IsToken( out comma, SqlTokenType.Comma, false ) )
                                    {
                                        SqlTokenLiteralInteger scale;
                                        if( !IsToken( out scale, true ) ) return false;
                                        if( scale.Value > precision.Value )
                                        {
                                            R.SetLastError( "Scale must be less or equal to Precision." );
                                            return false;
                                        }
                                        if( !IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;
                                        typeDecl = new SqlExprTypeDecl( new SqlExprTypeDeclDecimal( id, openPar, precision, comma, scale, closePar ) );
                                    }
                                    else
                                    {
                                        if( !IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;
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
                                if( IsToken( out openPar, SqlTokenType.OpenPar, false ) )
                                {
                                    SqlTokenIdentifier sizeMax;
                                    SqlTokenLiteralInteger size = null;
                                    if( !IsUnquotedKeyword( out sizeMax, "max", false ) && !IsToken( out size, true ) ) return false;
                                    if( size != null && size.Value == 0 )
                                    {
                                        R.SetLastError( "Size can not be 0." );
                                        return false;
                                    }
                                    if( !IsToken( out closePar, SqlTokenType.ClosePar, true ) ) return false;
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

            bool IsUnquotedKeyword( out SqlTokenIdentifier keyword, string name, bool setLastError = true )
            {
                Predicate<SqlTokenIdentifier> p = null;
                if( name != null ) p = (t => t.NameEquals( name ));
                return IsUnquotedKeyword( out keyword, p, setLastError );
            }

            bool IsUnquotedKeyword( out SqlTokenIdentifier keyword, Predicate<SqlTokenIdentifier> filter = null, bool expected = true )
            {
                if( R.Current.TokenType == SqlTokenType.IdentifierReservedKeyword )
                {
                    SqlTokenIdentifier t = (SqlTokenIdentifier)R.Current;
                    if( filter == null || filter( t ) )
                    {
                        keyword = t;
                        R.MoveNext();
                        return true;
                    }
                }
                if( expected ) R.SetLastError( "Reserved Keyword expected." );
                keyword = null;
                return false;
            }

            bool IsMonoIdentifier( out SqlExprIdentifier id, bool expected = true )
            {
                id = null;
                SqlTokenIdentifier token;
                if( !IsToken( out token, expected ) ) return false;
                id = new SqlExprIdentifier( token );
                return true;
            }

            bool IsTypeDeclUserDefined( out SqlExprTypeDeclUserDefined udt, bool expected = true )
            {
                udt = null;
                IAbstractExpr[] multi;
                if( !IsMultipleIdentifierArray( out multi, expected ) ) return false;
                udt = new SqlExprTypeDeclUserDefined( multi );
                return true;
            }

            bool IsMultiIdentifier( out SqlExprMultiIdentifier id, bool expected = true )
            {
                id = null;
                IAbstractExpr[] multi;
                if( !IsMultipleIdentifierArray( out multi, expected ) ) return false;
                id = new SqlExprMultiIdentifier( multi );
                return true;
            }

            bool IsMultipleIdentifierArray( out IAbstractExpr[] multi, bool expected = true )
            {
                multi = null;
                if( (R.Current.TokenType & SqlTokenType.IsIdentifier) != 0 )
                {
                    string error = SqlExprMultiIdentifier.BuildArray( R, out multi );
                    if( error != null ) return R.SetLastError( error );
                    return true;
                }
                if( expected ) R.SetLastError( "Expected identifier." );
                return false;
            }

            /// <summary>
            /// Collects tokens in an <see cref="SqlExprUnmodeledTokens"/> until a given token is found.
            /// </summary>
            /// <typeparam name="T">Type of the stopper token.</typeparam>
            /// <param name="tokens">An unmodeled list of tokens. Null if the stopper occurs immediately.</param>
            /// <param name="stopper">Stopper eventually found.</param>
            /// <param name="stopperDefinition">Predicate that defines the stop.</param>
            /// <returns>True if a stopper has been found. False if an error or the end of input has been encountered.</returns>
            bool ReadUntil<T>( out SqlExprUnmodeledTokens tokens, out T stopper, Predicate<T> stopperDefinition ) where T : SqlToken
            {
                Debug.Assert( stopperDefinition != null );
                tokens = null;
                stopper = null;

                List<SqlToken> all = new List<SqlToken>();
                while( !IsToken( out stopper, stopperDefinition ) )
                {
                    if( R.IsErrorOrEndOfInput )
                    {
                        if( R.LastError == null ) R.SetLastError( R.Current.TokenType == SqlTokenType.EndOfInput ? "Unexpected end of input." : R.Current.ToString() );
                        return false;
                    }
                    all.Add( R.Current );
                    R.MoveNext();
                }
                if( all.Count > 0 ) tokens = new SqlExprUnmodeledTokens( all );
                return true;
            }

            bool IsToken<T>( out T t, bool expected = true ) where T : SqlToken
            {
                return IsToken( out t, null, expected );
            }

            bool IsToken<T>( out T t, Predicate<T> filter = null, bool expected = true ) where T : SqlToken
            {
                t = R.Current as T;
                if( t != null && (filter == null || filter(t)) )
                {
                    t = R.Read<T>();
                    return true;
                }
                if( expected ) R.SetLastError( "Expected '{0}'. ", typeof( T ).Name.Replace( "SqlToken", String.Empty ) );
                t = null;
                return false;
            }

            bool IsToken<T>( out T t, SqlTokenType type, bool expected = true ) where T : SqlToken
            {
                if( R.Current is T && R.Current.TokenType == type )
                {
                    t = R.Read<T>();
                    return true;
                }
                if( expected ) R.SetLastError( "Expected token '{0}'. ", type );
                t = null;
                return false;
            }



        }

    
}

