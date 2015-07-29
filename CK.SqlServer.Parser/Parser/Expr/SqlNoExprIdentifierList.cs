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
    /// Non-enclosable list of comma separated <see cref="SqlTokenIdentifier"/>.
    /// </summary>
    public class SqlNoExprIdentifierList : SqlNoExprList<SqlTokenIdentifier>
    {
        public SqlNoExprIdentifierList( IList<ISqlItem> components )
            : base( components.ToArray() )
        {
        }

        internal SqlNoExprIdentifierList( ISqlItem[] items )
            : base( items )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
