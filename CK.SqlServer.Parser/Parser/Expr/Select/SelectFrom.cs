#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\Select\SelectFrom.cs) is part of CK-Database. 
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
    /// Captures the optional "From ..." select part.
    /// </summary>
    public class SelectFrom : SqlNoExpr
    {
        public SelectFrom( SqlTokenIdentifier fromT, SqlExpr content )
            : this( CreateArray( fromT, content ) )
        {
        }

        internal SelectFrom( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlTokenIdentifier FromT { get { return (SqlTokenIdentifier)Slots[0]; } }
        
        public SqlExpr Content { get { return (SqlExpr)Slots[1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
