using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using CK.Core;
using System.Globalization;
using System.Collections.Generic;
using System.Data;

namespace CK.SqlServer.Parser
{

    /// <summary>
    ///	Sql tokenizer.
    /// </summary>
    public class SqlTokenizer
    {
        #region Private fields

        string _input;
        int _inputIdx;
        int _headPos; 

        // Lookup characters (because of comment detection 
        // in trivias, 2 characters are required).
        int			_curC0;
        int			_curC1;

        List<SqlTrivia> _leadingTrivias;
        List<SqlTrivia> _trailingTrivias;

        StringBuilder	_buffer;
        string	        _bufferString;

        string          _identifierValue;
        int             _integerValue;
        double          _doubleValue;

        int		 _tokenType;
        SqlToken _token;

        Dictionary<string,string> _stringPool = new Dictionary<string, string>();

        static char[] _moneyPrefix = new char[] { '\u0024', '\u00A2', '\u00A3', '\u00A4', '\u00A5', '\u09F2', '\u09F3', '\u0E3F', '\u17DB', '\u20A0', '\u20A1', '\u20A2', '\u20A3', '\u20A4', '\u20A5', '\u20A6', '\u20A7', '\u20A8', '\u20A9', '\u20AA', '\u20AB', '\u20AC', '\u20AD', '\u20AE', '\u20AF', '\u20B0', '\u20B1', '\u20B9', '\uFDFC', '\uFE69', '\uFF04', '\uFFE0', '\uFFE1', '\uFFE5', '\uFFE6' };

        #endregion

        [DebuggerStepThrough]
        public SqlTokenizer()
        {
            Debug.Assert( _moneyPrefix.IsSortedStrict(), "So that BinaryFind works." );
            _leadingTrivias = new List<SqlTrivia>();
            _trailingTrivias = new List<SqlTrivia>();
            _buffer = new StringBuilder( 512 );
            _stringPool = new Dictionary<string, string>();
            _stringPool.Add( " ", " " );
            _stringPool.Add( Environment.NewLine, Environment.NewLine );
            _input = String.Empty;
            _inputIdx = -1;
            _headPos = 0;
        }

        public bool Reset( string input )
        {
            if( input == null ) throw new ArgumentNullException( "input" );
            _input = input;
            _inputIdx = -1;
            _headPos = 0;
            if( (_curC0 = ReadInput()) != -1 ) _curC1 = ReadInput();
            _tokenType = 0;
            ClearBuffer();
            NextToken2();
            return _tokenType >= 0;
        }

        public string ToString( int spanText )
        {
            if( _input.Length == 0 ) return "<no input>";
            int idx = _headPos;
            if( idx > _input.Length ) idx = _input.Length;
            if( _input.Length <= spanText ) return _input.Insert( idx, "[[HEAD]]" );
            else 
            {
                int start = idx - spanText;
                if( start > 0 ) 
                {
                    int lenAfter = (start+spanText)-idx;
                    if( idx + lenAfter >= _input.Length ) 
                    {
                        return "..." + _input.Substring( start, idx - start ) + "[[HEAD]]" + _input.Substring( idx );
                    }
                    else
                    {
                        return "..." + _input.Substring( start, idx - start ) + "[[HEAD]]" + _input.Substring( idx, lenAfter ) + "...";
                    }
                }
                else
                {
                    int lenAfter = spanText - idx;
                    return _input.Substring( 0, idx ) + "[[HEAD]]" + _input.Substring( idx, lenAfter ) + "...";
                }
            }
        }

        public override string ToString()
        {
            return ToString( 300 );
        }

        /// <summary>
        /// Forwards the head to the next token.
        /// </summary>
        /// <returns>True if a token is available. False if the end of the stream is encountered
        /// or an error occurred.</returns>
        public bool Forward()
        {
            return NextToken2() >= 0;
        }

        /// <summary>
        /// True if an error or the end of the stream is reached.
        /// </summary>
        /// <returns>True on error or end of input.</returns>
        public bool IsErrorOrEndOfInput
        {
            get { return _tokenType < 0; }
        }

        /// <summary>
        /// Gets the current token.
        /// </summary>
        public SqlToken Token
        {
            get { return _token; }
        }

