using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprTerminal : SqlExprBaseMonoToken
    {
        protected SqlExprTerminal( SqlTokenTerminal t )
            : base( t )
        {
        }

        public new SqlTokenTerminal Token { get { return (SqlTokenTerminal)base.Token; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
