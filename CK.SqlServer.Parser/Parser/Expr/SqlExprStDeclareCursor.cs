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
    public class SqlExprStDeclareCursor : SqlExprBaseSt
    {
        public SqlExprStDeclareCursor( SqlTokenIdentifier declareToken, SqlTokenIdentifier variable, ISqlExprCursor cursor, SqlTokenTerminal terminator )
            : base( Build( declareToken, variable, cursor ),  terminator )
        {
        }

        internal SqlExprStDeclareCursor( ISqlItem[] components )
            : base( components )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier declareToken, SqlTokenIdentifier variable, ISqlExprCursor cursor )
        {
            if( declareToken == null || declareToken.TokenType != SqlTokenType.Declare ) throw new ArgumentException( "declareToken" );
            if( variable == null ) throw new ArgumentException( "variable" );
            if( cursor == null ) throw new ArgumentException( "cursor" );
            return CreateArray( declareToken, variable, cursor );
        }

        public SqlTokenIdentifier DeclareT { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlTokenIdentifier Variable { get { return (SqlTokenIdentifier)Slots[1]; } }

        public ISqlExprCursor Cursor { get { return (ISqlExprCursor)Slots[2]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
