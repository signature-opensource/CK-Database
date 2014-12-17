#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Tokenizer\Token\SqlTokenLiteralFloat.cs) is part of CK-Database. 
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
    public sealed class SqlTokenLiteralFloat : SqlTokenBaseLiteral
    {
        readonly string _literal;

        public SqlTokenLiteralFloat( SqlTokenType t, string literal, double value, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( t != SqlTokenType.Float ) throw new ArgumentException( "Invalid token type.", "t" );
            _literal = literal;
            Value = value;
        }

        public double Value { get; private set; }

        public override string LiteralValue { get { return _literal; } }
    }


}
