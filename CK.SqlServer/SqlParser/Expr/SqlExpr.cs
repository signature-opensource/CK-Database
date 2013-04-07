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

        internal virtual SqlExpr LiftedExpression 
        { 
            get { return this; } 
        }

        internal static IAbstractExpr[] ApplyLift( IAbstractExpr[] t )
        {
            IAbstractExpr[] modified = null;
            int len = t.Length;
            for( int i = 0; i < len; i++ )
            {
                SqlExpr o = t[i] as SqlExpr;
                if( o != null )
                {
                    SqlExpr r = o.LiftedExpression;
                    if( !ReferenceEquals( r, o ) )
                    {
                        if( modified == null ) modified = (IAbstractExpr[])t.Clone();
                        modified[i] = r;
                    }
                }
            }
            return modified;
        }

    }

}
