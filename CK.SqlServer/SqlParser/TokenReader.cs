using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// An <see cref="IEnumerator{T}"/> of <see cref="SqlToken"/>.
    /// </summary>
    internal class TokenReader : IEnumerator<SqlToken> 
    {
        readonly IEnumerable<SqlToken> _tokens;
        IEnumerator<SqlToken> _e;
        SqlToken _c;
        bool _comparisonContext;

        public TokenReader( IEnumerable<SqlToken> tokens )
        {
            Debug.Assert( tokens != null );
            _tokens = tokens;
        }

        public bool ComparisonContext
        {
            get { return _comparisonContext; }
            set { _comparisonContext = value; }
        }

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

        public bool Match( SqlTokenType t )
        {
            CheckPosition();
            if( _c.TokenType == t )
            {
                MoveNext();
                return true;
            }
            return false;
        }

        public void ClearError()
        {
            LastError = null;
        }

        public string LastError { get; private set; }

        public bool SetLastError( string error, params object[] parameters )
        {
            Debug.Assert( !String.IsNullOrWhiteSpace( error ) );
            LastError = String.Format( error, parameters );
            return false;
        }

        public SqlExprSyntaxError ExtractError( bool clearError = true )
        {
            SqlExprSyntaxError err = null;
            if( LastError != null )
            {
                err = new SqlExprSyntaxError( LastError );
                if( clearError ) ClearError();
            }
            else throw new InvalidOperationException( "Missing error." );
            return err;
        }

        public T Read<T>() where T : SqlToken
        {
            CheckPosition();
            T result = (T)_c;
            MoveNext();
            return result;
        }

        public bool MoveNext()
        {
            if( _e == null ) throw new ObjectDisposedException( "TokenReader" ); 
            if( _e.MoveNext() )
            {
                _c = _e.Current;
                if( _c.TokenType == SqlTokenType.Assign && _comparisonContext ) _c = new SqlTokenTerminal( SqlTokenType.Equal, _c.LeadingTrivia, _c.TrailingTrivia );
                return true;
            }
            if( !IsErrorOrEndOfInput ) _c = SqlTokenError.EndOfInput;
            return false;
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
            _c = null;
        }

        void CheckPosition()
        {
            if( _e == null ) throw new ObjectDisposedException( "TokenReader" );
            if( _c == null ) throw new InvalidOperationException( "MoveNext must be called." );
        }


    }

}
