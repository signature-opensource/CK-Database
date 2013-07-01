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
    /// List of comma separated <see cref="SelectColumn"/>
    /// </summary>
    public class SelectColumnList : SqlNoExpr, IReadOnlyList<SelectColumn>
    {
        public SelectColumnList( IList<ISqlItem> components )
            : this( components.ToArray() )
        {
            SqlExprBaseListWithSeparator<SelectColumn>.CheckArray( Slots, true, false, false, SqlToken.IsCommaSeparator );
        }

        internal SelectColumnList( ISqlItem[] items )
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

        public SelectColumn this[int index]
        {
            get { return (SelectColumn)Slots[index * 2]; }
        }

        /// <summary>
        /// Gets the number of <see cref="SelectColumn"/>.
        /// </summary>
        public int Count
        {
            get { return (Slots.Length + 1) / 2; }
        }

        /// <summary>
        /// Gets the identifiers.
        /// </summary>
        public IEnumerator<SelectColumn> GetEnumerator()
        {
            return Slots.Where( ( x, i ) => i % 2 == 0 ).Cast<SelectColumn>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
