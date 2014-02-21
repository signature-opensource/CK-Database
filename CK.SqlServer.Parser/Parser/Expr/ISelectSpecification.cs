using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{

    public interface ISelectSpecification : ISqlItem
    {
        /// <summary>
        /// Gets the operator token type: it can be: <see cref="SqlTokenType.Union"/>, <see cref="SqlTokenType.Except"/>, <see cref="SqlTokenType.Intersect"/>
        /// if this is a <see cref="SelectCombineOperator"/>, <see cref="SqlTokenType.Order"/> for a <see cref="SelectOrderBy"/>, <see cref="SqlTokenType.For"/> 
        /// for <see cref="SelectFor"/> and <see cref="SqlTokenType.None"/> if this is a <see cref="SelectSpecification"/>.
        /// </summary>
        SqlTokenType CombinationKind { get; }

        /// <summary>
        /// Gets the columns. This is the columns of the first <see cref="SelectSpecification"/>.
        /// </summary>
        SelectColumnList Columns { get; }

        /// <summary>
        /// Gets the opening parenthesis. Can be empty.
        /// </summary>
        SqlExprMultiToken<SqlTokenOpenPar> Opener { get; }

        /// <summary>
        /// Gets the closing parenthesis. Can be empty.
        /// </summary>
        SqlExprMultiToken<SqlTokenClosePar> Closer { get; }

    }
}
