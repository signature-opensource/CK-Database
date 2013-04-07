using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public interface ISqlExprEnclosable : IAbstractExpr
    {
        /// <summary>
        /// Gets the opening parenthesis. Can be empty.
        /// </summary>
        SqlExprMultiToken<SqlTokenOpenPar> Opener { get; }

        /// <summary>
        /// Gets the closing parenthesis. Can be empty.
        /// </summary>
        SqlExprMultiToken<SqlTokenClosePar> Closer { get; }

        /// <summary>
        /// Gets whether this can actually be enclosed by a new pair of parenthesis.
        /// </summary>
        bool CanEnclose { get; }

        /// <summary>
        /// Creates a new enclosable that is enclosed by a new pair of parenthesis.
        /// Must throw an <see cref="InvalidOperationException"/> if <see cref="CanEnclose"/> is false.
        /// </summary>
        /// <param name="openPar">New opening parenthesis.</param>
        /// <param name="closePar">New closing parenthesis.</param>
        /// <returns>A clone of this object enclosed by the new parenthesis.</returns>
        ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar );

        /// <summary>
        /// Gets the components of this expression: it is a mix of <see cref="SqlToken"/> and <see cref="SqlExpr"/>
        /// that are enclosed by the <see cref="Opener"/> and <see cref="Closer"/> (that may be empty).
        /// Never null nor empty since an expression covers at least one token.
        /// </summary>
        IEnumerable<IAbstractExpr> Components { get; }

        /// <summary>
        /// Gets the components without the enclosing parenthesis if they exist.
        /// </summary>
        IEnumerable<IAbstractExpr> ComponentsWithoutParenthesis { get; }

    }
}
