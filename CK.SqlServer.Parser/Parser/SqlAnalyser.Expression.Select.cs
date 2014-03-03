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
        bool MatchSelectSpecification( out SelectSpecification e, SqlTokenIdentifier select, bool allowExtension )
        {
            e = null;
            SelectHeader header;
            SelectColumnList columns;
            if( !MatchSelectHeader( out header, select ) ) return false;
            if( !IsSelectColumnList( out columns, false ) ) return false;

            SpecificationPart c = IsSpecificationPart( R.Current );
            if( c == SpecificationPart.None )
            {
                e = new SelectSpecification( header, columns );
            }
            else
            {
                SelectInto into = null;
                SelectFrom from = null;
                SelectWhere where = null;
                SelectGroupBy groupBy = null;
                if( c == SpecificationPart.Into )
                {
                    SqlTokenIdentifier partName = R.Read<SqlTokenIdentifier>();
                    SqlExprMultiIdentifier table;
                    IsMultiIdentifier( out table, true );
                    into = new SelectInto( partName, table );
                    c = IsSpecificationPart( R.Current );
                }
                if( c == SpecificationPart.From )
                {
                    SqlTokenIdentifier partName = R.Read<SqlTokenIdentifier>();
                    SqlExpr content;
                    if( !IsExpressionOrRawList( out content, SelectPartStopper, false, true ) ) return false;
                    from = new SelectFrom( partName, content );
                    c = IsSpecificationPart( R.Current );
                }
                if( c == SpecificationPart.Where )
                {
                    SqlTokenIdentifier partName = R.Read<SqlTokenIdentifier>();
                    SqlExpr whereCond;
                    if( !IsOneExpression( out whereCond, false ) ) return false;
                    where = new SelectWhere( partName, whereCond );
                    c = IsSpecificationPart( R.Current );
                }
                if( c == SpecificationPart.Group )
                {
                    SqlTokenIdentifier partName = R.Read<SqlTokenIdentifier>();
                    SqlTokenIdentifier by;
                    SqlExpr content;
                    SqlTokenIdentifier having;
                    SqlExpr havingClause = null;
                    if( !R.IsToken( out by, SqlTokenType.By, true ) ) return false;
                    if( !IsExpressionOrRawList( out content, SelectPartStopper, false, true ) ) return false;
                    if( R.IsToken( out having, SqlTokenType.Having, false ) )
                    {
                        if( !IsOneExpression( out havingClause, false ) ) return false;
                    }
                    groupBy = new SelectGroupBy( partName, by, content, having, havingClause );
                    c = IsSpecificationPart( R.Current );
                }
                e = new SelectSpecification( header, columns, into, from, where, groupBy );
            }
            return true;
        }

        bool IsSelectColumnList( out SelectColumnList e, bool expectAtLeastOne )
        {
            e = null;
            List<ISqlItem> items;
            if( !IsCommaListNonEnclosed<SelectColumn>( out items, MatchColumn, expectAtLeastOne ) ) return false;
            e = new SelectColumnList( items );
            return true;
        }

        bool MatchColumn( out SelectColumn column, bool expected )
        {
            column = null;
            if( !IsPossibleColumnDefinition( R.Current ) )
            {
                if( expected ) R.SetCurrentError( "Expected column definition." );
                return false;
            }
            using( R.SetAssignmentContext( true ) )
            {
                SqlExpr e;
                if( !IsOneExpression( out e, parenthesisRequired: false ) ) return false;
                SqlExprAssign eA = e as SqlExprAssign;
                if( eA != null ) column = new SelectColumn( eA.Identifier, eA.AssignTok, eA.Right );
                else
                {
                    SqlTokenIdentifier asToken;
                    SqlExprIdentifier colName = null;
                    if( R.IsToken( out asToken, SqlTokenType.As, false ) )
                    {
                        if( !IsMonoIdentifier( out colName, true ) ) return false;
                        column = new SelectColumn( e, asToken, colName );
                    }
                    else if( IsPossibleColumnDefinition( R.Current ) && IsMonoIdentifier( out colName, false ) )
                    {
                        column = new SelectColumn( e, colName );
                    }
                    else
                    {
                        column = new SelectColumn( e );
                    }
                }
            }
            return true;
        }

        private bool IsOverClause( out SqlNoExprOverClause over )
        {
            over = null;
            SqlTokenIdentifier overToken;
            if( !R.IsToken( out overToken, SqlTokenType.Over, false ) ) return false;
            using( R.SetAssignmentContext( false ) )
            {
                SqlTokenOpenPar openPar;
                SqlExpr overClause;
                if( !R.IsToken( out openPar, true ) ) return false;
                if( !IsExpressionOrRawList( out overClause, openPar, true ) ) return false;
                over = new SqlNoExprOverClause( overToken, overClause );
                return true;
            }
        }

        bool SelectPartStopper( SqlToken t )
        {
            return t.TokenType == SqlTokenType.EndOfInput
                    || SqlToken.IsCloseParenthesisOrTerminatorOrPossibleStartStatement( t )
                    || SqlToken.IsSelectOperator( t.TokenType )
                    || IsSpecificationPart( t ) != SpecificationPart.None
                    || SqlToken.IsUnquotedIdentifier( t, "having", "option" );
        }

        bool IsPossibleColumnDefinition( SqlToken t )
        {
            return !SelectPartStopper( t );
        }

        enum SpecificationPart
        {
            None = 0,
            Into = 1,
            From = 2,
            Where = 3,
            Group = 4
        }

        SpecificationPart IsSpecificationPart( SqlToken t )
        {
            SpecificationPart c = SpecificationPart.None;
            SqlTokenIdentifier id = t as SqlTokenIdentifier;
            if( id != null && !id.IsQuoted )
            {
                if( id.NameEquals( "into" ) ) c = SpecificationPart.Into;
                else if( id.NameEquals( "from" ) ) c = SpecificationPart.From;
                else if( id.NameEquals( "where" ) ) c = SpecificationPart.Where;
                else if( id.NameEquals( "group" ) ) c = SpecificationPart.Group;
            }
            return c;
        }

        bool MatchSelectHeader( out SelectHeader e, SqlTokenIdentifier select )
        {
            e = null;
            SqlTokenIdentifier allOrDistinct = null;
            SqlTokenIdentifier top = null;
            SqlExpr topExpression = null;
            SqlTokenIdentifier percent = null;
            SqlTokenIdentifier with = null;
            SqlTokenIdentifier ties = null;

            if( !R.IsToken( out allOrDistinct, SqlTokenType.All, false ) ) R.IsToken( out allOrDistinct, SqlTokenType.Distinct, false );
            if( R.IsToken( out top, SqlTokenType.Top, false ) )
            {
                SqlTokenLiteralInteger intVal;
                if( R.IsToken( out intVal, false ) )
                {
                    topExpression = new SqlExprLiteral( intVal );
                    topExpression.MutableEnclose( SqlTokenOpenPar.OpenPar, SqlTokenOpenPar.ClosePar );
                }
                else if( !IsOneExpression( out topExpression, true ) ) return false;
                if( R.IsToken( out percent, SqlTokenType.Percent, false ) )
                {
                    if( R.IsToken( out with, SqlTokenType.With, false ) ) R.IsUnquotedIdentifier( out ties, "ties", true );
                }
            }
            e = new SelectHeader( select, allOrDistinct, top, topExpression, percent, with, ties );
            return true;
        }

        bool IsSelectOrderByColumnList( out SelectOrderByColumnList e )
        {
            e = null;
            List<ISqlItem> items;
            if( !IsCommaListNonEnclosed<SelectOrderByColumn>( out items, MatchOrderByColumn, true ) ) return false;
            e = new SelectOrderByColumnList( items );
            return true;
        }

        bool MatchOrderByColumn( out SelectOrderByColumn column, bool expected )
        {
            column = null;
            SqlExpr definition;
            if( !IsOneExpression( out definition, parenthesisRequired: false ) ) return false;
            SqlTokenIdentifier ascOrDesc;
            if( !R.IsToken( out ascOrDesc, SqlTokenType.Asc, false ) ) R.IsToken( out ascOrDesc, SqlTokenType.Desc, false );
            column = ascOrDesc != null ? new SelectOrderByColumn( definition, ascOrDesc ) : new SelectOrderByColumn( definition );
            return true;
        }

        bool IsSelectOrderByOffset( out SelectOrderByOffset e )
        {
            e = null;
            SqlTokenIdentifier offsetToken;
            SqlExpr offsetExpr;
            SqlTokenIdentifier rowsToken;
            if( !R.IsToken( out offsetToken, SqlTokenType.Offset, false ) ) return false;
            if( !IsExpression( out offsetExpr, 0, true ) ) return false;
            if( !R.IsToken( out rowsToken, SqlTokenType.Rows, true ) ) return false;
            SqlTokenIdentifier fetchToken;
            if( R.IsToken( out fetchToken, SqlTokenType.Fetch, false ) )
            {
                SqlTokenIdentifier firstOrNextToken;
                if( !R.IsToken( out firstOrNextToken, SqlTokenType.First, false ) && !R.IsToken( out firstOrNextToken, SqlTokenType.Next, true ) ) return false;
                SqlExpr fetchExpr;
                if( !IsExpression( out fetchExpr, 0, true ) ) return false;
                SqlTokenIdentifier fetchRowsToken;
                if( !R.IsToken( out fetchRowsToken, SqlTokenType.Rows, true ) ) return false;
                SqlTokenIdentifier onlyToken;
                if( !R.IsToken( out onlyToken, SqlTokenType.Only, true ) ) return false;
                e = new SelectOrderByOffset( offsetToken, offsetExpr, rowsToken, fetchToken, firstOrNextToken, fetchExpr, fetchRowsToken, onlyToken );
            }
            else e = new SelectOrderByOffset( offsetToken, offsetExpr, rowsToken );
            return true;
        }
    }
}