        /// <summary>
        /// Parses and enumerates the tokens including a final <see cref="SqlTokenError"/> (an error or the end of the input).
        /// </summary>
        /// <param name="input">Text to parse.</param>
        /// <returns>Tokens including the end of the input (<see cref="SqlTokenError"/>).</returns>
        public IEnumerable<SqlToken> Parse( string input )
        {
            Reset( input );
            yield return _token;
            for( ; ; )
            {
                if( IsErrorOrEndOfInput ) break;
                Forward();
                yield return _token;
            }
        }

        /// <summary>
        /// Parses and enumerates the tokens without the final <see cref="SqlTokenError"/> (an error or the end of the input).
        /// </summary>
        /// <param name="input">Text to parse.</param>
        /// <returns>Valid tokens (no <see cref="SqlTokenError"/> end or error).</returns>
        public IEnumerable<SqlToken> ParseWithoutError( string input )
        {
            Reset( input );
            for( ; ; )
            {
                if( IsErrorOrEndOfInput ) break;
                yield return _token;
                Forward();
            }
        }

        /// <summary>
        /// Computes the precedence with a provision of 1 bit to ease the handling of right associative infix operators.
        /// </summary>
        /// <returns>An even precedence level between 30 and 2. 
        /// It is 0 if the token has <see cref="SqlTokenTypeError.IsErrorOrEndOfInput"/> bit set.</returns>
        /// <remarks>
        /// This uses <see cref="SqlTokenType.OpLevelMask"/> and <see cref="SqlTokenType.OpLevelShift"/>.
        /// </remarks>
        public static int PrecedenceLevel( SqlTokenType t )
        {
            return t > 0 ? (((int)(t & SqlTokenType.OpLevelMask)) >> (int)SqlTokenType.OpLevelShift) << 1 : 0;
        }

        #region Explain Token

        static string[] _assignOperator = { "=", "|=", "&=", "^=", "+=", "-=", "/=", "*=", "%=" };
        static string[] _basicOperator = { "|", "^", "&", "+", "-", "*", "/", "%", "~" };
        static string[] _compareOperator = { "=", ">", "<", ">=", "<=", "<>", "!=", "!>", "!<" };
        static string[] _punctuations = { ".", ",", ";", ":", "::" };

        public static string Explain( SqlTokenType t )
        {
            Debug.Assert( _assignOperator.Length == (int)SqlTokenType.AssignOperatorCount );
            Debug.Assert( _basicOperator.Length == (int)SqlTokenType.BasicOperatorCount );
            Debug.Assert( _compareOperator.Length == (int)SqlTokenType.CompareOperatorCount );
            Debug.Assert( _punctuations.Length == (int)SqlTokenType.PunctuationCount );
            if( t < 0 )
            {
                return ((SqlTokenTypeError)t).ToString();
            }
            if( (t & SqlTokenType.IsAssignOperator) != 0 ) return _assignOperator[((int)t & 15) - 1];
            if( (t & SqlTokenType.IsBasicOperator) != 0 ) return _basicOperator[((int)t & 15) - 1];
            if( (t & SqlTokenType.IsCompareOperator) != 0 ) return _compareOperator[((int)t & 15) - 1];
            if( (t & SqlTokenType.IsPunctuation) != 0 ) return _punctuations[((int)t & 15) - 1];

            if( t == SqlTokenType.String ) return "'string'";
            if( t == SqlTokenType.UnicodeString ) return "N'unicode string'";

            if( t == SqlTokenType.Integer ) return "42";
            if( t == SqlTokenType.Float ) return "6.02214129e+23";
            if( t == SqlTokenType.Binary ) return "0x00CF12A4";
            if( t == SqlTokenType.Decimal ) return "124.587";
            if( t == SqlTokenType.Money ) return "$548.7";

            if( t == SqlTokenType.StarComment ) return "/* ... */";
            if( t == SqlTokenType.LineComment ) return "-- ..." + Environment.NewLine;

            if( t == SqlTokenType.OpenPar ) return "(";
            if( t == SqlTokenType.ClosePar ) return ")";
            if( t == SqlTokenType.OpenBracket ) return "[";
            if( t == SqlTokenType.CloseBracket ) return "]";
            if( t == SqlTokenType.OpenCurly ) return "{";
            if( t == SqlTokenType.CloseCurly ) return "}";
            
            if( (t & SqlTokenType.IsIdentifier) != 0 )
            {
                switch( t&SqlTokenType.IdentifierTypeMask )
                {
                    case SqlTokenType.IdentifierStandard: return "identifier";
                    case SqlTokenType.IdentifierReserved: return "reserved";
                    case SqlTokenType.IdentifierStandardStatement:
                    case SqlTokenType.IdentifierReservedStatement: return "statement";
                    case SqlTokenType.IdentifierQuoted: return "\"quoted identifier\"";
                    case SqlTokenType.IdentifierQuotedBracket: return "[quoted identifier]";
                    case SqlTokenType.IdentifierSpecial:
                        {
                            if( t == SqlTokenType.IdentifierStar ) return "*";
                            return "identifier-special";
                        }
                    case SqlTokenType.IdentifierDbType:
                        {
                            return SqlKeyword.FromSqlTokenTypeToSqlDbType( t ).Value.ToString();
                        }
                    case SqlTokenType.IdentifierVariable: return "@var";
                }
            }
            return SqlTokenType.None.ToString();
        }

