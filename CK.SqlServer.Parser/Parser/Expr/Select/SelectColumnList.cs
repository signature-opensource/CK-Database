#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\Select\SelectColumnList.cs) is part of CK-Database. 
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
    /// List of comma separated <see cref="SelectColumn"/>
    /// </summary>
    public class SelectColumnList : SqlNoExprList<SelectColumn>
    {
        public SelectColumnList( IList<ISqlItem> components )
            : base( components )
        {
        }

        internal SelectColumnList( ISqlItem[] items )
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
