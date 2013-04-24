using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public static class SqlReservedKeyword
    {
        #region Arrays of keywords
        
        static string[] _sqlServerReserved = new string[] 
        {
            "add",
            "all",
            "any",
            "some",
            "exists",
            "precision",
            "exit",
            "primary",
            "alter",
            "external",
            "print",
            "fetch",
            "proc",
            "file",
            "procedure",
            "as",
            "fillfactor",
            "public",
            "asc",
            "raiserror",
            "authorization",
            "foreign",
            "read",
            "backup",
            "freetext",
            "readtext",
            "begin",
            "freetexttable",
            "reconfigure",
            "references",
            "break",
            "full",
            "replication",
            "browse",
            "function",
            "restore",
            "bulk",
            "goto",
            "restrict",
            "by",
            "grant",
            "return",
            "cascade",
            "revert",
            "case",
            "having",
            "revoke",
            "check",
            "holdlock",
            "right",
            "checkpoint",
            "identity",
            "rollback",
            "close",
            "identity_insert",
            "rowcount",
            "clustered",
            "identitycol",
            "rowguidcol",
            "coalesce",
            "if",
            "rule",
            "collate",
            "save",
            "column",
            "index",
            "schema",
            "commit",
            "inner",
            "securityaudit",
            "compute",
            "insert",
            "select",
            "constraint",
            "session_user",
            "contains",
            "set",
            "containstable",
            "setuser",
            "continue",
            "join",
            "shutdown",
            "convert",
            "key",
            "create",
            "kill",
            "statistics",
            "cross",
            "left",
            "system_user",
            "current",
            "table",
            "current_date",
            "lineno",
            "tablesample",
            "current_time",
            "load",
            "textsize",
            "current_timestamp",
            "merge",
            "then",
            "current_user",
            "national",
            "to",
            "cursor",
            "nocheck",
            "top",
            "database",
            "nonclustered",
            "tran",
            "dbcc",
            "transaction",
            "deallocate",
            "null",
            "trigger",
            "declare",
            "nullif",
            "truncate",
            "default",
            "of",
            "tsequal",
            "delete",
            "off",
            "deny",
            "offsets",
            "unique",
            "desc",
            "on",
            "unpivot",
            "disk",
            "open",
            "update",
            "distinct",
            "opendatasource",
            "updatetext",
            "distributed",
            "openquery",
            "use",
            "double",
            "openrowset",
            "user",
            "drop",
            "openxml",
            "values",
            "dump",
            "varying",
            "else",
            "view",
            "end",
            "waitfor",
            "errlvl",
            "outer",
            "when",
            "escape",
            "over",
            "percent",
            "while",
            "exec",
            "pivot",
            "with",
            "execute",
            "plan",
            "writetext",

            "into", 
            "from", 
            "where",
            "group",
            "option"
        };
        #endregion

        static Dictionary<string,object> _keywords;
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

        static SqlReservedKeyword()
        {
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeXml                & SqlTokenType.IdentifierMask)-4] == SqlDbType.Xml );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTimeOffset     & SqlTokenType.IdentifierMask)-4] == SqlDbType.DateTimeOffset );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTime2          & SqlTokenType.IdentifierMask)-4] == SqlDbType.DateTime2 );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTime           & SqlTokenType.IdentifierMask)-4] == SqlDbType.DateTime );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallDateTime      & SqlTokenType.IdentifierMask)-4] == SqlDbType.SmallDateTime );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDate               & SqlTokenType.IdentifierMask)-4] == SqlDbType.Date );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTime               & SqlTokenType.IdentifierMask)-4] == SqlDbType.Time );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeFloat              & SqlTokenType.IdentifierMask)-4] == SqlDbType.Float );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeReal               & SqlTokenType.IdentifierMask)-4] == SqlDbType.Real );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDecimal            & SqlTokenType.IdentifierMask)-4] == SqlDbType.Decimal );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeMoney              & SqlTokenType.IdentifierMask)-4] == SqlDbType.Money );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallMoney         & SqlTokenType.IdentifierMask)-4] == SqlDbType.SmallMoney );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBigInt             & SqlTokenType.IdentifierMask)-4] == SqlDbType.BigInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeInt                & SqlTokenType.IdentifierMask)-4] == SqlDbType.Int );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallInt           & SqlTokenType.IdentifierMask)-4] == SqlDbType.SmallInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTinyInt            & SqlTokenType.IdentifierMask)-4] == SqlDbType.TinyInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBit                & SqlTokenType.IdentifierMask)-4] == SqlDbType.Bit );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNText              & SqlTokenType.IdentifierMask)-4] == SqlDbType.NText );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeText               & SqlTokenType.IdentifierMask)-4] == SqlDbType.Text );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeImage              & SqlTokenType.IdentifierMask)-4] == SqlDbType.Image );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTimestamp          & SqlTokenType.IdentifierMask)-4] == SqlDbType.Timestamp );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeUniqueIdentifier   & SqlTokenType.IdentifierMask)-4] == SqlDbType.UniqueIdentifier );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNVarChar           & SqlTokenType.IdentifierMask)-4] == SqlDbType.NVarChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNChar              & SqlTokenType.IdentifierMask)-4] == SqlDbType.NChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVarChar            & SqlTokenType.IdentifierMask)-4] == SqlDbType.VarChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeChar               & SqlTokenType.IdentifierMask)-4] == SqlDbType.Char );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVarBinary          & SqlTokenType.IdentifierMask)-4] == SqlDbType.VarBinary );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBinary             & SqlTokenType.IdentifierMask)-4] == SqlDbType.Binary );  
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVariant            & SqlTokenType.IdentifierMask)-4] == SqlDbType.Variant );

            _keywords = new Dictionary<string, object>( StringComparer.InvariantCultureIgnoreCase );

            // Identifiers mapped to SqlTokenType.
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

            _keywords.Add( "or", SqlTokenType.Or );
            _keywords.Add( "and", SqlTokenType.And );
            _keywords.Add( "not", SqlTokenType.Not );
            _keywords.Add( "between", SqlTokenType.Between );
            _keywords.Add( "in", SqlTokenType.In );
            _keywords.Add( "is", SqlTokenType.Is );
            _keywords.Add( "like", SqlTokenType.Like );
            _keywords.Add( "union", SqlTokenType.Union );
            _keywords.Add( "intersect", SqlTokenType.Intersect );
            _keywords.Add( "except", SqlTokenType.Except );
            _keywords.Add( "order", SqlTokenType.Order );
            _keywords.Add( "for", SqlTokenType.For );

            Debug.Assert( _keywords.Keys.Intersect( _sqlServerReserved ).Any() == false ); 
            // Reserved keywords.
            foreach( string s in _sqlServerReserved )
            {
                #if DEBUG
                if( _keywords.ContainsKey( s ) ) Debugger.Break();
                #endif
                _keywords.Add( s, s );
            }
        }

        public static SqlDbType? FromSqlTokenTypeToSqlDbType( SqlTokenType t )
        {
            Debug.Assert( (int)(SqlTokenType.IdentifierReservedKeyword&SqlTokenType.IdentifierMask) == 1 );
            Debug.Assert( (int)(SqlTokenType.IdentifierVariable & SqlTokenType.IdentifierMask) == 2 );
            Debug.Assert( (int)(SqlTokenType.IdentifierStar & SqlTokenType.IdentifierMask) == 3 );
            Debug.Assert( (int)(SqlTokenType.IdentifierTypeXml & SqlTokenType.IdentifierMask) == 4, "First real type." );
            Debug.Assert( (int)(SqlTokenType.IdentifierTypeVariant&SqlTokenType.IdentifierMask) == 32, "Last real type." );

            if( (t&SqlTokenType.IsIdentifier) == 0 ) return null;
            int iT = (int)(t & SqlTokenType.IdentifierMask) - 4;
            if( iT < 0 || iT > 32-4 ) return null;
            return _sqlDbTypesMapped[iT];
        }

        public static object MapKeyword( string s )
        {
            object mapped;
            _keywords.TryGetValue( s, out mapped );
            return mapped;
        }
    }
}
