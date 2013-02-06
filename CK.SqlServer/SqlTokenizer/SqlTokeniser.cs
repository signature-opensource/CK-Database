using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using CK.Core;
using System.Globalization;
using System.Collections.Generic;

namespace CK.SqlServer
{

    /// <summary>
    ///	Small tokenizer to handle javascript based language (ECMAScript).
    /// </summary>
    public class SqlTokeniser
    {
        #region Private fields

        TextReader		_inner;
        int             _prevCharPosTokenEnd;
        int             _charPosTokenBeg;
        int             _charPos;
        int				_nextC;
        SourceLocation  _prevNonCommentLocation;
        SourceLocation  _location;
        bool			_lineInc;

        bool			_skipComments;
        bool            _comparisonContext;

        string          _identifierValue;
        int             _integerValue;

        StringBuilder	_buffer;
        string	        _bufferString;
        int				_token;
        int             _prevNonCommentToken;

        char[] _moneyPrefix = new char[] { '\u0024', '\u00A2', '\u00A3', '\u00A4', '\u00A5', '\u09F2', '\u09F3', '\u0E3F', '\u17DB', '\u20A0', '\u20A1', '\u20A2', '\u20A3', '\u20A4', '\u20A5', '\u20A6', '\u20A7', '\u20A8', '\u20A9', '\u20AA', '\u20AB', '\u20AC', '\u20AD', '\u20AE', '\u20AF', '\u20B0', '\u20B1', '\u20B9', '\uFDFC', '\uFDFC', '\uFE69', '\uFF04', '\uFFE0', '\uFFE1', '\uFFE5', '\uFFE6' };

        #endregion

        public SqlTokeniser()
        {
            Debug.Assert( _moneyPrefix.IsSortedStrict(), "So that BinaryFind works." );
            _skipComments = true;
            _buffer = new StringBuilder( 512 );
        }

        public bool Reset( string input, string source = SourceLocation.NoSource, int startLineNumber = 0, int startColumnNumber = 0 )
        {
            return Reset( new StringReader( input ), source, startLineNumber, startColumnNumber );
        }

        public bool Reset( TextReader input, string source, int startLineNumber, int startColumnNumber )
        {
            _inner = input;
            _location.Source = source ?? SourceLocation.NoSource;
            _location.Line = startLineNumber;
            _location.Column = startColumnNumber;

            _charPosTokenBeg = 0;
            _prevCharPosTokenEnd = 0;
            _charPos = 0;
            _nextC = 0;
            _token = 0;
            ClearBuffer();
            NextToken2();
            return _token >= 0;
        }

        /// <summary>
        /// Defaults to true.
        /// </summary>
        public bool SkipComments
        {
            get { return _skipComments; }
            set { _skipComments = value; }
        }

        /// <summary>
        /// Gets the current precedence level from <see cref="CurrentToken"/> with a provision of 1 bit
        /// to ease the handling of right associative infix operators (this level is even).
        /// </summary>
        /// <remarks>
        /// This uses <see cref="SqlToken.OpLevelMask"/> and <see cref="SqlToken.OpLevelShift"/>.
        /// </remarks>
        public int CurrentPrecedenceLevel
        {
            get { return PrecedenceLevel( CurrentToken ); }
        }

        /// <summary>
        /// Computes the precedence with a provision of 1 bit to ease the handling of right associative infix operators.
        /// </summary>
        /// <returns>An even precedence level between 30 and 2. 0 if the token has <see cref="SqlTokenError.IsErrorOrEndOfInput"/> bit set.</returns>
        /// <remarks>
        /// This uses <see cref="SqlToken.OpLevelMask"/> and <see cref="SqlToken.OpLevelShift"/>.
        /// </remarks>
        public static int PrecedenceLevel( SqlToken t )
        {
            return t > 0 ? (((int)(t & SqlToken.OpLevelMask)) >> (int)SqlToken.OpLevelShift) << 1 : 0;
        }

