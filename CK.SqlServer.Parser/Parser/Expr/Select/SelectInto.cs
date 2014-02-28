using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Captures the optional "INTO table".
    /// </summary>
    public class SelectInto : SqlNoExpr
    {

        public SelectInto( SqlTokenIdentifier intoToken, SqlExprMultiIdentifier tableName )
            : this( CreateArray( intoToken, tableName ) )
        {
        }

        internal SelectInto( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExprMultiIdentifier TableName { get { return (SqlExprMultiIdentifier)Slots[1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
