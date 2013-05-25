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
    /// Raw list of contiguous <see cref="ISqlItem"/> that can be enclosed in parenthesis.
    /// </summary>
    public sealed class SqlExprRawItemList : SqlExpr
    {
        /// <summary>
        /// Initializes a new raw list without any opener/closer parenthesis.
        /// </summary>
        /// <param name="items">List of any kind of <see cref="ISqlItem"/> that compose this block.</param>
        public SqlExprRawItemList( IList<ISqlItem> items )
            : this( CreateEnclosedArray( items.AsReadOnlyList() ) )
        {
        }

        /// <summary>
        /// Initializes a new raw list that is enclosed in a pair of opener/closer parenthesis.
        /// </summary>
        /// <param name="openPar">Opening parenthesis.</param>
        /// <param name="items">
        /// List of <see cref="ISqlItem"/> that compose this block. 
        /// This MUST not contain the <see cref="Opener"/> and/or the <see cref="Closer"/>.</param>
        /// <param name="closePar">Closing parentehsis.</param>
        public SqlExprRawItemList( SqlTokenOpenPar openPar, IList<ISqlItem> items, SqlTokenClosePar closePar )
            : this( CreateArray( openPar, items.AsReadOnlyList(), items.Count, closePar ) )
        {
        }

        internal SqlExprRawItemList( ISqlItem[] items )
            : base( items )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
