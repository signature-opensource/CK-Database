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
            //bool IsSelectQuery( out SelectQuery e, bool expected )
            //{
            //    e = null;
            //    ISelectSpecification spec;
            //    SelectContinuation c;
            //    if( !IsSelectSpecification( out spec, out c, expected ) ) return false;
            //    return false;
            //}

            //bool IsSelectSpecification( out ISelectSpecification e, out SelectContinuation c, bool expected )
            //{
            //    e = null;
            //    SelectSpecification nud;
            //    if( !IsSelectSpecificationInternal( out nud, out c, expected ) ) return false;
            //    e = nud;
            //    Debug.Assert( SqlTokenType.Intersect > SqlTokenType.Except && SqlTokenType.Except > SqlTokenType.Union, "Order matters: it is the precedence level." );
            //    while( (c & SelectContinuation.IsCombination) != 0 && (int)c > (int)e.CombinationKind )
            //    {
            //        SqlTokenTerminal op = R.Read<SqlTokenTerminal>();
            //        SqlTokenIdentifier allOrDistinct = null;
            //        if( op.TokenType == SqlTokenType.Union )
            //        {
            //            if( !R.IsUnquotedKeyword( out allOrDistinct, "all", false ) ) R.IsUnquotedKeyword( out allOrDistinct, "distinct", false );
            //        }
            //        ISelectSpecification led;
            //        if( !IsSelectSpecification( out led, out c, true ) ) return false;
            //        e = new SelectCombineOperator( e, op, allOrDistinct, led );
            //    }
            //    return true;
            //}

            bool IsSelectSpecification( out SelectSpecification e, bool allowExtension, bool expected )
            {
                e = null;
                SelectHeader header;
                SelectColumnList columns;
                if( !IsSelectHeader( out header, expected ) ) return false;
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
                    SelectOrderBy orderBy = null;
                    SelectFor forPart = null;                   
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
                        if( !IsExpressionOrRawList( out content, SelectPartStopper, true ) ) return false;
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
                        if( !R.IsUnquotedKeyword( out by, "by", true ) ) return false;
                        if( !IsExpressionOrRawList( out content, SelectPartStopper, true ) ) return false;
                        if( R.IsUnquotedKeyword( out having, "having", false ) )
                        {
                            if( !IsOneExpression( out havingClause, false ) ) return false;
                        }
                        groupBy = new SelectGroupBy( partName, by, content, having, havingClause );
                        c = IsSpecificationPart( R.Current );
                    }
                    if( allowExtension && (c == SpecificationPart.Order || c == SpecificationPart.For) )
                    {
                        if( !IsSelectSpecificationExtension( out orderBy, out forPart ) ) return false;
                    }
                    e = new SelectSpecification( header, columns, into, from, where, groupBy, orderBy, forPart );
                }
                return true;
            }

            private bool IsSelectSpecificationExtension( out SelectOrderBy orderBy, out SelectFor forPart )
            {
                orderBy = null;
                forPart = null;
                if( SqlToken.IsUnquotedIdentifier( R.Current, "order" ) )
                {
                    SqlTokenIdentifier partName = R.Read<SqlTokenIdentifier>();
                    SqlTokenIdentifier by;
                    SqlExpr content;
                    if( !R.IsUnquotedKeyword( out by, "by", true ) ) return false;
                    if( !IsExpressionOrRawList( out content, SelectPartStopper, true ) ) return false;
                    orderBy = new SelectOrderBy( partName, by, content );
                }
                if( SqlToken.IsUnquotedIdentifier( R.Current, "for" ) )
                {
                    SqlTokenIdentifier partName = R.Read<SqlTokenIdentifier>();
                    SqlExpr content;
                    if( !IsExpressionOrRawList( out content, SelectPartStopper, true ) ) return false;
                    forPart = new SelectFor( partName, content );
                }
                return true;
            }

            bool IsSelectColumnList( out SelectColumnList e, bool expectAtLeastOne )
            {
                e = null;
                SqlTokenOpenPar openPar;
                List<ISqlItem> items;
                SqlTokenClosePar closePar;
                if( !IsCommaList<SelectColumn>( out openPar, out items, out closePar, false, MatchColumn ) ) return false;
                if( openPar != null ) return R.SetCurrentError( "Unexpected parenthesis." );
                if( expectAtLeastOne && items.Count == 0 ) return R.SetCurrentError( "Expected a column definition." );
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
                    if( eA != null ) column = new SelectColumn( eA.Identifier, eA.AssignToken, eA.Right );
                    else
                    {
                        SqlTokenIdentifier asToken;
                        SqlExprIdentifier colName = null;
                        if( R.IsUnquotedKeyword( out asToken, "as", false ) )
                        {
                            if( !IsMonoIdentifier( out colName, true ) ) return false;
                            column = new SelectColumn( e, asToken, colName );
                        }
                        if( IsPossibleColumnDefinition( R.Current ) && IsMonoIdentifier( out colName, false ) )
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

            bool SelectPartStopper( SqlToken t )
            {
                return t.TokenType == SqlTokenType.EndOfInput
                        || (t.TokenType & SqlTokenType.IsSelectPart) != 0
                        || SqlToken.IsCloseParenthesisOrTerminator( t )
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
                Group = 4,
                Order = 5,
                For = 6
            }

            SpecificationPart IsSpecificationPart( SqlToken t )
            {
                SpecificationPart c = SpecificationPart.None;
                SqlTokenIdentifier id = R.Current as SqlTokenIdentifier;
                if( id != null && !id.IsQuoted )
                {
                    if( id.NameEquals( "into" ) ) c = SpecificationPart.Into;
                    else if( id.NameEquals( "from" ) ) c = SpecificationPart.From;
                    else if( id.NameEquals( "where" ) ) c = SpecificationPart.Where;
                    else if( id.NameEquals( "group" ) ) c = SpecificationPart.Group;
                    else if( id.NameEquals( "order" ) ) c = SpecificationPart.Order;
                    else if( id.NameEquals( "for" ) ) c = SpecificationPart.For;
                }
                return c;
            }

            bool IsSelectHeader( out SelectHeader e, bool expected )
            {
                e = null;
                SqlTokenIdentifier select;
                SqlTokenIdentifier allOrDistinct = null;
                SqlTokenIdentifier top = null;
                SqlExpr topExpression = null;
                SqlTokenIdentifier percent = null;
                SqlTokenIdentifier with = null;
                SqlTokenIdentifier ties = null;

                if( !R.IsUnquotedKeyword( out select, "select", expected ) ) return false;
                if( !R.IsUnquotedKeyword( out allOrDistinct, "all", false ) ) R.IsUnquotedKeyword( out allOrDistinct, "distinct", false );
                if( R.IsUnquotedKeyword( out top, "top", false ) )
                {
                    SqlTokenLiteralInteger intVal;
                    if( R.IsToken( out intVal, false ) )
                    {
                        topExpression = new SqlExprLiteral( intVal );
                        topExpression.MutableEnclose( SqlTokenOpenPar.OpenPar, SqlTokenOpenPar.ClosePar );
                    }
                    else if( !IsOneExpression( out topExpression, true ) ) return false;
                    if( R.IsUnquotedKeyword( out percent, "percent", false ) )
                    {
                        if( R.IsUnquotedKeyword( out with, "with", false ) ) R.IsUnquotedKeyword( out ties, "ties", true );
                    }
                }
                e = new SelectHeader( select, allOrDistinct, top, topExpression, percent, with, ties );
                return true;
            }

        }

    
}

