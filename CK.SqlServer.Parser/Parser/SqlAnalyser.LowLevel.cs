using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.SqlServer.Parser
{
        public partial class SqlAnalyser
        {
            delegate bool IsExprFunc<T>( out T e, bool expected );

            /// <summary>
            /// Matches a list of comma separated expressions optionnally enclosed in parenthesis.
            /// </summary>
            /// <typeparam name="T">Type of the expressions to match.</typeparam>
            /// <param name="openPar">Optional opening parenthesis.</param>
            /// <param name="items">List of items: contains expressions and comma tokens. Can be empty if no expression have been matched.</param>
            /// <param name="closePar">Closing parenthesis. Not null if and only if an opening parenthesis exists.</param>
            /// <param name="expectParenthesis">True to expect parenthesis. An error is set if the current token is not an opening parenthesis.</param>
            /// <param name="match">Function that knows how to match an expression. If this function returns true and a null item, null is not collected into the items.</param>
            /// <returns>True on success. Can be false only if <paramref name="expectParenthesis"/> is true.</returns>
            bool IsCommaList<T>( out SqlTokenOpenPar openPar, out List<ISqlItem> items, out SqlTokenClosePar closePar, bool expectParenthesis, IsExprFunc<T> match ) where T : SqlItem
            {
                items = null;
                closePar = null;

                if( !R.IsToken( out openPar, expectParenthesis ) && expectParenthesis )
                {
                    Debug.Assert( R.IsError, "Set by R.IsToken." );
                    return false;
                }
                items = new List<ISqlItem>();
                T item;
                if( !R.IsErrorOrEndOfInput && match( out item, false ) )
                {
                    if( item != null ) items.Add( item );
                    SqlTokenTerminal comma;
                    while( R.IsToken( out comma, SqlTokenType.Comma, false ) )
                    {
                        items.Add( comma );
                        match( out item, true );
                        items.Add( item );
                    }
                }
                if( !R.IsError && openPar != null && !R.IsToken( out closePar, true ) )
                {
                    Debug.Assert( R.IsError, "Set by R.IsToken." );
                    return false;
                }
                return !R.IsError;
            }


            SqlTokenTerminal GetOptionalTerminator()
            {
                SqlTokenTerminal term;
                R.IsToken( out term, SqlTokenType.SemiColon, false );
                return term;
            }
        }
}

