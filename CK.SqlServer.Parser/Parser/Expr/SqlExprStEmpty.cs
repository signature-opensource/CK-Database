#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprStEmpty.cs) is part of CK-Database. 
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
    /// An isolated statement terminator ; is valid.
    /// </summary>
    public class SqlExprStEmpty : SqlExprBaseSt
    {
        static ISqlItem[] _empty = new ISqlItem[0];

        public SqlExprStEmpty( SqlTokenTerminal statementTerminator )
            : base( _empty, statementTerminator )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
