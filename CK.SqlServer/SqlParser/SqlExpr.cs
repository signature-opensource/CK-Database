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
    public abstract class SqlExpr
    {
        protected SqlExpr( SourceLocation location )
        {
            Location = location;
        }

        public readonly SourceLocation Location;

        internal protected abstract T Accept<T>( IExprVisitor<T> visitor );
    }

}
