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
    public abstract class SqlExpr : IAbstractExpr
    {
        /// <summary>
        /// Gets the tokens that compose this expression.
        /// Never null nor empty: an expression covers at least one token.
        /// </summary>
        public abstract IEnumerable<SqlToken> Tokens { get; }

        /// <summary>
        /// Overriden to generate the representation of an expression as the result of the <see cref="Write"/> method.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            Write( b );
            return b.ToString();
        }

        /// <summary>
        /// Writing an expression is, by default, calling <see cref="SqlToken.Write"/> on each of its <see cref="Tokens"/>.
        /// </summary>
        /// <param name="b">StringBuilder to write into.</param>
        public virtual void Write( StringBuilder b )
        {
            foreach( var t in Tokens ) t.Write( b );
        }

        internal protected abstract T Accept<T>( IExprVisitor<T> visitor );


        static internal IEnumerable<SqlToken> Flatten( IEnumerable<IAbstractExpr> e )
        {
            foreach( var a in e )
            {
                SqlToken t = a as SqlToken;
                if( t != null ) yield return t;
                foreach( var ta in Flatten( a.Tokens ) ) yield return ta;
            }
        }
    }

}
