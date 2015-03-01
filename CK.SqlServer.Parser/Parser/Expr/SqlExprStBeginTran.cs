#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprStBeginTran.cs) is part of CK-Database. 
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
    public class SqlExprStBeginTran : SqlExprBaseSt
    {
        public SqlExprStBeginTran( SqlTokenIdentifier begin, SqlTokenIdentifier tranToken, SqlTokenIdentifier tranNameOrVariable, SqlTokenIdentifier withToken, SqlTokenIdentifier markToken, SqlTokenLiteralString description, SqlTokenTerminal terminator )
            : base( Build( begin, tranToken, tranNameOrVariable, withToken, markToken, description ),  terminator )
        {
        }

        internal SqlExprStBeginTran( ISqlItem[] components )
            : base( components )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier begin, SqlTokenIdentifier tranToken, SqlTokenIdentifier tranNameOrVariable, SqlTokenIdentifier withToken, SqlTokenIdentifier markToken, SqlTokenLiteralString description )
        {
            if( begin == null || begin.TokenType != SqlTokenType.Begin ) throw new ArgumentException( "begin" );
            if( tranToken == null || tranToken.TokenType != SqlTokenType.Transaction ) throw new ArgumentException( "tranToken" );
            if( withToken != null && withToken.TokenType != SqlTokenType.With ) throw new ArgumentException( "withToken" );
            if( withToken != null && (markToken == null || !markToken.NameEquals( "mark" )) ) throw new ArgumentException( "markToken" );

            if( tranNameOrVariable != null )
            {
                if( withToken != null )
                {
                    if( description != null )
                    {
                        return CreateArray( begin, tranToken, tranNameOrVariable, withToken, markToken, description );
                    }
                    else
                    {
                        return CreateArray( begin, tranToken, tranNameOrVariable, withToken, markToken );
                    }
                }
                else
                {
                    return CreateArray( begin, tranToken, tranNameOrVariable );
                }
            }
            else
            {
                return CreateArray( begin, tranToken );
            }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
