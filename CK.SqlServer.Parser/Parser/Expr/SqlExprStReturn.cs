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
    public class SqlExprStReturn : SqlExprBaseSt
    {
        public SqlExprStReturn( SqlTokenIdentifier returnToken, SqlExpr value, SqlTokenTerminal terminator )
            : base( Build( returnToken, value ),  terminator )
        {
        }

        internal SqlExprStReturn( ISqlItem[] components )
            : base( components )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier returnToken, SqlExpr value )
        {
            if( returnToken == null || returnToken.TokenType != SqlTokenType.Return ) throw new ArgumentException( "returnToken" );
            return value != null ? CreateArray( returnToken, value ) : CreateArray( returnToken );
        }

        public SqlTokenIdentifier ReturnT { get { return (SqlTokenIdentifier)Slots[0]; } }
        
        public SqlExpr Value 
        { 
            get 
            {
                // Slots[1] may not exist (return) or be the terminator (return ;).
                return Slots.Length >= 2 ? Slots[1] as SqlExpr : null; 
            } 
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
