#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Tokenizer\Token\SqlTokenLiteralMoney.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public sealed class SqlTokenLiteralMoney : SqlTokenBaseLiteral
    {
        public SqlTokenLiteralMoney( SqlTokenType t, string value, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( t != SqlTokenType.Money ) throw new ArgumentException( "Invalid token type.", "t" );
            Value = value;
        }

        /// <summary>
        /// Money is kept as a string, it is not converted to a numeric .Net type.
        /// Since Money is actually a Int64 for sql server: we could handle the conversion here...
        /// </summary>
        public string Value { get; private set; }

        public override string LiteralValue { get { return Value; } }
    }

}
