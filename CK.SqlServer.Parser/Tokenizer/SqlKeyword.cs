#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Tokenizer\SqlKeyword.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    public static class SqlKeyword
    {
        #region Arrays of keywords

        /// <summary>
        /// Mapped to SqlTokenType.IdentifierReserved.
        /// </summary>
        static string[] _sqlServerReserved = new string[] 
        {
            "freetext",
            "freetexttable",
            "reconfigure",
            "references",
            "full",
            "replication",
            "browse",
            "restore",
            "bulk",
            "check",
            "holdlock",
            "right",
            "checkpoint",
            "identity",
            "identity_insert",
            "rowcount",
            "save",
            "column",
            "index",
            "schema",
            "inner",
            "securityaudit",
            "compute",
            "constraint",
            "session_user",
            "setuser",
            "join",
            "shutdown",
            "convert",
            "key",
            "statistics",
            "system_user",
            "current",
            "current_date",
            "lineno",
            "tablesample",
            "current_time",
            "load",
            "textsize",
            "current_timestamp",
            "current_user",
            "national",
            "cursor",
            "nullif",
            "default",
            "tsequal",
            "off",
            "deny",
            "offsets",
            "unique",
            "unpivot",
            "disk",
            "opendatasource",
            "openrowset",
            "user",
            "openxml",
            "dump",
            "varying",
            "errlvl",

            "restrict",
            "grant",
            "cascade",
            "revert",
            "revoke",
            "read",
            "backup",
            "any",
            "some",
            "precision",
            "exit",
            "primary",
            "plan",
            "file",
            "fillfactor",
            "public",
            "authorization",
            "openquery",
            "distributed",
            "coalesce",
            "rule",
            "identitycol",
            "rowguidcol",
            "contains",
            "containstable",
            "nocheck",
            "nonclustered",
            "double",
            "outer",

            // These keywords are explicitly associated to a SqlTokenType (OpLevelXX | IdentifierReserved | YY).
            // "or",
            // "and",
            // "not",
            // "between",
            // "in",
            // "is",
            // "like",
            // "union",
            // "intersect",
            // "except",
            // "order",
            // "for",
            // "over",

            // These keywords are explicitly associated to a SqlTokenType (IdentifierStandard | YY).
            // "case",
            // "when",
            // "null",
            // "when",
            // "by",
            // "all",
            // "then",
            // "else",
            // "tran", "transaction",   // Both map to SqlTokenType.Transaction.
            // "with",                  // Considered as a normal reserved keyword (not a IdentifierReservedStart) since it is mandatory to put a ; before it.
            // "proc", "procedure",
            // "function",
            // "view",
            // "table",
            // "database",
            // "trigger",
            // "as",
            // "asc",
            // "desc",
            // "exists",
            // "on",
            // "to",
            // "of",
            // "top",
            // "escape",
            // "into", 
            // "from", 
            // "where",
            // "group",
            // "option",
            // "add",
            // "max",
            // "output",
            // "readonly",
            // "cross",
            // "foreign",
            // "clustered",
            // "left",
            // "percent",
            // "values",
            // "distinct",
            // "pivot",


        };

        /// <summary>
        /// Mapped to SqlTokenType.IdentifierStandardStatement.
        /// </summary>
        static string[] _keyWordStartStatement = new string[] 
        {
            // Explicitly mapped.
            // "select",
            // "begin",
            // "end",
            // "create",
            // "drop",
            // "alter",
            // "declare",
            // "break",
            // "continue",
            // "goto",
            // "while",
            // "if",
            // "deallocate",
            // "close",
            // "fetch",
            // "open",
            // "return",
            // "throw",

            "raiserror",
            "waitfor",
            "use",
            "truncate",
            "print",
            "commit",
            "rollback",
            "delete",
            "update",
            "updatetext",
            "insert",
            "merge",
            "kill",
            "set",
            "readtext",
            "writetext",
            "dbcc",

        };
        #endregion

        static Dictionary<string,SqlTokenType> _keywords;

        static SqlDbType[] _sqlDbTypesMapped = new SqlDbType[]
            {
                SqlDbType.Xml,
                SqlDbType.DateTimeOffset,
                SqlDbType.DateTime2,
                SqlDbType.DateTime,
                SqlDbType.SmallDateTime,
                SqlDbType.Date,
                SqlDbType.Time,
                SqlDbType.Float,
                SqlDbType.Real,
                SqlDbType.Decimal,
                SqlDbType.Money,
                SqlDbType.SmallMoney,
                SqlDbType.BigInt,
                SqlDbType.Int,
                SqlDbType.SmallInt,
                SqlDbType.TinyInt,
                SqlDbType.Bit,
                SqlDbType.NText,
                SqlDbType.Text,
                SqlDbType.Image,
                SqlDbType.Timestamp,
                SqlDbType.UniqueIdentifier,
                SqlDbType.NVarChar,
                SqlDbType.NChar,
                SqlDbType.VarChar,
                SqlDbType.Char,
                SqlDbType.VarBinary,
                SqlDbType.Binary,  
                SqlDbType.Variant,
            };

        static SqlKeyword()
        {
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeXml                & SqlTokenType.IdentifierValueMask)] == SqlDbType.Xml );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTimeOffset     & SqlTokenType.IdentifierValueMask)] == SqlDbType.DateTimeOffset );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTime2          & SqlTokenType.IdentifierValueMask)] == SqlDbType.DateTime2 );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTime           & SqlTokenType.IdentifierValueMask)] == SqlDbType.DateTime );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallDateTime      & SqlTokenType.IdentifierValueMask)] == SqlDbType.SmallDateTime );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDate               & SqlTokenType.IdentifierValueMask)] == SqlDbType.Date );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTime               & SqlTokenType.IdentifierValueMask)] == SqlDbType.Time );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeFloat              & SqlTokenType.IdentifierValueMask)] == SqlDbType.Float );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeReal               & SqlTokenType.IdentifierValueMask)] == SqlDbType.Real );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDecimal            & SqlTokenType.IdentifierValueMask)] == SqlDbType.Decimal );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeMoney              & SqlTokenType.IdentifierValueMask)] == SqlDbType.Money );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallMoney         & SqlTokenType.IdentifierValueMask)] == SqlDbType.SmallMoney );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBigInt             & SqlTokenType.IdentifierValueMask)] == SqlDbType.BigInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeInt                & SqlTokenType.IdentifierValueMask)] == SqlDbType.Int );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallInt           & SqlTokenType.IdentifierValueMask)] == SqlDbType.SmallInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTinyInt            & SqlTokenType.IdentifierValueMask)] == SqlDbType.TinyInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBit                & SqlTokenType.IdentifierValueMask)] == SqlDbType.Bit );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNText              & SqlTokenType.IdentifierValueMask)] == SqlDbType.NText );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeText               & SqlTokenType.IdentifierValueMask)] == SqlDbType.Text );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeImage              & SqlTokenType.IdentifierValueMask)] == SqlDbType.Image );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTimestamp          & SqlTokenType.IdentifierValueMask)] == SqlDbType.Timestamp );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeUniqueIdentifier   & SqlTokenType.IdentifierValueMask)] == SqlDbType.UniqueIdentifier );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNVarChar           & SqlTokenType.IdentifierValueMask)] == SqlDbType.NVarChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNChar              & SqlTokenType.IdentifierValueMask)] == SqlDbType.NChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVarChar            & SqlTokenType.IdentifierValueMask)] == SqlDbType.VarChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeChar               & SqlTokenType.IdentifierValueMask)] == SqlDbType.Char );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVarBinary          & SqlTokenType.IdentifierValueMask)] == SqlDbType.VarBinary );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBinary             & SqlTokenType.IdentifierValueMask)] == SqlDbType.Binary );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVariant            & SqlTokenType.IdentifierValueMask)] == SqlDbType.Variant );

            _keywords = new Dictionary<string, SqlTokenType>( StringComparer.InvariantCultureIgnoreCase );

            // Identifiers mapped to SqlTokenType.
            
            // SqlDbType mapping.
            _keywords.Add( "sql_variant", SqlTokenType.IdentifierTypeVariant );
            _keywords.Add( "xml", SqlTokenType.IdentifierTypeXml );
            _keywords.Add( "datetimeoffset", SqlTokenType.IdentifierTypeDateTimeOffset );
            _keywords.Add( "datetime2", SqlTokenType.IdentifierTypeDateTime2 );
            _keywords.Add( "datetime", SqlTokenType.IdentifierTypeDateTime );
            _keywords.Add( "smalldatetime", SqlTokenType.IdentifierTypeSmallDateTime );
            _keywords.Add( "date", SqlTokenType.IdentifierTypeDate );
            _keywords.Add( "time", SqlTokenType.IdentifierTypeTime );
            _keywords.Add( "float", SqlTokenType.IdentifierTypeFloat );
            _keywords.Add( "real", SqlTokenType.IdentifierTypeReal );
            _keywords.Add( "decimal", SqlTokenType.IdentifierTypeDecimal );
            _keywords.Add( "numeric", SqlTokenType.IdentifierTypeDecimal );
            _keywords.Add( "money", SqlTokenType.IdentifierTypeMoney );
            _keywords.Add( "smallmoney", SqlTokenType.IdentifierTypeSmallMoney );
            _keywords.Add( "bigint", SqlTokenType.IdentifierTypeBigInt );
            _keywords.Add( "int", SqlTokenType.IdentifierTypeInt );
            _keywords.Add( "smallint", SqlTokenType.IdentifierTypeSmallInt );
            _keywords.Add( "tinyint", SqlTokenType.IdentifierTypeTinyInt );
            _keywords.Add( "bit", SqlTokenType.IdentifierTypeBit );
            _keywords.Add( "ntext", SqlTokenType.IdentifierTypeNText );
            _keywords.Add( "text", SqlTokenType.IdentifierTypeText );
            _keywords.Add( "image", SqlTokenType.IdentifierTypeImage );
            _keywords.Add( "timestamp", SqlTokenType.IdentifierTypeTimestamp );
            _keywords.Add( "uniqueidentifier", SqlTokenType.IdentifierTypeUniqueIdentifier );
            _keywords.Add( "nvarchar", SqlTokenType.IdentifierTypeNVarChar );
            _keywords.Add( "nchar", SqlTokenType.IdentifierTypeNChar );
            _keywords.Add( "varchar", SqlTokenType.IdentifierTypeVarChar );
            _keywords.Add( "char", SqlTokenType.IdentifierTypeChar );
            _keywords.Add( "varbinary", SqlTokenType.IdentifierTypeVarBinary );
            _keywords.Add( "binary", SqlTokenType.IdentifierTypeBinary );

            // SqlTokenType.IdentifierStandardStatement values: these are not reserved keywords but they can start a statement.
            _keywords.Add( "throw", SqlTokenType.Throw );
            _keywords.Add( "get", SqlTokenType.Get );
            _keywords.Add( "move", SqlTokenType.Move );
            _keywords.Add( "receive", SqlTokenType.Receive );
            _keywords.Add( "send", SqlTokenType.Send );

            // SqlTokenType.IdentifierStandard values: these are not reserved keywords.
            _keywords.Add( "try", SqlTokenType.Try );
            _keywords.Add( "catch", SqlTokenType.Catch );
            _keywords.Add( "dialog", SqlTokenType.Dialog );
            _keywords.Add( "conversation", SqlTokenType.Conversation );
            _keywords.Add( "returns", SqlTokenType.Returns );
            _keywords.Add( "max", SqlTokenType.Max );
            _keywords.Add( "readonly", SqlTokenType.Readonly );
            _keywords.Add( "out", SqlTokenType.Output );
            _keywords.Add( "output", SqlTokenType.Output );
            _keywords.Add( "row", SqlTokenType.Rows );
            _keywords.Add( "rows", SqlTokenType.Rows );
            _keywords.Add( "offset", SqlTokenType.Offset );
            _keywords.Add( "first", SqlTokenType.First );
            _keywords.Add( "next", SqlTokenType.Next );
            _keywords.Add( "only", SqlTokenType.Only );
            _keywords.Add( "cast", SqlTokenType.Cast );

            // LogicalOperator (they are reserved keywords).
            _keywords.Add( "or", SqlTokenType.Or );
            _keywords.Add( "and", SqlTokenType.And );
            _keywords.Add( "not", SqlTokenType.Not );
            // CompareOperator (they are reserved keywords).
            _keywords.Add( "between", SqlTokenType.Between );
            _keywords.Add( "in", SqlTokenType.In );
            _keywords.Add( "is", SqlTokenType.Is );
            _keywords.Add( "like", SqlTokenType.Like );
            // Select operators (they are reserved keywords).
            _keywords.Add( "union", SqlTokenType.Union );
            _keywords.Add( "intersect", SqlTokenType.Intersect );
            _keywords.Add( "except", SqlTokenType.Except );
            _keywords.Add( "order", SqlTokenType.Order );
            _keywords.Add( "for", SqlTokenType.For );

            // SqlTokenType.IdentifierReserved values.
            _keywords.Add( "case", SqlTokenType.Case );
            _keywords.Add( "null", SqlTokenType.Null );
            _keywords.Add( "when", SqlTokenType.When );
            _keywords.Add( "by", SqlTokenType.By );
            _keywords.Add( "all", SqlTokenType.All );
            _keywords.Add( "then", SqlTokenType.Then );
            _keywords.Add( "else", SqlTokenType.Else );
            _keywords.Add( "tran", SqlTokenType.Transaction );
            _keywords.Add( "transaction", SqlTokenType.Transaction );
            _keywords.Add( "with", SqlTokenType.With );
            _keywords.Add( "proc", SqlTokenType.Procedure );
            _keywords.Add( "procedure", SqlTokenType.Procedure );
            _keywords.Add( "function", SqlTokenType.Function );
            _keywords.Add( "view", SqlTokenType.View );
            _keywords.Add( "table", SqlTokenType.Table );
            _keywords.Add( "trigger", SqlTokenType.Trigger );
            _keywords.Add( "as", SqlTokenType.As );
            _keywords.Add( "asc", SqlTokenType.Asc );
            _keywords.Add( "desc", SqlTokenType.Desc );
            _keywords.Add( "exists", SqlTokenType.Exists );
            _keywords.Add( "on", SqlTokenType.On );
            _keywords.Add( "to", SqlTokenType.To );
            _keywords.Add( "of", SqlTokenType.Of );
            _keywords.Add( "top", SqlTokenType.Top );
            _keywords.Add( "escape", SqlTokenType.Escape );
            _keywords.Add( "into",  SqlTokenType.Into );
            _keywords.Add( "from",  SqlTokenType.From );
            _keywords.Add( "where", SqlTokenType.Where );
            _keywords.Add( "group", SqlTokenType.Group );
            _keywords.Add( "option", SqlTokenType.Option );
            _keywords.Add( "add", SqlTokenType.Add );
            _keywords.Add( "database", SqlTokenType.Database );
            _keywords.Add( "external", SqlTokenType.External );
            _keywords.Add( "over", SqlTokenType.Over );
            _keywords.Add( "cross", SqlTokenType.Cross );
            _keywords.Add( "foreign", SqlTokenType.Foreign );
            _keywords.Add( "clustered", SqlTokenType.Clustered );
            _keywords.Add( "left", SqlTokenType.Left );
            _keywords.Add( "percent", SqlTokenType.Percent );
            _keywords.Add( "values", SqlTokenType.Values );
            _keywords.Add( "distinct", SqlTokenType.Distinct );
            _keywords.Add( "pivot", SqlTokenType.Pivot );
            _keywords.Add( "having", SqlTokenType.Having );
            _keywords.Add( "collate", SqlTokenType.Collate );
                        

            // SqlTokenType.IdentifierReservedStart values.
            _keywords.Add( "select", SqlTokenType.Select );
            _keywords.Add( "begin", SqlTokenType.Begin );
            _keywords.Add( "end", SqlTokenType.End );
            _keywords.Add( "create", SqlTokenType.Create );
            _keywords.Add( "drop", SqlTokenType.Drop );
            _keywords.Add( "alter", SqlTokenType.Alter );
            _keywords.Add( "declare", SqlTokenType.Declare );
            _keywords.Add( "break", SqlTokenType.Break );
            _keywords.Add( "continue", SqlTokenType.Continue );
            _keywords.Add( "goto", SqlTokenType.Goto );
            _keywords.Add( "while", SqlTokenType.While );
            _keywords.Add( "if", SqlTokenType.If );
            _keywords.Add( "deallocate", SqlTokenType.Deallocate );
            _keywords.Add( "close", SqlTokenType.Close );
            _keywords.Add( "fetch", SqlTokenType.Fetch );
            _keywords.Add( "open", SqlTokenType.Open );
            _keywords.Add( "return", SqlTokenType.Return );
            _keywords.Add( "exec", SqlTokenType.Execute );
            _keywords.Add( "execute", SqlTokenType.Execute );

            // Reserved keywords.
            foreach( string s in _sqlServerReserved )
            {
                #if DEBUG
                if( _keywords.ContainsKey( s ) ) Debugger.Break();
                #endif
                _keywords.Add( s, SqlTokenType.IdentifierReserved );
            }
            // Reserved keywords.
            foreach( string s in _keyWordStartStatement )
            {
                #if DEBUG
                if( _keywords.ContainsKey( s ) ) Debugger.Break();
                #endif
                _keywords.Add( s, SqlTokenType.IdentifierReservedStatement );
            }

        }

        public static SqlDbType? FromSqlTokenTypeToSqlDbType( SqlTokenType t )
        {
            if( t < 0 || (t & SqlTokenType.IsIdentifier) == 0 || (t & SqlTokenType.IdentifierTypeMask) != SqlTokenType.IdentifierDbType ) return null;
            int iT = (int)(t & SqlTokenType.IdentifierValueMask);
            return _sqlDbTypesMapped[iT];
        }

        public static bool IsReservedKeyword( string s )
        {
            SqlTokenType tokenType;
            return IsReservedKeyword( s, out tokenType );
        }

        public static bool IsReservedKeyword( string s, out SqlTokenType tokenType )
        {
            if( !_keywords.TryGetValue( s, out tokenType ) ) return false;
            // DbType are the only keywords we map that are not reserved.
            if( (tokenType & SqlTokenType.IdentifierDbType) == SqlTokenType.IdentifierDbType ) return false;
            return true;
        }

        public static SqlTokenType MapKeyword( string s )
        {
            SqlTokenType mapped;
            _keywords.TryGetValue( s, out mapped );
            return mapped;
        }
    }
}
