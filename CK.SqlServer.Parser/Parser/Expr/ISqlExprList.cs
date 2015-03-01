#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\ISqlExprList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public interface ISqlExprList<out T> : IReadOnlyList<T> where T : SqlItem
    {
        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        int SeparatorCount { get; }

        /// <summary>
        /// Gets the separators.
        /// </summary>
        IEnumerable<ISqlItem> SeparatorTokens { get; }
        
    }
}
