using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using CK.Core;
using System.Globalization;
using System.Collections.Generic;
using System.Data;

namespace CK.SqlServer
{

    /// <summary>
    ///	Small tokenizer to handle javascript based language (ECMAScript).
    /// </summary>
    public class SqlTokeniser
    {
        #region Private fields

        TextReader		_inner;
        int             _idxPrevTokenEnd;
        int             _idxTokenBeg;
        int             _idxHead;
        int             _idxPrevNonComment;
        int				_nextC;

        bool			_skipComments;
        bool            _comparisonContext;

        string          _identifierValue;
        int             _integerValue;

        StringBuilder	_buffer;
        string	        _bufferString;

        int				_token;
        int             _prevNonCommentToken;

        char[] _moneyPrefix = new char[] { '\u0024', '\u00A2', '\u00A3', '\u00A4', '\u00A5', '\u09F2', '\u09F3', '\u0E3F', '\u17DB', '\u20A0', '\u20A1', '\u20A2', '\u20A3', '\u20A4', '\u20A5', '\u20A6', '\u20A7', '\u20A8', '\u20A9', '\u20AA', '\u20AB', '\u20AC', '\u20AD', '\u20AE', '\u20AF', '\u20B0', '\u20B1', '\u20B9', '\uFDFC', '\uFE69', '\uFF04', '\uFFE0', '\uFFE1', '\uFFE5', '\uFFE6' };

        #endregion

        public SqlTokeniser()
        {
            Debug.Assert( _moneyPrefix.IsSortedStrict(), "So that BinaryFind works." );
            _skipComments = true;
            _buffer = new StringBuilder( 512 );
        }

        public bool Reset( string input )
        {
            return Reset( new StringReader( input ) );
        }

