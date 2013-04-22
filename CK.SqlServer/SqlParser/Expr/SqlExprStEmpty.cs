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
    /// An isolated statement terminator ; is valid.
    /// </summary>
    public class SqlExprStEmpty : SqlExprBaseSt
    {
        static ISqlItem[] _empty = new ISqlItem[0];

        public SqlExprStEmpty( SqlTokenTerminal statementTerminator )
            : base( _empty, statementTerminator )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