        #endregion

        #region Implementation

        #region Basic input

        int ReadInput()
        {
            return ++_inputIdx >= _input.Length ? -1 : _input[_inputIdx];
        }

        int Peek()
        {
            return _curC0;
        }

        bool Read( int c1, int c2 )
        {
            if( _curC0 != c1 || _curC1 != c2 ) return false;
            if( (_curC0 = ReadInput()) != -1 ) _curC1 = ReadInput();
            _headPos += 2;
            return true;
        }

        bool Read( int c )
        {
            if( _curC0 != c ) return false;
            if( (_curC0 = _curC1) != -1 ) _curC1 = ReadInput();
            _headPos += 1;
            return true;
        }

        int Read()
        {
            int c;
            if( (c = _curC0) != -1 && (_curC0 = _curC1) != -1 ) _curC1 = ReadInput();
            _headPos += 1;
            return c;
        }

        int ReadLeadingWhitespace()
        {
            int c;
            ClearBuffer();
            while( (c = Read()) != -1 && Char.IsWhiteSpace( (char)c ) ) _buffer.Append( (char)c );
            if( _buffer.Length > 0 ) _leadingTrivias.Add( BuildTrivia( SqlTokenType.None, _buffer.ToString() ) );
            return c;
        }

        void CollectTrailingTrivias()
        {
            _trailingTrivias.Clear();
            int ic;
            _buffer.Length = 0;
            while( (ic = Peek()) != -1 ) 
            {
                if( Read( '/', '*' ) )
                {
                    if( _buffer.Length > 0 ) _trailingTrivias.Add( BuildTrivia( SqlTokenType.None, _buffer.ToString() ) );
                    // Unterminated Star comment is ignored.
                    if( HandleStarComment() != (int)SqlTokenType.StarComment )
                    {
                        // End of input.
                        return;
                    }
                    if( _buffer.Length > 0 )
                    {
                        _trailingTrivias.Add( BuildTrivia( SqlTokenType.StarComment, _buffer.ToString() ) );
                        _buffer.Length = 0;
                    }
                    // Continue after Star comment: remaining trivias will be added to trailing trivias.
                    continue;
                }
                if( Read( '-', '-' ) )
                {
                    if( _buffer.Length > 0 ) _trailingTrivias.Add( BuildTrivia( SqlTokenType.None, _buffer.ToString() ) );
                    HandleLineComment();
                    if( _buffer.Length > 0 ) _trailingTrivias.Add( BuildTrivia( SqlTokenType.LineComment, _buffer.ToString() ) );
                    // Line comment ends the trailing trivias.
                    return;
                }
                if( ic == '\r' || ic == '\n' || ic == '\u2028' || ic == '\u2029' )
                {
                    Read();
                    if( ic == '\r' ) Read( '\n' );
                    _buffer.Append( Environment.NewLine );
                    /// New line ends the current trailing trivia.
                    break;
                }
                if( !Char.IsWhiteSpace( (char)ic ) )
                {
                    // Any non-whitespace character ends the current trailing trivia.
                    break;
                }
                _buffer.Append( (char)ic );
                Read();
            }
            if( _buffer.Length > 0 ) _trailingTrivias.Add( BuildTrivia( SqlTokenType.None, _buffer.ToString() ) );
        }

