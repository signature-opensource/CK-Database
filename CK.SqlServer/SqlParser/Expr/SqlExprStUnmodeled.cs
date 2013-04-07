using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Captures any statement. It is an identifier followed by any number of comma separated list of mere <see cref="SqlToken"/>s or <see cref="SqlExpr"/>.
    /// </summary>
    public class SqlExprStUnmodeled : SqlExprBaseSt
    {
        public SqlExprStUnmodeled( SqlTokenIdentifier id, SqlExprList content, SqlTokenTerminal statementTerminator = null )
            : base( CreateArray( id, content ), statementTerminator )
        {
        }
        
        public SqlTokenIdentifier Identifier { get { return (SqlTokenIdentifier)At(0); } }

        public SqlExprList Content { get { return (SqlExprList)At( 1 ); } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
