#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprStReturn.cs) is part of CK-Database. 
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
    /// 
    /// </summary>
    public class SqlExprStSetOpt : SqlExprBaseSt
    {
        public SqlExprStSetOpt( SqlTokenIdentifier setToken, SqlExpr list, SqlTokenTerminal terminator )
            : base( Build( setToken, list ),  terminator )
        {
        }

        internal SqlExprStSetOpt( ISqlItem[] components )
            : base( components )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier setToken, SqlExpr list )
        {
            if( setToken == null || setToken.TokenType != SqlTokenType.Set ) throw new ArgumentException( "setToken" );
            if( list == null ) throw new ArgumentException( "list" );
            return CreateArray( setToken, list );
        }

        public SqlTokenIdentifier SetT { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlExpr List { get { return (SqlExpr)Slots[1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