        static private int FromHexDigit( int c )
        {
            Debug.Assert( '0' < 'A' && 'A' < 'a' );
            c -= '0';
            if( c < 0 ) return -1;
            if( c <= 9 ) return c;
            c -= 'A' - '0';
            if( c < 0 ) return -1;
            if( c <= 5 ) return 10 + c;
            c -= 'a' - 'A';
            if( c >= 0 && c <= 5 ) return 10 + c;
            return -1;
        }

        static private int FromDecDigit( int c )
        {
            c -= '0';
            return c >= 0 && c <= 9 ? c : -1;
        }

        #endregion

        int NextToken2()
        {
            if( _tokenType >= 0 )
            {
                _token = null;
                _leadingTrivias.Clear();
                for( ; ; )
                {
                    _tokenType = NextTokenLowLevel();
                    if( (_tokenType & (int)SqlTokenType.IsComment) == 0 ) break;
                    if( _buffer.Length > 0 ) _leadingTrivias.Add( BuildTrivia( (SqlTokenType)_tokenType, _buffer.ToString() ) );
                }
                if( _tokenType < 0 )
                {
                    _token = new SqlTokenError( (SqlTokenTypeError)_tokenType, _leadingTrivias.ToReadOnlyList() );
                }
                else
                {
                    Debug.Assert( (_tokenType & (int)SqlTokenType.IsComment) == 0, "Comments are considered as Trivias." );
                    // Captures buffer (if not already done) before reading trailing trivias.
                    if( _bufferString == null ) _bufferString = _buffer.ToString();
                    CollectTrailingTrivias();
                    var lead = _leadingTrivias.ToReadOnlyList();
                    var tail = _trailingTrivias.ToReadOnlyList();
                    if( (_tokenType & (int)SqlTokenType.IsIdentifier) != 0 )
                    {
                        _token = new SqlTokenIdentifier( (SqlTokenType)_tokenType, _identifierValue, lead, tail );
                    }
                    else if( (_tokenType & (int)SqlTokenType.IsString) != 0 )
                    {
                        _token = new SqlTokenLiteralString( (SqlTokenType)_tokenType, _bufferString, lead, tail );
                    }
                    else if( (_tokenType & (int)SqlTokenType.IsNumber) != 0 )
                    {
                        switch( (SqlTokenType)_tokenType )
                        {
                            case SqlTokenType.Integer: _token = new SqlTokenLiteralInteger( SqlTokenType.Integer, _integerValue, lead, tail ); break;
                            case SqlTokenType.Float: _token = new SqlTokenLiteralFloat( SqlTokenType.Float, _bufferString, _doubleValue, lead, tail ); break;
                            case SqlTokenType.Binary: _token = new SqlTokenLiteralBinary( SqlTokenType.Binary, _bufferString, lead, tail ); break;
                            case SqlTokenType.Decimal: _token = new SqlTokenLiteralDecimal( SqlTokenType.Decimal, _bufferString, lead, tail ); break;
                            case SqlTokenType.Money: _token = new SqlTokenLiteralMoney( SqlTokenType.Money, _bufferString, lead, tail ); break;
                        }
                    }
                    else
                    {
                        Debug.Assert( (_tokenType & (int)SqlTokenType.TerminalMask) != 0 );
                        if( _tokenType == (int)SqlTokenType.OpenPar ) _token = new SqlTokenOpenPar( lead, tail );
                        else if( _tokenType == (int)SqlTokenType.ClosePar ) _token = new SqlTokenClosePar( lead, tail );
                        else _token = new SqlTokenTerminal( (SqlTokenType)_tokenType, lead, tail );
                    }
                    Debug.Assert( _token != null );
                }
            }
            Debug.Assert( _token != null && (int)_token.TokenType == _tokenType );
            return _tokenType;
        }

