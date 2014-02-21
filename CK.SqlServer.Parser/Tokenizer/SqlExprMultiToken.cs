using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Composition of similar tokens (can be empty).
    /// Used with <see cref="SqlTokenOpenPar"/> and <see cref="SqlTokenClosePar"/>.
    /// </summary>
    /// <typeparam name="T">Token type (must be a <see cref="SqlToken"/>).</typeparam>
    public sealed class SqlExprMultiToken<T> : ISqlItem, IReadOnlyList<T> where T : SqlToken
    {
        readonly T[] _tokens;

        static readonly T[] _empty = new T[0];
        static readonly public SqlExprMultiToken<T> Empty = new SqlExprMultiToken<T>();

        public SqlExprMultiToken( params T[] tokens )
        {
            _tokens = tokens;
        }

        SqlExprMultiToken()
        {
            _tokens = _empty;
        }

        SqlExprMultiToken( T token )
        {
            _tokens = new T[] { token };
        }

        SqlExprMultiToken( T prefix, SqlExprMultiToken<T> tail )
        {
            _tokens = new T[tail._tokens.Length + 1];
            tail._tokens.CopyTo( _tokens, 1 );
            _tokens[0] = prefix;
        }

        SqlExprMultiToken( SqlExprMultiToken<T> head, T suffix )
        {
            _tokens = new T[head._tokens.Length + 1];
            head._tokens.CopyTo( _tokens, 0 );
            _tokens[head._tokens.Length] = suffix;
        }

        SqlExprMultiToken( SqlExprMultiToken<T> head, SqlExprMultiToken<T> tail )
        {
            _tokens = new T[head._tokens.Length + tail._tokens.Length];
            head._tokens.CopyTo( _tokens, 0 );
            tail._tokens.CopyTo( _tokens, head._tokens.Length );
        }

        public static SqlExprMultiToken<T> Create( T token )
        {
            if( token == null ) throw new ArgumentNullException( "token" );
            return new SqlExprMultiToken<T>( token );
        }

        public static SqlExprMultiToken<T> Create( T prefix, SqlExprMultiToken<T> tail )
        {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( tail == null ) throw new ArgumentNullException( "tail" );
            return tail != Empty ? new SqlExprMultiToken<T>( prefix, tail ) : Create( prefix );
        }

        public static SqlExprMultiToken<T> Create( SqlExprMultiToken<T> head, T suffix )
        {
            if( head == null ) throw new ArgumentNullException( "head" );
            if( suffix == null ) throw new ArgumentNullException( "suffix" );
            return head != Empty ? new SqlExprMultiToken<T>( head, suffix ) : Create( suffix );
        }

        public static SqlExprMultiToken<T> Create( SqlExprMultiToken<T> head, SqlExprMultiToken<T> tail )
        {
            if( head == null ) throw new ArgumentNullException( "head" );
            if( tail == null ) throw new ArgumentNullException( "tail" );
            if( head == Empty ) return tail;
            if( tail == Empty ) return head;
            return new SqlExprMultiToken<T>( head, tail );
        }

        public IReadOnlyCollection<T> Tokens { get { return _tokens; } }

        IEnumerable<SqlToken> ISqlItem.Tokens  { get { return _tokens; } }

        public SqlToken LastOrEmptyToken { get { return _tokens == _empty ? SqlToken.Empty : _tokens[_tokens.Length-1]; } }

        public SqlToken FirstOrEmptyToken { get { return _tokens == _empty ? SqlToken.Empty : _tokens[0]; ; } }

        public T this[int index]
        {
            get { return _tokens[index]; }
        }

        public int Count
        {
            get { return _tokens.Length; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)_tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        public override string ToString()
        {
            var b = new StringBuilder();
            _tokens.WriteTokensWithoutTrivias( String.Empty, b );
            return b.ToString();
        }
    }

}
