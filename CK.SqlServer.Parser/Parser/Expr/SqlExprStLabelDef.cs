#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprStLabelDef.cs) is part of CK-Database. 
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
    /// Label definition (a target for the goto).
    /// </summary>
    public class SqlExprStLabelDef : SqlExprBaseSt
    {
        public SqlExprStLabelDef( SqlTokenIdentifier id, SqlTokenTerminal colon, SqlTokenTerminal statementTerminator )
            : base( Build( id, colon ), statementTerminator )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier id, SqlTokenTerminal colon )
        {
            if( id == null
                || id.IsQuoted
                || SqlKeyword.IsReservedKeyword( id.Name )
                || id.TrailingTrivia.Count > 0
                || colon == null
                || colon.TokenType != SqlTokenType.Colon
                || colon.LeadingTrivia.Count > 0 ) throw new ArgumentException( "Invalid 'label:' definition." );
            return CreateArray( id, colon );
        }

        public SqlTokenIdentifier IdentifierT { get { return (SqlTokenIdentifier)Slots[0]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