        int NextTokenLowLevel()
        {
            int ic = ReadLeadingWhitespace();
            if( ic == -1 ) return (int)SqlTokenTypeError.EndOfInput;
            switch( ic )
            {
                case '\'': return ReadString( false );
                case '=': return (int)SqlTokenType.Equal; // For SqlTokenType.Assign, we must be in an "Assignment Context".
                case ':': return Read( ':' ) ? (int)SqlTokenType.DoubleColons : (int)SqlTokenType.Colon;
                case '*': return Read( '=' ) ? (int)SqlTokenType.MultAssign : (int)SqlTokenType.Mult;
                case '!':
                    if( Read( '=' ) ) return (int)SqlTokenType.Different;
                    if( Read( '>' ) ) return (int)SqlTokenType.NotGreaterThan;
                    if( Read( '<' ) ) return (int)SqlTokenType.NotLessThan;
                    return (int)SqlTokenTypeError.ErrorInvalidChar;
                case '^':
                    if( Read( '=' ) ) return (int)SqlTokenType.BitwiseXOrAssign;
                    return (int)SqlTokenType.BitwiseXOr;
                case '&':
                    if( Read( '=' ) ) return (int)SqlTokenType.BitwiseAndAssign;
                    return (int)SqlTokenType.BitwiseAnd;
                case '|':
                    if( Read( '=' ) ) return (int)SqlTokenType.BitwiseOrAssign;
                    return (int)SqlTokenType.BitwiseOr;
                case '>':
                    if( Read( '=' ) ) return (int)SqlTokenType.GreaterOrEqual;
                    return (int)SqlTokenType.Greater;
                case '<':
                    if( Read( '=' ) ) return (int)SqlTokenType.LessOrEqual;
                    if( Read( '>' ) ) return (int)SqlTokenType.NotEqualTo;
                    return (int)SqlTokenType.Less;
                case '.':
                    // A numeric can start with a dot.
                    ic = FromDecDigit( Peek() );
                    if( ic >= 0 )
                    {
                        Read();
                        return ReadNumber( ic, true );
                    }
                    return (int)SqlTokenType.Dot;

                case '[': return ReadQuotedIdentifier( ']', SqlTokenType.IdentifierQuotedBracket );
                case '"': return ReadQuotedIdentifier( '"', SqlTokenType.IdentifierQuoted );
                case '{': return (int)SqlTokenType.OpenCurly;
                case '}': return (int)SqlTokenType.CloseCurly;
                case '(': return (int)SqlTokenType.OpenPar;
                case ')': return (int)SqlTokenType.ClosePar;
                case ';': return (int)SqlTokenType.SemiColon;
                case ',': return (int)SqlTokenType.Comma;
                case '/':
                    {
                        if( Read( '*' ) ) return HandleStarComment();
                        if( Read( '=' ) ) return (int)SqlTokenType.DivideAssign;
                        return (int)SqlTokenType.Divide;
                    }
                case '-':
                    if( Read( '-' ) ) return HandleLineComment();
                    if( Read( '=' ) ) return (int)SqlTokenType.MinusAssign;
                    return (int)SqlTokenType.Minus;
                case '+':
                    if( Read( '=' ) ) return (int)SqlTokenType.PlusAssign;
                    return (int)SqlTokenType.Plus;
                case '%':
                    if( Read( '=' ) ) return (int)SqlTokenType.ModuloAssign;
                    return (int)SqlTokenType.Modulo;
                case '~':
                    return (int)SqlTokenType.BitwiseNot;
                default:
                    {
                        if( ic == 'N' )
                        {
                            if( Read( '\'' ) ) return ReadString( true );
                            return ReadIdentifier( ic );
                        }
                        
                        int digit = FromDecDigit( ic );
                        if( digit >= 0 ) return ReadAllKindOfNumber( digit );
                        
                        if( Array.BinarySearch( _moneyPrefix, (char)ic ) >= 0 )
                        {
                            return ReadMoney( ic );
                        }
                        
                        if( SqlToken.IsIdentifierStartChar( ic ) ) return ReadIdentifier( ic );
                        
                        return (int)SqlTokenTypeError.ErrorInvalidChar;
                    }
            }
        }

        int ReadMoney( int ic )
        {
            ClearBuffer();
            _buffer.Append( (char)ic );
            // Skips spaces and leading 0.
            while( Read( ' ' ) ) ;
            if( Read( '-' ) ) _buffer.Append( '-' );
            while( Read( '0' ) ) ;
            if( (ic = Peek()) == -1 )
            {
                // $ alone (or $     0000...000) is the same as $0 (that is $0.00).
                // Fix this (and do not let $-0).
                if( _buffer.Length == 2 ) _buffer.Replace( '-', '0', 1, 1 );
                else _buffer.Append( '0' );
                return (int)SqlTokenType.Money;
            }
            bool hasDigit = false;
            while( FromDecDigit( ic ) >= 0 )
            {
                _buffer.Append( (char)ic );
                hasDigit = true;
                Read();
                ic = Peek();
            }
            if( ic == '.' )
            {
                if( !hasDigit ) _buffer.Append( '0' );
                _buffer.Append( '.' );
                Read();
                hasDigit = false;
                while( FromDecDigit( (ic = Peek()) ) >= 0 )
                {
                    hasDigit = true;
                    _buffer.Append( (char)ic );
                    Read();
                }
                if( !hasDigit ) _buffer.Append( '0' );
            }
            if( SqlToken.IsIdentifierStartChar( ic ) ) return (int)SqlTokenTypeError.ErrorNumberUnterminatedValue;
            return (int)SqlTokenType.Money;
        }

