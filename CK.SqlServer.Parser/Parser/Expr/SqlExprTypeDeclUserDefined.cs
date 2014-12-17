#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprTypeDeclUserDefined.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{

    /// <summary>
    /// A user defined type is denoted by a dotted identifier [dbo].DefinedType or single identifier like geometry.
    /// </summary>
    public class SqlExprTypeDeclUserDefined : SqlNoExpr, IReadOnlyList<SqlTokenIdentifier>, ISqlExprUnifiedTypeDecl
    {
        public SqlExprTypeDeclUserDefined( IList<ISqlItem> tokens )
            : this( CreateArray( tokens.ToArray() ) )
        {
            SqlExprBaseListWithSeparator<SqlTokenIdentifier>.CheckArray( Slots, false, false, false, SqlToken.IsDotSeparator );
        }

        internal SqlExprTypeDeclUserDefined( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlDbType DbType { get { return SqlDbType.Udt; } }

        public SqlExprTypeDeclUserDefined RemoveQuoteIfPossible( bool keepIfReservedKeyword )
        {
            ISqlItem[] c = SqlExprBaseListWithSeparator<SqlTokenIdentifier>.ReplaceNonSeparator( Slots, false, t => t.RemoveQuoteIfPossible( keepIfReservedKeyword ) );
            return c != null ? new SqlExprTypeDeclUserDefined( c ) : this;
        }

        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        public int SeparatorCount { get { return Slots.Length / 2; } }

        /// <summary>
        /// Gets the separators token.
        /// </summary>
        public IEnumerable<SqlTokenTerminal> SeparatorTokens { get { return Slots.Where( ( x, i ) => i % 2 != 0 ).Cast<SqlTokenTerminal>(); } }

        /// <summary>
        /// Gets the identifier by its index.
        /// </summary>
        /// <param name="index">Index of the identifier.</param>
        /// <returns>The identifier.</returns>
        public SqlTokenIdentifier this[int index]
        {
            get { return (SqlTokenIdentifier)Slots[index * 2]; }
        }

        /// <summary>
        /// Gets the number of <see cref="SqlTokenIdentifier"/>.
        /// </summary>
        public int Count
        {
            get { return (Slots.Length + 1) / 2; }
        }

        /// <summary>
        /// Gets the identifiers.
        /// </summary>
        public IEnumerator<SqlTokenIdentifier> GetEnumerator()
        {
            return Slots.Where( ( x, i ) => i % 2 == 0 ).Cast<SqlTokenIdentifier>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }


        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        int ISqlExprUnifiedTypeDecl.SyntaxSize
        {
            get { return -2; }
        }

        byte ISqlExprUnifiedTypeDecl.SyntaxPrecision
        {
            get { return 0; }
        }

        byte ISqlExprUnifiedTypeDecl.SyntaxScale
        {
            get { return 0; }
        }

        int ISqlExprUnifiedTypeDecl.SyntaxSecondScale
        {
            get { return -1; }
        }

    }

}
