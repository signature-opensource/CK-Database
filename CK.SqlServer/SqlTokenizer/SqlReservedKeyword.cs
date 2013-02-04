using System;
using System.Collections.Generic;
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

        static SqlReservedKeyword()
        {
            _keywords = new Dictionary<string, object>( StringComparer.InvariantCultureIgnoreCase );

            // Sql Server Types.
            _keywords.Add( "sql_variant", "sql_variant" );
            _keywords.Add( "xml", "xml" );
            _keywords.Add( "datetimeoffset", "datetimeoffset" );
            _keywords.Add( "datetime2", "datetime2" );
            _keywords.Add( "datetime", "datetime" );
            _keywords.Add( "smalldatetime", "smalldatetime" );
            _keywords.Add( "date", "date" );
            _keywords.Add( "time", "time" );
            _keywords.Add( "float", "float" );
            _keywords.Add( "real", "real" );
            _keywords.Add( "decimal", "decimal" );
            _keywords.Add( "money", "money" );
            _keywords.Add( "smallmoney", "smallmoney" );
            _keywords.Add( "bigint", "bigint" );
            _keywords.Add( "int", "int" );
            _keywords.Add( "smallint", "smallint" );
            _keywords.Add( "tinyint", "tinyint" );
            _keywords.Add( "bit", "bit" );
            _keywords.Add( "ntext", "ntext" );
            _keywords.Add( "text", "text" );
            _keywords.Add( "image", "image" );
            _keywords.Add( "timestamp", "timestamp" );
            _keywords.Add( "uniqueidentifier", "uniqueidentifier" );
            _keywords.Add( "nvarchar", "nvarchar" );
            _keywords.Add( "nchar", "nchar" );
            _keywords.Add( "varchar", "varchar" );
            _keywords.Add( "char", "char" );
            _keywords.Add( "varbinary", "varbinary" );
            _keywords.Add( "binary", "binary" );

            // Logical or Set operators.
            _keywords.Add( "or", SqlToken.Or );
            _keywords.Add( "and", SqlToken.And );
            _keywords.Add( "not", SqlToken.Not );
            _keywords.Add( "all", SqlToken.All );
            _keywords.Add( "any", SqlToken.Any );
            _keywords.Add( "some", SqlToken.Any );
            _keywords.Add( "between", SqlToken.Between );
            _keywords.Add( "exists", SqlToken.Exists );
            _keywords.Add( "in", SqlToken.In );
            _keywords.Add( "like", SqlToken.Like );

            foreach( string s in _sqlServerReserved )
            {
                _keywords.Add( s, s );
            }
        }

        public static object MapKeyword( string s )
        {
            object mapped;
            _keywords.TryGetValue( s, out mapped );
            return mapped;
        }
    }
}
