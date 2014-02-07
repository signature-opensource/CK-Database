using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using CK.Core;
using System.Diagnostics;
using System.Globalization;

namespace CK.SqlServer
{
    public class SqlExprUnmodeledItems : SqlNoExpr
    {
        public SqlExprUnmodeledItems( IEnumerable<ISqlItem> items )
            : base( Build( items ) )
        {
        }

        static ISqlItem[] Build( IEnumerable<ISqlItem> items )
        {
            if( items == null ) throw new ArgumentNullException( "items" );
            ISqlItem[] t = items.ToArray();
            if( t.Length == 0 ) throw new ArgumentException( "items" );
            return t;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