        /// <summary>
        /// Gets the current <see cref="SqlToken"/> code.
        /// </summary>
        public SqlToken CurrentToken
        {
            get { return (SqlToken)_token; }
        }

        /// <summary>
        /// Gets the <see cref="SqlTokenError"/> code if the parser is in error
        /// (or the end of the input is reached). <see cref="SqlTokenError.None"/> if
        /// no error occured.
        /// </summary>
        public SqlTokenError ErrorCode
        {
            get { return _token < 0 ? (SqlTokenError)_token : SqlTokenError.None; }
        }

        #region IsErrorOrEndOfInput, IsEndOfInput, IsAssignOperator, ..., IsUnaryOperator
        /// <summary>
        /// True if an error or the end of the stream is reached.
        /// </summary>
        /// <returns></returns>
        public bool IsErrorOrEndOfInput
        {
            get { return _token < 0; }
        }

        /// <summary>
        /// True if <see cref="ErrorCode"/> is <see cref="SqlTokenError.EndOfInput"/>.
        /// </summary>
        /// <returns></returns>
        public bool IsEndOfInput
        {
            get { return _token == (int)SqlTokenError.EndOfInput; }
        }

        public bool IsAssignOperator
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsAssignOperator) != 0; }
        }

        public bool IsBinaryOperator
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsOperator) != 0; }
        }

        public bool IsBracket
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsBracket) != 0; }
        }

        public bool IsCompareOperator
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsCompareOperator) != 0; }
        }

        public bool IsComment
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsComment) != 0; }
        }

        /// <summary>
        /// True if the current token is an identifier. <see cref="ReadIdentifier"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsIdentifier
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsIdentifier) != 0; }
        }

        public bool IsLogicalOrSet
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsLogicalOrSet) != 0; }
        }

        #region IsNumber, IsNumberFloat and IsNumberInteger
        /// <summary>
        /// True if the current token is a number. <see cref="ReadNumber"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsNumber
        {
            get { return (_token & (int)SqlToken.IsNumber) != 0; }
        }
        #endregion

        public bool IsPunctuation
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsPunctuation) != 0; }
        }

        /// <summary>
        /// True if the current token is a string. <see cref="ReadString"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsString
        {
            get { return _token >= 0 && (_token & (int)SqlToken.IsString) != 0; }
        }

        #endregion

        /// <summary>
        /// Forwards the head to the next token.
        /// </summary>
        /// <returns>True if a token is available. False if the end of the stream is encountered
        /// or an error occured.</returns>
        public bool Forward()
        {
            return NextToken2() >= 0;
        }

        /// <summary>
        /// Gets the character index in the input stream of the current token.
        /// </summary>
        public int CharPosTokenBeg
        {
            get { return _charPosTokenBeg; }
        }

        /// <summary>
        /// Gets the current character index in the input stream: it corresponds to the
        /// end of the current token.
        /// </summary>
        public int CharPosTokenEnd
        {
            get { return _charPos; }
        }

        /// <summary>
        /// Gets the current source location. A <see cref="SourceLocation"/> is a value type.
        /// </summary>
        public SourceLocation Location
        {
            get { return _location; }
        }

        /// <summary>
        /// Gets the previous token (ignoring any comments that may have occured).
        /// </summary>
        public SqlToken PrevNonCommentToken
        {
            get { return (SqlToken)_prevNonCommentToken; }
        }

        /// <summary>
        /// Gets the previous token source location. A <see cref="SourceLocation"/> is a value type.
        /// </summary>
        public SourceLocation PrevNonCommentLocation
        {
            get { return _prevNonCommentLocation; }
        }

        /// <summary>
        /// Gets the character index in the input stream before the current token.
        /// Since it is the end of the previous token, separators (white space, comments if <see cref="SkipComments"/> is 
        /// true) before the current token are included.
        /// If SkipComments is false and a comment exists before the current token, this is the index of 
        /// the end of the comment.
        /// </summary>
        public int PrevCharPosTokenEnd
        {
            get { return _prevCharPosTokenEnd; }
        }

        /// <summary>
        /// Reads a comment (with its opening and closing tags) and forwards head. Returns null and 
        /// does not forward the head if current token is not a comment. 
        /// To be able to read comments (ie. returning not null here) requires <see cref="SkipComments"/> to be false.
        /// </summary>
        /// <returns></returns>
        public string ReadComment()
        {
            return _token > 0 && (_token & (int)SqlToken.IsComment) != 0 ? ReadBuffer() : null;
        }

        /// <summary>
        /// Reads a string value and forwards head. 
        /// Returns null and does not forward the head if current token is not a string. 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            return _token > 0 && (_token & (int)SqlToken.IsString) != 0 ? ReadBuffer() : null;
        }

        /// <summary>
        /// Reads an identifier and forwards head. 
        /// Returns null and does not forward the head if current token is not an identifier. 
        /// </summary>
        /// <returns></returns>
        public string ReadIdentifier()
        {
            string id = null;
            if( IsIdentifier )
            {
                id = _identifierValue;
                Forward();
            }
            return id;
        }

        /// <summary>
        /// Reads a dotted identifier and forwards head (stops on any non identifier nor dot token). 
        /// Returns null and does not forward the head if current token is not an identifier. 
        /// </summary>
        /// <remarks>
        /// If the identifier ends with a dot, this last dot is kept in the result.
        /// </remarks>
        /// <returns>The dotted identifier or null if not found.</returns>
        public string ReadDottedIdentifier()
        {
            string multiId = null;
            string id = ReadIdentifier();
            if( id != null )
            {
                multiId = id;
                while( _token == (int)SqlToken.Dot )
                {
                    multiId += '.';
                    Forward();
                    id = ReadIdentifier();
                    if( id == null ) break;
                    multiId += id;
                }
            }
            return multiId;
        }

        /// <summary>
        /// Reads an identifier and forwards head. 
        /// Returns false and does not forward the head if current token is not an identifier. 
        /// </summary>
        /// <returns>True if the identifier matches and head has been forwarded.</returns>
        public bool MatchIdentifier( string identifier, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase )
        {
            if( _token > 0
                && (_token & (int)SqlToken.IsIdentifier) != 0
                && String.Compare( _identifierValue, identifier, comparisonType ) == 0 )
            {
                Forward();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Match identifier. Forward the head on success and can throw an exception
        /// if not found.
        /// </summary>
        public bool MatchIdentifier( string identifier, bool throwError )
        {
            if( !MatchIdentifier( identifier ) )
            {
                if( throwError )
                {
                    throw new CKException( "Identifier '{0}' expected. {1}.", identifier, _location );
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Matches a token. Forwards the head on success.
        /// </summary>
        /// <param name="token">Must be one of <see cref="SqlToken"/> value (not an Error one).</param>
        /// <returns>True if the given token matches.</returns>
        public bool Match( SqlToken token )
        {
            if( token < 0 ) throw new ArgumentException( "Token must not be an Error token." );
            if( _token == (int)token )
            {
                Forward();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Matches a token. Forwards the head on success and can throw an error
        /// if token does not match.
        /// </summary>
        /// <param name="token">Token to match (must not be an Error one).</param>
        /// <returns>True if the given token matches.</returns>
        public bool Match( SqlToken token, bool throwError )
        {
            if( !Match( token ) )
            {
                if( throwError )
                    throw new CKException( "Token {0} expected. {1}.", token.ToString(), _location );
                return false;
            }
            return true;
        }

        private string ReadBuffer()
        {
            Debug.Assert( _token > 0 );
            string r = _bufferString ?? (_bufferString = _buffer.ToString());
            Forward();
            return r;
        }

        #region Explain Token

        static string[] _assignOperator = { "=", "|=", "&=", "^=", "+=", "-=", "/=", "*=", "%=" };
        static string[] _operator = { "|", "^", "&", "+", "-", "*", "/", "%", "~" };
        static string[] _compareOperator = { "=", ">", "<", ">=", "<=", "<>", "!=", "!>", "!<" };
        static string[] _logicalOrSet = { "not", "or", "and", "all", "any", "between", "exists", "in", "some", "like" };

        static string[] _punctuations = { ".", ",", ";" };

        public static string Explain( SqlToken t )
        {
            if( t < 0 )
            {
                return ((SqlTokenError)t).ToString();
            }
            if( (t & SqlToken.IsAssignOperator) != 0 ) return _assignOperator[((int)t & 15) - 1];
            if( (t & SqlToken.IsOperator) != 0 ) return _operator[((int)t & 15) - 1];
            if( (t & SqlToken.IsCompareOperator) != 0 ) return _compareOperator[((int)t & 15) - 1];
            if( (t & SqlToken.IsLogicalOrSet) != 0 ) return _logicalOrSet[((int)t & 15) - 1];
            if( (t & SqlToken.IsPunctuation) != 0 ) return _punctuations[((int)t & 15) - 1];

            if( t == SqlToken.Identifier ) return "identifier";
            if( t == SqlToken.IdentifierQuoted ) return "\"quoted identifier\"";
            if( t == SqlToken.IdentifierQuotedBracket ) return "[quoted identifier]";
            if( t == SqlToken.Variable ) return "@var";
            if( t == SqlToken.Keyword ) return "keyword";
            if( t == SqlToken.String ) return "'string'";
            if( t == SqlToken.UnicodeString ) return "N'unicode string'";

            if( t == SqlToken.Integer ) return "42";
            if( t == SqlToken.Float ) return "6.02214129e+23";
            if( t == SqlToken.Binary ) return "0x00CF12A4";
            if( t == SqlToken.Decimal ) return "124.587";
            if( t == SqlToken.Money ) return "$548.7";

            if( t == SqlToken.StarComment ) return "/* ... */";
            if( t == SqlToken.LineComment ) return "-- ..." + Environment.NewLine;

            if( t == SqlToken.OpenPar ) return "(";
            if( t == SqlToken.ClosePar ) return ")";
            if( t == SqlToken.OpenBracket ) return "[";
            if( t == SqlToken.CloseBracket ) return "]";
            if( t == SqlToken.OpenCurly ) return "{";
            if( t == SqlToken.CloseCurly ) return "}";

            return SqlToken.None.ToString();
        }

        #endregion

        #region Basic input
        int Peek()
        {
            return _nextC == 0 ? (_nextC = _inner.Read()) : _nextC;
        }

        bool Read( int c )
        {
            if( Peek() == c )
            {
                Read();
                return true;
            }
            return false;
        }

        int Read()
        {
            int ret;
            if( _nextC != 0 )
            {
                ret = _nextC;
                _nextC = 0;
            }
            else ret = _inner.Read();

            _charPos++;

            if( _lineInc )
            {
                _location.Line++;
                _location.Column = 1;
                _lineInc = false;
            }
            if( ret != '\r' )
            {
                // Line Separator \u2028 and Paragraph Separator \u2029
                // are mapped to \n.
                if( ret == '\n' || ret == '\u2028' || ret == '\u2029' )
                {
                    ret = '\n';
                    _lineInc = true;
                }
                _location.Column++;
            }
            return ret;
        }

        int ReadFirstNonWhiteSpace()
        {
            int c;
            for( ; ; )
            {
                switch( (c = Read()) )
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n': continue;
                    default: return c;
                }
            }
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
            if( _token >= 0 )
            {
                // Current char position is the end of the previous token.
                _prevCharPosTokenEnd = _charPos;

                if( (_token & (int)SqlToken.IsComment) == 0 )
                {
                    // Previous token and token location are preserved.
                    _prevNonCommentLocation = _location;
                    _prevNonCommentToken = _token;
                }
                do
                {
                    _token = NextTokenLowLevel();
                }
                while( (_token & (int)SqlToken.IsComment) != 0 && _skipComments );
            }
            return _token;
        }

        int NextTokenLowLevel()
        {
            int ic = ReadFirstNonWhiteSpace();
            // Current char position is the beginning of the new current token.
            _charPosTokenBeg = _charPos;

            if( ic == -1 ) return (int)SqlTokenError.EndOfInput;
            switch( ic )
            {
                case '\'': return ReadString( false );
                case '=': return _comparisonContext ? (int)SqlToken.Equal : (int)SqlToken.Assign;
                case '*': return Read( '=' ) ? (int)SqlToken.MultAssign : (int)SqlToken.Mult;
                case '!':
                    if( Read( '=' ) ) return (int)SqlToken.Different;
                    if( Read( '>' ) ) return (int)SqlToken.NotGreaterThan;
                    if( Read( '<' ) ) return (int)SqlToken.NotLessThan;
                    return (int)SqlTokenError.ErrorInvalidChar;
                case '^':
                    if( Read( '=' ) ) return (int)SqlToken.BitwiseXOrAssign;
                    return (int)SqlToken.BitwiseXOr;
                case '&':
                    if( Read( '=' ) ) return (int)SqlToken.BitwiseAndAssign;
                    return (int)SqlToken.BitwiseAnd;
                case '|':
                    if( Read( '=' ) ) return (int)SqlToken.BitwiseOrAssign;
                    return (int)SqlToken.BitwiseOr;
                case '>':
                    if( Read( '=' ) ) return (int)SqlToken.GreaterOrEqual;
                    return (int)SqlToken.Greater;
                case '<':
                    if( Read( '=' ) ) return (int)SqlToken.LessOrEqual;
                    if( Read( '>' ) ) return (int)SqlToken.NotEqualTo;
                    return (int)SqlToken.Less;
                case '.':
                    // A numeric can start with a dot.
                    ic = FromDecDigit( Peek() );
                    if( ic >= 0 )
                    {
                        Read();
                        return ReadNumber( ic, true );
                    }
                    return (int)SqlToken.Dot;

                case '[': return ReadQuotedIdentifier( ']', SqlToken.IdentifierQuotedBracket );
                case '"': return ReadQuotedIdentifier( '"', SqlToken.IdentifierQuoted );
                case '{': return (int)SqlToken.OpenCurly;
                case '}': return (int)SqlToken.CloseCurly;
                case '(': return (int)SqlToken.OpenPar;
                case ')': return (int)SqlToken.ClosePar;
                case ';': return (int)SqlToken.SemiColon;
                case ',': return (int)SqlToken.Comma;
                case '/':
                    {
                        if( Read( '*' ) ) return HandleStarComment();
                        if( Read( '=' ) ) return (int)SqlToken.DivideAssign;
                        return (int)SqlToken.Divide;
                    }
                case '-':
                    if( Read( '-' ) ) return HandleLineComment();
                    if( Read( '=' ) ) return (int)SqlToken.MinusAssign;
                    return (int)SqlToken.Minus;
                case '+':
                    if( Read( '=' ) ) return (int)SqlToken.PlusAssign;
                    return (int)SqlToken.Plus;
                case '%':
                    if( Read( '=' ) ) return (int)SqlToken.ModuloAssign;
                    return (int)SqlToken.Modulo;
                case '~':
                    return (int)SqlToken.BitwiseNot;
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
                        
                        if( IsIdentifierStartChar( ic ) ) return ReadIdentifier( ic );
                        
                        return (int)SqlTokenError.ErrorInvalidChar;
                    }
            }
        }

        private int ReadMoney( int ic )
        {
            ClearBuffer();
            _buffer.Append( (char)ic );
            for( ; ; )
            {
                if( (ic = Read()) == -1 ) return (int)SqlToken.Money;
                if( ic != ' ' )
                {
                    if( Read( '-' ) ) _buffer.Append( '-' );
                    int digit = FromDecDigit( ic );
                    if( digit >= 0 )
                    {
                        ReadAllKindOfNumber( digit );
                    }
                    return (int)SqlTokenError.ErrorInvalidChar;
                }
            }
        }

        int HandleStarComment()
        {
            ClearBuffer();
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == '*' && Read( '/' ) ) return (int)SqlToken.StarComment;
                _buffer.Append( (char)ic );
            }
            return (int)SqlTokenError.EndOfInput;
        }

        int HandleLineComment()
        {
            ClearBuffer();
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == '\r' || ic == '\n' ) return (int)SqlToken.LineComment;
                _buffer.Append( (char)ic );
            }
            return (int)SqlTokenError.EndOfInput;
        }

        /// <summary>
        /// Quoted "horrible identifier" or [horrible identifier].
        /// </summary>
        /// <param name="end">Ending char.</param>
        /// <param name="token">Token type.</param>
        /// <returns>Token or error value.</returns>
        int ReadQuotedIdentifier( char end, SqlToken token )
        {
            ClearBuffer();
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == end )
                {
                    if( Peek() != end ) return (int)token;
                    Read();
                }
                _buffer.Append( (char)ic );
            }
            return (int)SqlTokenError.ErrorIdentifierUnterminated;
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
                return (int)SqlToken.Binary;
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
            if( hasDot ) _buffer.Append( "0." );
            _buffer.Append( (char)(firstDigit + '0') );
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
                    _buffer.Append( 'E' );
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
                        _buffer.Append( '.' );
                        // Dot can be the last character. It is considered as a decimal.
                        continue;
                    }
                    return (int)SqlTokenError.ErrorNumberIdentifierStartsImmediately;
                }

                if( nextRequired == 1 ) return (int)SqlTokenError.ErrorNumberUnterminatedValue;
                if( IsIdentifierStartChar( ic ) ) return (int)SqlTokenError.ErrorNumberIdentifierStartsImmediately;
                break;
            }
            if( hasDot )
            {
                if( hasExp ) return (int)SqlToken.Float;
                return (int)SqlToken.Decimal;
            }
            _bufferString = _buffer.ToString();
            if( Int32.TryParse( _bufferString, out _integerValue ) ) return (int)SqlToken.Integer;
            return (int)SqlToken.Decimal;
        }

        int ReadString( bool unicode )
        {
            ClearBuffer();
            for( ; ; )
            {
                int ic = Read();
                if( ic == -1 ) return (int)SqlTokenError.ErrorStringUnterminated;
                if( ic == '\'' )
                {
                    if( Peek() != '\'' ) return unicode ? (int)SqlToken.UnicodeString : (int)SqlToken.String;
                    Read();
                }
                _buffer.Append( (char)ic );
            }
        }

        static bool IsIdentifierStartChar( int c )
        {
            return c == '@' || c == '#' || c == '_' || Char.IsLetter( (char)c );
        }

        static bool IsIdentifierChar( int c )
        {
            return IsIdentifierStartChar( c ) || Char.IsDigit( (char)c );
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
            Debug.Assert( IsIdentifierStartChar( ic ) );
            bool isVar = ic == '@';
            ClearBuffer();
            for( ; ; )
            {
                _buffer.Append( (char)ic );
                if( (IsIdentifierChar( ic = Peek() )) ) Read();
                else break;
            }
            _identifierValue = _bufferString = _buffer.ToString();
            if( isVar ) return (int)SqlToken.Variable;

            object mapped = SqlReservedKeyword.MapKeyword( _identifierValue );
            if( mapped != null )
            {
                if( mapped is string )
                {
                    _identifierValue = (string)mapped;
                    return (int)SqlToken.Keyword;
                }
                _identifierValue = _identifierValue.ToLowerInvariant();
                return (int)mapped;
            }
            return (int)SqlToken.Identifier;
        }

        StringBuilder ClearBuffer()
        {
            _bufferString = null;
            _buffer.Clear();
            return _buffer;
        }

    }
}
