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
    public abstract class SqlExpr : SqlExprAbstract
    {


        internal protected abstract T Accept<T>( IExprVisitor<T> visitor );
    }

}
