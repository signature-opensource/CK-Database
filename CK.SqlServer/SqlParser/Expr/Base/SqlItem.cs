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
    public abstract class SqlItem : ISqlItem
    {
        /// <summary>
        /// Gets the components of this expression: it is a mix of <see cref="SqlToken"/> and <see cref="SqlExpr"/>.
        /// Never null but can be empty.
        /// </summary>
        public abstract IEnumerable<ISqlItem> Components { get; }

        /// <summary>
        /// Gets the last token of the expression.
        /// </summary>
        public abstract SqlToken LastOrEmptyToken { get; }

        /// <summary>
        /// Gets the first token of the expression.
        /// </summary>
        public abstract SqlToken FirstOrEmptyToken { get; }

        /// <summary>
        /// Gets the tokens that compose this expression.
        /// </summary>
        public virtual IEnumerable<SqlToken> Tokens  { get { return Flatten( Components ); } }

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
        public void Write( StringBuilder b )
        {
            foreach( var t in Tokens ) t.Write( b );
        }


        internal protected abstract T Accept<T>( IExprVisitor<T> visitor );

        static internal ISqlItem[] CreateArray( params ISqlItem[] e )
        {
            Debug.Assert( e != null && e.All( i => i != null ) );
            return e;
        }

        static internal SqlToken[] CreateArray( params SqlToken[] e )
        {
            Debug.Assert( e != null && e.All( i => i != null ) );
            return e;
        }

        static internal SqlTokenIdentifier[] CreateArray( params SqlTokenIdentifier[] e )
        {
            Debug.Assert( e != null && e.All( i => i != null ) );
            return e;
        }

        static internal ISqlItem[] CreateArray( IEnumerable<ISqlItem> content, int contentLength, ISqlItem suffix )
        {
            Debug.Assert( content != null && suffix != null && contentLength <= content.Count() && contentLength >= 0 );
            var c = new ISqlItem[contentLength + 1];
            int i = 0;
            foreach( var e in content )
            {
                c[i++] = e;
                if( i == contentLength ) break;
            }
            c[contentLength] = suffix;
            return c;
        }

        static internal ISqlItem[] CreateArray( ISqlItem prefix, IEnumerable<ISqlItem> content, int skippedContent, int contentLength, ISqlItem suffix )
        {
            Debug.Assert( content != null && suffix != null && prefix != null 
                            && skippedContent >= 0 && contentLength >= 0 && skippedContent + contentLength <= content.Count() );
            var c = new ISqlItem[++contentLength + 1];
            c[0] = prefix;
            int i = 0;
            foreach( var e in content.Skip( skippedContent ) )
            {
                if( i == contentLength ) break;
                c[++i] = e;
            }
            c[contentLength] = suffix;
            return c;
        }

        static internal ISqlItem[] CreateArray( SqlTokenOpenPar openPar, IEnumerable<ISqlItem> content, int contentLength, SqlTokenClosePar closePar )
        {
            Debug.Assert( contentLength == 0 || !(content.First() is SqlExprMultiToken<SqlTokenOpenPar>) );
            return CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Create( openPar ), content, 0, contentLength, SqlExprMultiToken<SqlTokenClosePar>.Create( closePar ) );
        }

        static internal ISqlItem[] CreateParArray( IReadOnlyList<ISqlItem> content )
        {
            Debug.Assert( content.Count == 0 || !(content.First() is SqlExprMultiToken<SqlTokenOpenPar>) );
            return CreateArray( SqlToken.EmptyOpenPar, content, 0, content.Count, SqlToken.EmptyClosePar );
        }

        static internal ISqlItem[] CreateArray( SqlTokenOpenPar prefix, IReadOnlyList<ISqlItem> enclosedComponents, SqlTokenClosePar suffix )
        {
            Debug.Assert( prefix != null && enclosedComponents != null && suffix != null );
            Debug.Assert( enclosedComponents.Count >= 2 );
            Debug.Assert( enclosedComponents[0] is SqlExprMultiToken<SqlTokenOpenPar> );
            Debug.Assert( enclosedComponents[enclosedComponents.Count-1] is SqlExprMultiToken<SqlTokenClosePar> );

            SqlExprMultiToken<SqlTokenOpenPar> existOpen = (SqlExprMultiToken<SqlTokenOpenPar>)enclosedComponents[0];
            SqlExprMultiToken<SqlTokenClosePar> existClose = (SqlExprMultiToken<SqlTokenClosePar>)enclosedComponents[enclosedComponents.Count - 1];

            return CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Create( prefix, existOpen ), enclosedComponents, 1, enclosedComponents.Count - 2, SqlExprMultiToken<SqlTokenClosePar>.Create( existClose, suffix ) );
        }

        static internal IEnumerable<SqlToken> Flatten( IEnumerable<ISqlItem> e )
        {
            foreach( var a in e )
            {
                SqlToken t = a as SqlToken;
                if( t != null ) yield return t;
                else foreach( var ta in Flatten( a.Tokens ) ) yield return ta;
            }
        }


    }

}