        public bool Reset( TextReader input )
        {
            _inner = input;
            _idxTokenBeg = 0;
            _idxPrevTokenEnd = 0;
            _idxHead = 0;
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
        /// This uses <see cref="SqlTokenType.OpLevelMask"/> and <see cref="SqlTokenType.OpLevelShift"/>.
        /// </remarks>
        public int CurrentPrecedenceLevel
        {
            get { return PrecedenceLevel( CurrentToken ); }
        }

        /// <summary>
        /// Computes the precedence with a provision of 1 bit to ease the handling of right associative infix operators.
        /// </summary>
        /// <returns>An even precedence level between 30 and 2. 0 if the token has <see cref="SqlTokenTypeError.IsErrorOrEndOfInput"/> bit set.</returns>
        /// <remarks>
        /// This uses <see cref="SqlTokenType.OpLevelMask"/> and <see cref="SqlTokenType.OpLevelShift"/>.
        /// </remarks>
        public static int PrecedenceLevel( SqlTokenType t )
        {
            return t > 0 ? (((int)(t & SqlTokenType.OpLevelMask)) >> (int)SqlTokenType.OpLevelShift) << 1 : 0;
        }

        /// <summary>
        /// Gets the current <see cref="SqlTokenType"/> code.
        /// </summary>
        public SqlTokenType CurrentToken
        {
            get { return (SqlTokenType)_token; }
        }

        /// <summary>
        /// Gets the <see cref="SqlTokenTypeError"/> code if the parser is in error
        /// (or the end of the input is reached). <see cref="SqlTokenTypeError.None"/> if
        /// no error occured.
        /// </summary>
        public SqlTokenTypeError ErrorCode
        {
            get { return _token < 0 ? (SqlTokenTypeError)_token : SqlTokenTypeError.None; }
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
        /// True if <see cref="ErrorCode"/> is <see cref="SqlTokenTypeError.EndOfInput"/>.
        /// </summary>
        /// <returns></returns>
        public bool IsEndOfInput
        {
            get { return _token == (int)SqlTokenTypeError.EndOfInput; }
        }

        public bool IsAssignOperator
        {
            get { return (_token & (int)SqlTokenType.IsAssignOperator) != 0; }
        }

        public bool IsBinaryOperator
        {
            get { return (_token & (int)SqlTokenType.IsBasicOperator) != 0; }
        }

        public bool IsBracket
        {
            get { return (_token & (int)SqlTokenType.IsBracket) != 0; }
        }

        public bool IsCompareOperator
        {
            get { return (_token & (int)SqlTokenType.IsCompareOperator) != 0; }
        }

        public bool IsComment
        {
            get { return (_token & (int)SqlTokenType.IsComment) != 0; }
        }

        /// <summary>
        /// True if the current token is an identifier. <see cref="ReadIdentifier"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsIdentifier
        {
            get { return (_token & (int)SqlTokenType.IsIdentifier) != 0; }
        }

        public bool IsLogicalOrSetOperator
        {
            get { return (_token & (int)SqlTokenType.IsLogicalOrSetOperator) != 0; }
        }

        /// <summary>
        /// True if the token is a @variable or a literal value ('string' or 0x5454 number for instance).
        /// </summary>
        /// <param name="t">Token to test.</param>
        /// <returns>True for a variable or a literal.</returns>
        static public bool IsVariableNameOrLiteral( SqlTokenType t )
        {
            return t == SqlTokenType.IdentifierTypeVariable || (t & SqlTokenType.LitteralMask) != 0;
        }

        #region IsNumber, IsNumberFloat and IsNumberInteger
        /// <summary>
        /// True if the current token is a number. <see cref="ReadNumber"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsNumber
        {
            get { return (_token & (int)SqlTokenType.IsNumber) != 0; }
        }
        #endregion

        public bool IsPunctuation
        {
            get { return (_token & (int)SqlTokenType.IsPunctuation) != 0; }
        }

        /// <summary>
        /// True if the current token is a string. <see cref="ReadString"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsString
        {
            get { return (_token & (int)SqlTokenType.IsString) != 0; }
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
        public int TokenBegIndex
        {
            get { return _idxTokenBeg; }
        }

        /// <summary>
        /// Gets the current character index in the input stream: it corresponds to the
        /// end of the current token.
        /// </summary>
        public int TokenEndIndex
        {
            get { return _idxHead; }
        }

        /// <summary>
        /// Gets the previous token (ignoring any comments that may exist).
        /// </summary>
        public SqlTokenType PrevNonCommentToken
        {
            get { return (SqlTokenType)_prevNonCommentToken; }
        }

        /// <summary>
        /// Gets the character index in the input stream before the current token.
        /// Since it is the end of the previous token, separators (white space, comments if <see cref="SkipComments"/> is 
        /// true) before the current token are included.
        /// If SkipComments is false and a comment exists before the current token, this is the index of 
        /// the end of the comment.
        /// </summary>
        public int PrevTokenEndIndex
        {
            get { return _idxPrevTokenEnd; }
        }

        /// <summary>
        /// Gets the previous non comment token index.
        /// </summary>
        public int PrevNonCommentIndex
        {
            get { return _idxPrevNonComment; }
        }

        /// <summary>
        /// Reads a comment (with its opening and closing tags) and forwards head. Returns null and 
        /// does not forward the head if current token is not a comment. 
        /// To be able to read comments (ie. returning not null here) requires <see cref="SkipComments"/> to be false.
        /// </summary>
        /// <returns></returns>
        public string ReadComment()
        {
            return (_token & (int)SqlTokenType.IsComment) != 0 ? ReadBuffer() : null;
        }

        /// <summary>
        /// Reads a string value and forwards head. 
        /// Returns null and does not forward the head if current token is not a string. 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            return (_token & (int)SqlTokenType.IsString) != 0 ? ReadBuffer() : null;
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
        /// Reads an identifier and forwards head. 
        /// Returns false and does not forward the head if current token is not an identifier. 
        /// </summary>
        /// <returns>True if the identifier matches and head has been forwarded.</returns>
        public bool MatchIdentifier( string identifier, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase )
        {
            if( _token > 0
                && (_token & (int)SqlTokenType.IsIdentifier) != 0
                && String.Compare( _identifierValue, identifier, comparisonType ) == 0 )
            {
                Forward();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Matches a token. Forwards the head on success.
        /// </summary>
        /// <param name="token">Must be one of <see cref="SqlTokenType"/> value (not an Error one).</param>
        /// <returns>True if the given token matches.</returns>
        public bool Match( SqlTokenType token )
        {
            if( token < 0 ) throw new ArgumentException( "Token must not be an Error token." );
            if( _token == (int)token )
            {
                Forward();
                return true;
            }
            return false;
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

        public static string Explain( SqlTokenType t )
        {
            if( t < 0 )
            {
                return ((SqlTokenTypeError)t).ToString();
            }
            if( (t & SqlTokenType.IsAssignOperator) != 0 ) return _assignOperator[((int)t & 15) - 1];
            if( (t & SqlTokenType.IsBasicOperator) != 0 ) return _operator[((int)t & 15) - 1];
            if( (t & SqlTokenType.IsCompareOperator) != 0 ) return _compareOperator[((int)t & 15) - 1];
            if( (t & SqlTokenType.IsLogicalOrSetOperator) != 0 ) return _logicalOrSet[((int)t & 15) - 1];
            if( (t & SqlTokenType.IsPunctuation) != 0 ) return _punctuations[((int)t & 15) - 1];

            if( t == SqlTokenType.IdentifierNaked ) return "identifier";
            if( t == SqlTokenType.IdentifierQuoted ) return "\"quoted identifier\"";
            if( t == SqlTokenType.IdentifierQuotedBracket ) return "[quoted identifier]";
            if( t == SqlTokenType.IdentifierTypeVariable ) return "@var";
            if( t == SqlTokenType.IdentifierTypeReservedKeyword ) return "keyword";
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

            return SqlTokenType.None.ToString();
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
            _idxHead++;
            return ret;
        }

        int ReadFirstNonWhiteSpace()
        {
            int c;
            while( (c = Read()) != -1 && Char.IsWhiteSpace( (char)c ) ); 
            return c;
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
                _idxPrevTokenEnd = _idxHead;

                if( (_token & (int)SqlTokenType.IsComment) == 0 )
                {
                    // Previous token and token location are preserved.
                    _idxPrevNonComment = _idxHead;
                    _prevNonCommentToken = _token;
                }
                do
                {
                    _token = NextTokenLowLevel();
                }
                while( (_token & (int)SqlTokenType.IsComment) != 0 && _skipComments );
            }
            return _token;
        }

        int NextTokenLowLevel()
        {
            int ic = ReadFirstNonWhiteSpace();
            // Current char position is the beginning of the new current token.
            _idxTokenBeg = _idxHead;

            if( ic == -1 ) return (int)SqlTokenTypeError.EndOfInput;
            switch( ic )
            {
                case '\'': return ReadString( false );
                case '=': return _comparisonContext ? (int)SqlTokenType.Equal : (int)SqlTokenType.Assign;
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
                        
                        if( IsIdentifierStartChar( ic ) ) return ReadIdentifier( ic );
                        
                        return (int)SqlTokenTypeError.ErrorInvalidChar;
                    }
            }
        }

        private int ReadMoney( int ic )
        {
            ClearBuffer();
            _buffer.Append( (char)ic );
            for( ; ; )
            {
                if( (ic = Read()) == -1 ) return (int)SqlTokenType.Money;
                if( ic != ' ' )
                {
                    if( Read( '-' ) ) _buffer.Append( '-' );
                    int digit = FromDecDigit( ic );
                    if( digit >= 0 )
                    {
                        ReadAllKindOfNumber( digit );
                    }
                    return (int)SqlTokenTypeError.ErrorInvalidChar;
                }
            }
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
                if( ic == '\r' || ic == '\n' ) break;
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
                    return (int)SqlTokenTypeError.ErrorNumberIdentifierStartsImmediately;
                }

                if( nextRequired == 1 ) return (int)SqlTokenTypeError.ErrorNumberUnterminatedValue;
                if( IsIdentifierStartChar( ic ) ) return (int)SqlTokenTypeError.ErrorNumberIdentifierStartsImmediately;
                break;
            }
            if( hasDot )
            {
                if( hasExp ) return (int)SqlTokenType.Float;
                return (int)SqlTokenType.Decimal;
            }
            _bufferString = _buffer.ToString();
            if( Int32.TryParse( _bufferString, out _integerValue ) ) return (int)SqlTokenType.Integer;
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
        /// Tests whether an identifier must be quoted (it is empty, starts with @ or contains a character that is not valid).
        /// </summary>
        /// <param name="identifier">Identifier to test.</param>
        /// <returns>True if the identifier can be used without surrounding quotes.</returns>
        static public bool IsQuoteRequired( string identifier )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( identifier.Length > 0 )
            {
                char c = identifier[0];
                if( c != '@' && IsIdentifierStartChar( c ) )
                {
                    int i = 1;
                    while( i < identifier.Length )
                        if( !IsIdentifierChar( identifier[i++] ) ) break;
                    if( i == identifier.Length ) return false;
                }
            }
            return true;
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
            if( isVar ) return (int)SqlTokenType.IdentifierTypeVariable;

            // Not a variable.
            object mapped = SqlReservedKeyword.MapKeyword( _identifierValue );
            if( mapped != null )
            {
                if( mapped is string )
                {
                    _identifierValue = (string)mapped;
                    return (int)SqlTokenType.IdentifierTypeReservedKeyword;
                }
                _identifierValue = _identifierValue.ToLowerInvariant();
                return (int)mapped;
            }
            return (int)SqlTokenType.IdentifierNaked;
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
                        object mapped = SqlReservedKeyword.MapKeyword( _identifierValue );
                        if( mapped != null )
                        {
                            if( mapped is string )
                            {
                                _identifierValue = (string)mapped;
                                return (int)(SqlTokenType.IdentifierTypeReservedKeyword | (token & SqlTokenType.IdentifierQuoteMask));
                            }
                            Debug.Assert( ((int)mapped & (int)SqlTokenType.IsIdentifier) != 0, "IsIdentifier bit is set." );
                            Debug.Assert( ((int)mapped & (int)SqlTokenType.IdentifierTypeMask) != 0, "And a value for a known type." );
                            _identifierValue = _identifierValue.ToLowerInvariant();
                            return (int)mapped | (int)(token & SqlTokenType.IdentifierQuoteMask);
                        }
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

    }
}
