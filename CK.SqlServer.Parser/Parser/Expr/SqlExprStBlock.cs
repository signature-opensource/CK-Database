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
    /// A block is defined by begin...end enclosing keywords.
    /// </summary>
    public class SqlExprStBlock : SqlExprBaseSt
    {
        public SqlExprStBlock( SqlTokenIdentifier begin, SqlExprStatementList body, SqlTokenIdentifier end, SqlTokenTerminal statementTerminator = null )
            : base( CreateArray( begin, body, end ), statementTerminator )
        {
        }

        public SqlTokenIdentifier Begin { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlExprStatementList Body { get { return (SqlExprStatementList)Slots[1]; } }

        public SqlTokenIdentifier End { get { return (SqlTokenIdentifier)Slots[2]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
