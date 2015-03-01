#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlNoExprList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Non-enclosable list of comma separated <see cref="SqlItem"/>.
    /// </summary>
    public abstract class SqlNoExprList<T> : SqlNoExpr, IReadOnlyList<T> where T : SqlItem
    {
        public SqlNoExprList( IList<ISqlItem> components )
            : this( components.ToArray() )
        {
            SqlExprBaseListWithSeparator<T>.CheckArray( Slots, true, false, false, SqlToken.IsCommaSeparator );
        }

        internal SqlNoExprList( ISqlItem[] items )
            : base( items )
        {
        }

        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        public int SeparatorCount { get { return Slots.Length / 2; } }

        /// <summary>
        /// Gets the separators token.
        /// </summary>
        public IEnumerable<SqlTokenTerminal> SeparatorTokens { get { return Slots.Where( ( x, i ) => i % 2 != 0 ).Cast<SqlTokenTerminal>(); } }

        public T this[int index]
        {
            get { return (T)Slots[index * 2]; }
        }

        /// <summary>
        /// Gets the number of items in the list.
        /// </summary>
        public int Count
        {
            get { return (Slots.Length + 1) / 2; }
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return Slots.Where( ( x, i ) => i % 2 == 0 ).Cast<T>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }


}
