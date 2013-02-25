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
            "for",
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
            "from",
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
            "group",
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
            "intersect",
            "session_user",
            "contains",
            "into",
            "set",
            "containstable",
            "is",
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
            "union",
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
            "option",
            "varying",
            "else",
            "view",
            "end",
            "order",
            "waitfor",
            "errlvl",
            "outer",
            "when",
            "escape",
            "over",
            "where",
            "except",
            "percent",
            "while",
            "exec",
            "pivot",
            "with",
            "execute",
            "plan",
            "writetext"
        };
        #endregion

        static Dictionary<string,object> _keywords;
        static SqlDbType[] _sqlDbTypesMapped = new SqlDbType[]
            {
                SqlDbType.Variant,
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
                SqlDbType.Binary  
            };

        static SqlReservedKeyword()
        {
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVariant            & SqlTokenType.IdentifierMask)-3] == SqlDbType.Variant );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeXml                & SqlTokenType.IdentifierMask)-3] == SqlDbType.Xml );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTimeOffset     & SqlTokenType.IdentifierMask)-3] == SqlDbType.DateTimeOffset );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTime2          & SqlTokenType.IdentifierMask)-3] == SqlDbType.DateTime2 );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDateTime           & SqlTokenType.IdentifierMask)-3] == SqlDbType.DateTime );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallDateTime      & SqlTokenType.IdentifierMask)-3] == SqlDbType.SmallDateTime );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDate               & SqlTokenType.IdentifierMask)-3] == SqlDbType.Date );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTime               & SqlTokenType.IdentifierMask)-3] == SqlDbType.Time );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeFloat              & SqlTokenType.IdentifierMask)-3] == SqlDbType.Float );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeReal               & SqlTokenType.IdentifierMask)-3] == SqlDbType.Real );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeDecimal            & SqlTokenType.IdentifierMask)-3] == SqlDbType.Decimal );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeMoney              & SqlTokenType.IdentifierMask)-3] == SqlDbType.Money );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallMoney         & SqlTokenType.IdentifierMask)-3] == SqlDbType.SmallMoney );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBigInt             & SqlTokenType.IdentifierMask)-3] == SqlDbType.BigInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeInt                & SqlTokenType.IdentifierMask)-3] == SqlDbType.Int );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeSmallInt           & SqlTokenType.IdentifierMask)-3] == SqlDbType.SmallInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTinyInt            & SqlTokenType.IdentifierMask)-3] == SqlDbType.TinyInt );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBit                & SqlTokenType.IdentifierMask)-3] == SqlDbType.Bit );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNText              & SqlTokenType.IdentifierMask)-3] == SqlDbType.NText );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeText               & SqlTokenType.IdentifierMask)-3] == SqlDbType.Text );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeImage              & SqlTokenType.IdentifierMask)-3] == SqlDbType.Image );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeTimestamp          & SqlTokenType.IdentifierMask)-3] == SqlDbType.Timestamp );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeUniqueIdentifier   & SqlTokenType.IdentifierMask)-3] == SqlDbType.UniqueIdentifier );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNVarChar           & SqlTokenType.IdentifierMask)-3] == SqlDbType.NVarChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeNChar              & SqlTokenType.IdentifierMask)-3] == SqlDbType.NChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVarChar            & SqlTokenType.IdentifierMask)-3] == SqlDbType.VarChar );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeChar               & SqlTokenType.IdentifierMask)-3] == SqlDbType.Char );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeVarBinary          & SqlTokenType.IdentifierMask)-3] == SqlDbType.VarBinary );
            Debug.Assert( _sqlDbTypesMapped[(int)(SqlTokenType.IdentifierTypeBinary             & SqlTokenType.IdentifierMask)-3] == SqlDbType.Binary );  

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
            _keywords.Add( "all", SqlTokenType.All );
            _keywords.Add( "any", SqlTokenType.Any );
            _keywords.Add( "some", SqlTokenType.Any );
            _keywords.Add( "between", SqlTokenType.Between );
            _keywords.Add( "exists", SqlTokenType.Exists );
            _keywords.Add( "in", SqlTokenType.In );
            _keywords.Add( "like", SqlTokenType.Like );

            // Reserved keywords.
            foreach( string s in _sqlServerReserved )
            {
                _keywords.Add( s, s );
            }
        }

        public static SqlDbType? FromSqlTokenTypeToSqlDbType( SqlTokenType t )
        {
            Debug.Assert( (int)(SqlTokenType.IdentifierReservedKeyword&SqlTokenType.IdentifierMask) == 1 );
            Debug.Assert( (int)(SqlTokenType.IdentifierVariable&SqlTokenType.IdentifierMask) == 2 );
            Debug.Assert( (int)(SqlTokenType.IdentifierTypeVariant&SqlTokenType.IdentifierMask) == 3, "First real type." );
            Debug.Assert( (int)(SqlTokenType.IdentifierTypeBinary&SqlTokenType.IdentifierMask) == 31, "Last real type." );

            if( (t&SqlTokenType.IsIdentifier) == 0 ) return null;
            int iT = (int)(t & SqlTokenType.IdentifierMask) - 3;
            if( iT < 0 || iT > 31-3 ) return null;
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
