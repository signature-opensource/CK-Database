using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public interface ISelectSpecification : ISqlItem
    {
        /// <summary>
        /// Gets the operator token type: it can be: <see cref="SqlTokenType.Union"/>, <see cref="SqlTokenType.Except"/>, <see cref="SqlTokenType.Intersect"/>
        /// if this is a <see cref="SelectCombineOperator"/> or <see cref="SqlTokenType.None"/> if this is a <see cref="SelectSpecification"/>.
        /// </summary>
        SqlTokenType CombinationKind { get; }

        SelectColumnList Columns { get; }

        /// <summary>
        /// Gets the opening parenthesis. Can be empty.
        /// </summary>
        SqlExprMultiToken<SqlTokenOpenPar> Opener { get; }

        /// <summary>
        /// Gets the closing parenthesis. Can be empty.
        /// </summary>
        SqlExprMultiToken<SqlTokenClosePar> Closer { get; }


        bool ExtractExtensions( out SelectOrderBy orderBy, out SelectFor forPart, out ISelectSpecification cleaned );

    }
}
