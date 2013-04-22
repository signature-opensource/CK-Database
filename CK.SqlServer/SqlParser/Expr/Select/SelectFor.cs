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
    /// Captures the optional "For ..." select part.
    /// </summary>
    public class SelectFor : SqlNoExpr
    {
        public SelectFor( SqlTokenIdentifier forToken, SqlExpr content )
            : this( CreateArray( forToken, content ) )
        {
        }

        internal SelectFor( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExpr Expression { get { return (SqlExpr)Slots[2]; } }


        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
