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
    /// List of comma separated <see cref="SqlExprSelectColumn"/>
    /// </summary>
    public class SqlExprSelectColumnList : SqlExpr, IReadOnlyList<SqlExprSelectColumn>
    {
        readonly IAbstractExpr[] _components;
        
        public SqlExprSelectColumnList( IList<IAbstractExpr> components )
        {
            _components = components.ToArray();
            SqlExprBaseListWithSeparator<SqlTokenIdentifier>.CheckArray( _components, false, false, false, IsCommaSeparator );
        }
        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        public int SeparatorCount { get { return _components.Length / 2; } }

        /// <summary>
        /// Gets the separators token.
        /// </summary>
        public IEnumerable<SqlTokenTerminal> SeparatorTokens { get { return _components.Where( ( x, i ) => i % 2 != 0 ).Cast<SqlTokenTerminal>(); } }

        public SqlExprSelectColumn this[int index]
        {
            get { return (SqlExprSelectColumn)_components[index * 2]; }
        }

        /// <summary>
        /// Gets the number of <see cref="SqlTokenIdentifier"/>.
        /// </summary>
        public int Count
        {
            get { return (_components.Length + 1) / 2; }
        }

        /// <summary>
        /// Gets the identifiers.
        /// </summary>
        public IEnumerator<SqlExprSelectColumn> GetEnumerator()
        {
            return _components.Where( ( x, i ) => i % 2 == 0 ).Cast<SqlExprSelectColumn>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override IEnumerable<IAbstractExpr> Components
        {
            get { return _components; }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
