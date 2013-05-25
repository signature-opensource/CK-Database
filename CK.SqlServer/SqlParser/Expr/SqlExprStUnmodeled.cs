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
    /// Captures any statement: it can be any <see cref="SqlExpr"/>.
    /// </summary>
    public class SqlExprStUnmodeled : SqlExprBaseSt
    {
        public SqlExprStUnmodeled( SqlExpr content, SqlTokenTerminal statementTerminator = null )
            : base( CreateArray( content ), statementTerminator )
        {
            if( content == null ) throw new ArgumentNullException( "content" );
        }

        public SqlExpr Content { get { return (SqlExpr)Slots[0]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
