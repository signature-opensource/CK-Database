#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\SqlTokenReader.cs) is part of CK-Database. 
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
    /// <summary>
    /// An <see cref="IEnumerator{T}"/> of <see cref="SqlToken"/> decorator that acts as a reading head
    /// on a raw tokens stream. It adds useful behavior such as one token lookup and = (Assign vs. Compare) operator
    /// adaptation based on a toggle <see cref="AssignmentContext"/> flag.
    /// </summary>
    internal class SqlTokenReader : IEnumerator<SqlToken> 
    {
        readonly IEnumerable<SqlToken> _tokens;
        readonly Func<string> _currentAnalyzedText;
        readonly Func<SourcePosition> _currentTokenPosition;
        IEnumerator<SqlToken> _e;
        SqlToken _c;
        SqlToken _rawLookup;
        bool _assignmentContext;

        public SqlTokenReader( IEnumerable<SqlToken> tokens, Func<string> currentAnalyzedText, Func<SourcePosition> currentTokenPosition )
        {
            Debug.Assert( tokens != null );
            _tokens = tokens;
            _currentAnalyzedText = currentAnalyzedText;
            _currentTokenPosition = currentTokenPosition;
            Reset();
        }

        public bool AssignmentContext
        {
            get { return _assignmentContext; }
        }

        public IDisposable SetAssignmentContext( bool assignment )
        {
            if( _assignmentContext == assignment ) return Util.EmptyDisposable;
            if( (_assignmentContext = assignment) ) return Util.CreateDisposableAction( () => _assignmentContext = false );
            return Util.CreateDisposableAction( () => _assignmentContext = true );
        }

        /// <summary>
        /// Collects tokens.
        /// </summary>
        public class Collector : List<SqlToken>, IDisposable
        {
            readonly SqlTokenReader _r;

            internal Collector( SqlTokenReader r, bool addCurrentToken )
            {
                _r = r;
                if( addCurrentToken ) Add( _r._c );
                _r.TokenRead += this.Add;
            }

            /// <summary>
            /// Collects all tokens up to the end (or to the next semi colon terminator).
            /// Saves the semi colon terminator if possible.
            /// </summary>
            /// <returns></returns>
            public SqlTokenTerminal ReadToEnd( bool stopAtSemiColon = false )
            {
                SqlTokenTerminal term = null;
                if( stopAtSemiColon )
                {
                    do
                    {
                        if( term.TokenType == SqlTokenType.SemiColon )
                        {
                            term = _r.Read<SqlTokenTerminal>();
                            break;
                        }
                    }
                    while( _r.MoveNext() );
                }
                else
                {
                    while( _r.MoveNext() ) ;
                    if( Count > 0 && this[Count - 1].TokenType == SqlTokenType.SemiColon )
                    {
                        term = (SqlTokenTerminal)this[Count - 1];
                        RemoveAt( Count - 1 );
                    }
                }
                return term;
            }

            public void Dispose()
            {
                _r.TokenRead -= this.Add;
            }

        }

        /// <summary>
        /// Opens a disposable collector for tokens read by <see cref="MoveNext"/>.
        /// </summary>
        /// <returns>A disposable collector.</returns>
        public Collector OpenCollector( bool skipCurrentToken = false )
        {
            return new Collector( this, !skipCurrentToken );
        }

        /// <summary>
        /// Fires at each <see cref="MoveNext"/>.
        /// </summary>
        public event Action<SqlToken> TokenRead;

        /// <summary>
        /// Gets the current token.
        /// </summary>
        public SqlToken Current 
        { 
            get 
            {
                CheckPosition();
                return _c; 
            } 
        }

        /// <summary>
        /// Gets the current precedence with a provision of 1 bit.
        /// </summary>
        /// <remarks>
        /// This uses <see cref="SqlTokenType.OpLevelMask"/> and <see cref="SqlTokenType.OpLevelShift"/>.
        /// </remarks>
        public int CurrentPrecedenceLevel
        {
            get 
            {
                CheckPosition();
                return SqlTokenizer.PrecedenceLevel( _c.TokenType ); 
            }
        }

        /// <summary>
        /// True if an error or the end of the stream is reached (<see cref="TokenType"/> is negative).
        /// </summary>
        /// <returns>True on error or end of input.</returns>
        public bool IsErrorOrEndOfInput
        {
            get
            {
                CheckPosition();
                return _c.TokenType < 0;
            }
        }

        /// <summary>
        /// True if the end of the stream is reached (<see cref="TokenType"/> == <see cref="SqlTokenType.EndOfInput"/>).
        /// </summary>
        /// <returns>True on end of input.</returns>
        public bool IsEndOfInput
        {
            get
            {
                CheckPosition();
                return _c.TokenType == SqlTokenType.EndOfInput;
            }
        }

        /// <summary>
        /// True if a <see cref="Current"/> is an error. <see cref="ExtractError"/> can be called.
        /// This error may have been generated by the <see cref="SqlTokenizer"/> or externally created through a call to <see cref="SetCurrentError"/>.
        /// </summary>
        /// <returns>True on error.</returns>
        public bool IsError
        {
            get
            {
                CheckPosition();
                return (_c.TokenType&SqlTokenType.IsError) != 0;
            }
        }

        /// <summary>
        /// Sets the <see cref="Current"/> token as being a <see cref="SqlTokenError"/>.
        /// </summary>
        /// <param name="error">The error message format.</param>
        /// <param name="parameters">Optional parameters to be inserted in the placeholders of the <paramref name="error"/> string format.</param>
        /// <returns>Always false in order to easily write return SetCurrentError("..."); that returns false.</returns>
        public bool SetCurrentError( string error, params object[] parameters )
        {
            return SetCurrentError( String.Format( error, parameters ) );
        }

        /// <summary>
        /// Sets the <see cref="Current"/> token as being a <see cref="SqlTokenError"/>.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>Always false in order to easily write return SetCurrentError("..."); that returns false.</returns>
        public bool SetCurrentError( string error )
        {
            Debug.Assert( !String.IsNullOrWhiteSpace( error ) );
            string suffix = null;
            if( IsError )
            {
                suffix = " <- " + GetErrorMessage();
            }
            else if( _currentTokenPosition != null )
            {
                suffix = " " + _currentTokenPosition(); 
            }
            _c = new SqlTokenError( error + suffix );
            return false;
        }

        /// <summary>
        /// Get the syntax error of the current error (<see cref="Current"/> is a <see cref="SqlTokenError"/> object).
        /// <see cref="IsError"/> must be true for this method to be called.
        /// </summary>
        /// <returns>A syntax error for the <see cref="Current"/> <see cref="SqlTokenError"/>.</returns>
        public string GetErrorMessage()
        {
            if( (_c.TokenType&SqlTokenType.IsError) != 0 )
            {
                Debug.Assert( _c is SqlTokenError, "Only SqlTokenError can have an error TokenType." );
                return ((SqlTokenError)_c).ErrorMessage;
            }
            // Since we are internal, this could be a Debug.Assert:
            throw new InvalidOperationException( "Missing error." );
        }

        /// <summary>
        /// Reads the <see cref="Current"/> token that must be of the given type (otherwise an exception is thrown).
        /// Use <see cref="IsToken{T}"/> methods to test its type before reading the current token.
        /// </summary>
        /// <typeparam name="T">Type of the current token.</typeparam>
        /// <returns>The current token cast in the given type.</returns>
        public T Read<T>() where T : SqlToken
        {
            CheckPosition();
            T result = (T)_c;
            MoveNext();
            return result;
        }

        /// <summary>
        /// Reads the current token (forwards the head) if its type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of the token (must be a SqlToken).</typeparam>
        /// <param name="t">Read token or null.</param>
        /// <param name="expected">True to set an error if the current token is not of type <typeparamref name="T"/>.</param>
        /// <returns>True on success.</returns>
        public bool IsToken<T>( out T t, bool expected = true ) where T : SqlToken
        {
            return IsToken( out t, null, expected );
        }

        /// <summary>
        /// Reads the current token (forwards the head) if predicate matches.
        /// </summary>
        /// <typeparam name="T">Type of the token (must be a SqlToken).</typeparam>
        /// <param name="t">Read token or null.</param>
        /// <param name="filter">Predicate that must match.</param>
        /// <param name="expected">True to set an error if the current token does not match.</param>
        /// <returns>True on success.</returns>
        public bool IsToken<T>( out T t, Predicate<T> filter = null, bool expected = true ) where T : SqlToken
        {
            t = Current as T;
            if( t != null && (filter == null || filter( t )) )
            {
                t = Read<T>();
                return true;
            }
            if( expected ) SetCurrentError( "Expected '{0}' token. ", typeof( T ).Name.Replace( "SqlToken", String.Empty ) );
            t = null;
            return false;
        }

        /// <summary>
        /// Reads the current token (forwards the head) if type matches the given one.
        /// </summary>
        /// <typeparam name="T">Type of the token (must be a SqlToken).</typeparam>
        /// <param name="t">Read token or null.</param>
        /// <param name="type">Type of the token.</param>
        /// <param name="expected">True to set an error if the current token is not of the expected type.</param>
        /// <returns>True on success.</returns>
        public bool IsToken<T>( out T t, SqlTokenType type, bool expected ) where T : SqlToken
        {
            if( Current.TokenType == type && Current is T )
            {
                t = Read<T>();
                return true;
            }
            if( expected ) SetCurrentError( "Expected  '{0}' token.", type );
            t = null;
            return false;
        }

        public bool IsUnquotedIdentifier( out SqlTokenIdentifier identifier, string name, bool expected )
        {
            if( SqlToken.IsUnquotedIdentifier( Current, name ) )
            {
                identifier = Read<SqlTokenIdentifier>();
                return true;
            }
            if( expected ) SetCurrentError( "Expected '{0}' identifier.", name );
            identifier = null;
            return false;
        }

        /// <summary>
        /// Reads a list of tokens until a <paramref name="stopper"/> or the end of input or 
        /// an error is encountered (in such case, stopper is set to null).
        /// </summary>
        /// <typeparam name="T">Type of tokens.</typeparam>
        /// <param name="items">List of tokens or null if no tokens have been collected.</param>
        /// <param name="stopper">The stopper. Null if an error occurred or the end of the input was reached.</param>
        /// <param name="stopperDefinition">Lambda that defines what the stopper should be.</param>
        /// <param name="atLeastOne">True if at least one item should appear in the list.</param>
        /// <param name="matchers">
        /// Optional functions that can transform the <see cref="Current"/> token (and its followers) to any item. 
        /// Matchers are called up to the first one that returns an item different than the Current token.
        /// When a matcher returns null, the current token is ignored.
        /// </param>
        /// <returns>True if no error occurred.</returns>
        internal bool IsItemList<T>( out List<ISqlItem> items, out T stopper, Predicate<T> stopperDefinition, bool atLeastOne, params Func<ISqlItem>[] matchers ) where T : SqlToken
        {
            Debug.Assert( stopperDefinition != null );
            items = null;
            stopper = null;
            while( !IsErrorOrEndOfInput && !IsToken( out stopper, stopperDefinition, false ) )
            {
                if( items == null ) items = new List<ISqlItem>();
                if( matchers == null || matchers.Length == 0 )
                {
                    items.Add( Current );
                    MoveNext();
                }
                else
                {
                    ISqlItem item = Current;
                    foreach( var m in matchers )
                    {
                        item = m();
                        if( IsError ) return false;
                        if( item != Current ) break;
                        MoveNext();
                    }
                    if( item != null ) items.Add( item );
                }
            }
            if( IsError ) return false;
            if( (items == null || items.Count == 0) && atLeastOne ) return SetCurrentError( "Expected at least one token." );
            return true;
        }

        /// <summary>
        /// Gets the next token to be read.
        /// This token is not normalized: it is the token directly emitted by the inner token stream.
        /// </summary>
        public SqlToken RawLookup
        {
            get { return _rawLookup; }
        }

        /// <summary>
        /// Moves <see cref="Current"/> to the next token except if we are at the end of the input.
        /// Note that if <see cref="IsError"/> is true, the current error token is skipped.
        /// </summary>
        /// <returns>True if end of input was not reached yet.</returns>
        public bool MoveNext()
        {
            if( _e == null ) throw new ObjectDisposedException( "TokenReader" );
            if( _c == SqlTokenError.EndOfInput ) return false;
            _c = _rawLookup;
            if( _c.TokenType > 0 )
            {
                var h = TokenRead;
                if( h != null ) h( _c );
            }
            if( _c.TokenType == SqlTokenType.Equal && _assignmentContext ) _c = new SqlTokenTerminal( SqlTokenType.Assign, _c.LeadingTrivia, _c.TrailingTrivia );
            _rawLookup = _e.MoveNext() ? _e.Current : SqlTokenError.EndOfInput;
            return true;
        }

        public void Dispose()
        {
            if( _e != null )
            {
                _e.Dispose();
                _e = null;
                _c = null;
            }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public void Reset()
        {
            _e = _tokens.GetEnumerator();
            _rawLookup = _e.MoveNext() ? _e.Current : SqlTokenError.EndOfInput;
            _c = null;
        }

        [Conditional( "DEBUG" )]
        void CheckPosition()
        {
            if( _e == null ) throw new ObjectDisposedException( "TokenReader" );
            if( _c == null ) throw new InvalidOperationException( "MoveNext must be called." );
        }

        public override string ToString()
        {
            string shortToken = Current.ToString();
            if( shortToken.Length > 50 ) shortToken = shortToken.Substring( 0, 50 ) + "...";
            string msg = String.Format( "{0}: '{1}'", Current.GetType().Name.Replace( "SqlToken", String.Empty ), shortToken );
            if( _currentAnalyzedText != null ) msg += " - Text:" + _currentAnalyzedText();
            return msg;
        }

    }

}