        int HandleStarComment()
        {
            ClearBuffer();
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == '*' && Read( '/' ) ) return (int)SqlTokenType.StarComment;
                _buffer.Append( (char)ic );
            }
            return (int)SqlTokenTypeError.EndOfInput;
        }

        int HandleLineComment()
        {
            ClearBuffer();
            int ic;
            while( (ic = Read()) != -1 )
            {
                // Eats the end of line.
                // This is by design: LineComment contains the end-of-line (Line Separator \u2028 and Paragraph Separator \u2029).
                if( ic == '\r' || ic == '\n' || ic == '\u2028' || ic == '\u2029' )
                {
                    if( ic == '\r' ) Read( '\n' );
                    break;
                }
                _buffer.Append( (char)ic );
            }
            return (int)SqlTokenType.LineComment;
        }

        int ReadAllKindOfNumber( int firstDigit )
        {
            Debug.Assert( firstDigit >= 0 && firstDigit <= 9 );
            if( firstDigit == 0 && Read( 'x' ) )
            {
                ClearBuffer().Append( "0x" );
                while( FromHexDigit( Peek() ) >= 0 )
                {
                    _buffer.Append( (char)Read() );
                }
                return (int)SqlTokenType.Binary;
            }
            return ReadNumber( firstDigit, false );
        }

        /// <summary>
        /// May return an error code or a number token.
        /// Whatever the read result is, the buffer contains the token.
        /// </summary>
        int ReadNumber( int firstDigit, bool hasDot )
        {
            bool hasExp = false;
            int nextRequired = 0;
            ClearBuffer();
            if( hasDot )
            {
                _buffer.Append( "0." );
                _buffer.Append( (char)(firstDigit + '0') );
            }
            else
            {
                if( firstDigit == 0 ) while( Read( '0' ) ) ;
                else _buffer.Append( (char)(firstDigit + '0') );
            }
            for( ; ; )
            {
                int ic = Peek();
                if( ic >= '0' && ic <= '9' )
                {
                    Read();
                    _buffer.Append( (char)ic );
                    nextRequired = 0;
                    continue;
                }
                if( !hasExp && (ic == 'e' || ic == 'E') )
                {
                    Read();
                    hasExp = hasDot = true;
                    _buffer.Append( 'e' );
                    if( Read( '-' ) ) _buffer.Append( '-' );
                    else Read( '+' );
                    // At least a digit is required.
                    nextRequired = 1;
                    continue;
                }
                if( ic == '.' )
                {
                    if( !hasDot )
                    {
                        Read();
                        hasDot = true;
                        if( _buffer.Length == 0 ) _buffer.Append( "0." );
                        else _buffer.Append( '.' );
                        // Dot can be the last character. It is considered as a decimal.
                        continue;
                    }
                    return (int)SqlTokenTypeError.ErrorNumberIdentifierStartsImmediately;
                }

                if( nextRequired == 1 ) return (int)SqlTokenTypeError.ErrorNumberUnterminatedValue;
                if( SqlToken.IsIdentifierStartChar( ic ) ) return (int)SqlTokenTypeError.ErrorNumberIdentifierStartsImmediately;
                break;
            }
            _bufferString = _buffer.ToString();
            if( hasDot )
            {
                if( hasExp )
                {
                    if( Double.TryParse( _bufferString, NumberStyles.Float, CultureInfo.InvariantCulture, out _doubleValue ) ) return (int)SqlTokenType.Float;
                    return (int)SqlTokenTypeError.ErrorNumberValue;
                }
                return (int)SqlTokenType.Decimal;
            }
            if( _bufferString.Length == 0 )
            {
                _integerValue = 0;
                _bufferString = "0";
                return (int)SqlTokenType.Integer;
            }
            if( Int32.TryParse( _bufferString, out _integerValue ) )
            {
                return (int)SqlTokenType.Integer;
            }
            return (int)SqlTokenType.Decimal;
        }

        int ReadString( bool unicode )
        {
            ClearBuffer();
            for( ; ; )
            {
                int ic = Read();
                if( ic == -1 ) return (int)SqlTokenTypeError.ErrorStringUnterminated;
                if( ic == '\'' )
                {
                    if( Peek() != '\'' ) return unicode ? (int)SqlTokenType.UnicodeString : (int)SqlTokenType.String;
                    Read();
                }
                _buffer.Append( (char)ic );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ic"></param>
        /// <returns></returns>
        /// <remarks>
        /// 
        /// 1. The first character must be one of the following: 
        /// ◦ A letter as defined by the Unicode Standard 3.2. The Unicode definition of letters includes Latin characters 
        /// from a through z, from A through Z, and also letter characters from other languages.
        /// ◦ The underscore (_), at sign (@), or number sign (#). 
        /// 
        /// Certain symbols at the beginning of an identifier have special meaning in SQL Server. A regular identifier that starts 
        /// with the at sign always denotes a local variable or parameter and cannot be used as the name of any other type of object. 
        /// An identifier that starts with a number sign denotes a temporary table or procedure. An identifier that starts with double 
        /// number signs (##) denotes a global temporary object. Although the number sign or double number sign characters can be used 
        /// to begin the names of other types of objects, we do not recommend this practice.
        /// Some Transact-SQL functions have names that start with double at signs (@@). To avoid confusion with these functions, you 
        /// should not use names that start with @@. 
        ///
        /// 2. Subsequent characters can include the following: 
        /// ◦ Letters as defined in the Unicode Standard 3.2.
        /// ◦ Decimal numbers from either Basic Latin or other national scripts.
        /// ◦ The at sign, dollar sign ($), number sign, or underscore.
        /// 
        /// 3. The identifier must not be a Transact-SQL reserved word. SQL Server reserves both the uppercase and lowercase versions of reserved words.
        /// 
        /// 4. Embedded spaces or special characters are not allowed.
        /// 
        /// 5. Supplementary characters are not allowed.
        /// 
        /// </remarks>
        int ReadIdentifier( int ic )
        {
            Debug.Assert( SqlToken.IsIdentifierStartChar( ic ) );
            bool isVar = ic == '@';
            ClearBuffer();
            for( ; ; )
            {
                _buffer.Append( (char)ic );
                if( (SqlToken.IsIdentifierChar( ic = Peek() )) ) Read();
                else break;
            }
            _identifierValue = _bufferString = _buffer.ToString();
            if( isVar ) return (int)SqlTokenType.IdentifierVariable;

            // Not a variable.
            SqlTokenType mapped = SqlKeyword.MapKeyword( _identifierValue );
            if( mapped == SqlTokenType.None ) mapped = SqlTokenType.IdentifierStandard;
            return (int)mapped;
        }

        /// <summary>
        /// Quoted "horrible identifier" or [horrible identifier].
        /// </summary>
        /// <param name="end">Ending char.</param>
        /// <param name="token">Token type.</param>
        /// <returns>Token or error value.</returns>
        int ReadQuotedIdentifier( char end, SqlTokenType token )
        {
            Debug.Assert( end == '"' || end == ']' );
            Debug.Assert( token == SqlTokenType.IdentifierQuoted || token == SqlTokenType.IdentifierQuotedBracket );
            ClearBuffer();
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == end )
                {
                    if( Peek() != end )
                    {
                        _identifierValue = _bufferString = _buffer.ToString();
                        // Return the raw IdentifierQuoted or IdentifierQuotedBracket.
                        return (int)token;
                    }
                    Read();
                }
                _buffer.Append( (char)ic );
            }
            return (int)SqlTokenTypeError.ErrorIdentifierUnterminated;
        }

        StringBuilder ClearBuffer()
        {
            _bufferString = null;
            _buffer.Clear();
            return _buffer;
        }

        SqlTrivia BuildTrivia( SqlTokenType t, string text )
        {
            string shared;
            if( !_stringPool.TryGetValue( text, out shared ) )
            {
                if( t == SqlTokenType.None || text.Length < 4 )
                {
                    _stringPool.Add( text, text );
                }
                shared = text;
            }
            return new SqlTrivia( t, shared );
        }

        #endregion


    }
}
