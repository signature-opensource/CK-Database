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
    public class SqlExprStGoto : SqlExprBaseSt
    {
        public SqlExprStGoto( SqlTokenIdentifier gotoToken, SqlTokenIdentifier target, SqlTokenTerminal terminator )
            : base( Build( gotoToken, target ),  terminator )
        {
        }

        internal SqlExprStGoto( ISqlItem[] components )
            : base( components )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier gotoToken, SqlTokenIdentifier target )
        {
            if( gotoToken == null || gotoToken.TokenType != SqlTokenType.Goto ) throw new ArgumentException( "gotoToken" );
            if( target == null ) throw new ArgumentException( "goto expects a target.", "target" );
            return CreateArray( gotoToken, target );
        }

        public SqlTokenIdentifier GotoT { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlTokenIdentifier Target
        { 
            get { return (SqlTokenIdentifier)Slots[1]; } 
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
