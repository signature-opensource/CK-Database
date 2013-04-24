using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Label definition (a target for the goto).
    /// </summary>
    public class SqlExprStLabelDef : SqlExprBaseSt
    {
        public SqlExprStLabelDef( SqlTokenIdentifier id, SqlTokenTerminal statementTerminator )
            : base( BuildComponents( id, statementTerminator ), statementTerminator )
        {
        }

        static ISqlItem[] BuildComponents( SqlTokenIdentifier id, SqlTokenTerminal statementTerminator )
        {
            if( id == null
                || id.IsQuoted
                || id.IsKeywordName
                || id.TrailingTrivia.Count > 0
                || statementTerminator == null
                || statementTerminator.TokenType != SqlTokenType.Colon
                || statementTerminator.LeadingTrivia.Count > 0 ) throw new ArgumentException( "Invalid label: definition." );
            return CreateArray( id );
        }

        public SqlTokenIdentifier Identifier { get { return (SqlTokenIdentifier)Slots[0]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
