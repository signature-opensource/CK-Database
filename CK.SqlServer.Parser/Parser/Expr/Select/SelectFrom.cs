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
    /// Captures the optional "From ..." select part.
    /// </summary>
    public class SelectFrom : SqlNoExpr
    {
        public SelectFrom( SqlTokenIdentifier fromTok, SqlExpr content )
            : this( CreateArray( fromTok, content ) )
        {
        }

        internal SelectFrom( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier FromTok { get { return (SqlTokenIdentifier)Slots[0]; } }
        
        public SqlExpr Content { get { return (SqlExpr)Slots[1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
