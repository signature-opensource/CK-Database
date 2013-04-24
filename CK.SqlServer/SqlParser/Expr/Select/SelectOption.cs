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
    /// Captures the optional "Option ( ... )" select part.
    /// </summary>
    public class SelectOption : SqlNoExpr
    {
        public SelectOption( SqlTokenIdentifier optionToken, SqlExpr content )
            : this( CreateArray( optionToken, content ) )
        {
        }

        internal SelectOption( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExpr Content { get { return (SqlExpr)Slots[1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
