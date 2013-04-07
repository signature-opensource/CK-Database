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
    /// Mono identifier statements are "continue" or "break".
    /// </summary>
    public class SqlExprStMonoStatement : SqlExprBaseSt
    {
        public SqlExprStMonoStatement( SqlTokenIdentifier id, SqlTokenTerminal statementTerminator = null )
            : base( CreateArray( id ), statementTerminator )
        {
            if( id == null ) throw new ArgumentNullException( "id" );
        }

        public SqlTokenIdentifier Identifier { get { return (SqlTokenIdentifier)At(0); } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
