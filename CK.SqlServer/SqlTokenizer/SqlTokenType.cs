using System;

namespace CK.SqlServer
{
    /// <summary>
    /// Tokens definition.
    /// </summary>
    /// <remarks>
    /// There are only 8 operator precedence levels in T-SQL (http://msdn.microsoft.com/en-us/library/ms190276.aspx).
    /// 
    /// Operator                                                            Description
    ///
    /// 10       . (                                                        Dotted names, expression grouping.
    /// 9        ~                                                          Bitwise NOT
    /// 8        * /  %                                                     Multiplication, division, modulo division.
    /// 7        + - &amp; ^ |                                              + (for "Positive", "Add" and "Concatenate"), - (for "Negative" and "Subtract"), Bitwise AND, Bitwise Exclusive OR, and Bitwise OR.
    /// 6        = &gt; &lt; &gt;= &lt;= &lt;&gt; != !&gt; !&lt;            Comparison operators (last 3 ones are not ISO).
    /// 5        NOT                                                        Logical NOT.
    /// 4        AND                                                        Logical AND.
    /// 3        ALL, ANY, BETWEEN, EXISTS, IN, SOME, LIKE, OR              Set operators, LIKE and logical OR.
    /// 2        = += -= *= /= %= &amp;= |= ^=                              Assignments (IsAssignOperator).
    ///
    /// </remarks>
    [Flags]
    public enum SqlTokenType
    {
        /// <summary>
        /// Not a token per-se.
        /// Can be used to denote white space.
        /// </summary>
        None = 0,

        #region SqlTokenTypeError values bits (negative values)
        /// <summary>
        /// Any negative value indicates an error or the end of the input.
        /// </summary>
        IsErrorOrEndOfInput = SqlTokenTypeError.IsErrorOrEndOfInput,       
        /// <summary>
        /// Same value as <see cref="SqlTokenTypeError.EndOfInput"/>.
        /// The two most significant bits are set.
        /// </summary>
        EndOfInput = SqlTokenTypeError.EndOfInput,
        
        ErrorMask = SqlTokenTypeError.ErrorMask,
        ErrorInvalidChar = SqlTokenTypeError.ErrorInvalidChar,
        ErrorStringMask = SqlTokenTypeError.ErrorStringMask,
        ErrorNumberMask = SqlTokenTypeError.ErrorNumberMask,
        ErrorIdentifierMask = SqlTokenTypeError.ErrorIdentifierMask,
        ErrorIdentifierUnterminated = SqlTokenTypeError.ErrorIdentifierUnterminated,
        ErrorStringUnterminated = SqlTokenTypeError.ErrorStringUnterminated,
        ErrorNumberUnterminatedValue = SqlTokenTypeError.ErrorNumberUnterminatedValue,
        ErrorNumberValue = SqlTokenTypeError.ErrorNumberValue,
        #endregion

        #region Operator precedence bits n°25 to 21 (levels from 0 to 15).
        OpLevelShift = 21,
        OpLevelMask = 15 << OpLevelShift,

        OpLevel00 = 0,
        OpLevel01 = 1 << OpLevelShift,
        OpLevel02 = 2 << OpLevelShift,
        OpLevel03 = 3 << OpLevelShift,
        OpLevel04 = 4 << OpLevelShift,
        OpLevel05 = 5 << OpLevelShift,
        OpLevel06 = 6 << OpLevelShift,
        OpLevel07 = 7 << OpLevelShift,
        OpLevel08 = 8 << OpLevelShift,
        OpLevel09 = 9 << OpLevelShift,
        OpLevel10 = 10 << OpLevelShift,
        OpLevel11 = 11 << OpLevelShift,
        OpLevel12 = 12 << OpLevelShift,
        OpLevel13 = 13 << OpLevelShift,
        OpLevel14 = 14 << OpLevelShift,
        OpLevel15 = 15 << OpLevelShift,
        #endregion


        /// <summary>
        /// Combines all IsXXXXOperator (Assign, Basic, Compare, LogicalOrSet).
        /// </summary>
        AllOperatorMask = IsAssignOperator | IsBasicOperator | IsCompareOperator | IsLogicalOrSetOperator,

        /// <summary>
        /// Mask that covers IsXXX discriminators (including <see cref="IsComment"/>).
        /// </summary>
        TokenDiscriminatorMask = AllOperatorMask
                                    |IsBracket
                                    |IsIdentifier
                                    |IsNumber
                                    |IsPunctuation
                                    |IsString
                                    |IsComment,

        /// <summary>
        /// Mask that covers operators, punctuations and brakets: the token is fully defined by 
        /// the <see cref="SqlTokenType"/> itself (no associated value is necessary).
        /// </summary>
        TerminalMask = AllOperatorMask | IsBracket | IsPunctuation,

        /// <summary>
        /// Mask that covers literals: IsString & IsNumber.
        /// </summary>
        LitteralMask = IsString | IsNumber,

        #region Token discriminators bits n°19 to 9 (IsAssignOperator to IsComment) - (10 & 9 are unused).
        /// <summary>
        /// Covers = |= &amp;= ^= += -= /= *= and %=.
        /// </summary>
        IsAssignOperator = 1 << 19,

        /// <summary>
        /// Covers binary operators |, ^, &amp; +, -, /, *, % and the unary ~ (bitwise not).
        /// </summary>
        IsBasicOperator = 1 << 18,

        /// <summary>
        /// Covers [], () and {}.
        /// </summary>
        IsBracket = 1 << 17,

        /// <summary>
        /// Covers = &gt; &lt; &gt;= &lt;= &lt;&gt; != !&gt; !&lt;.
        /// </summary>
        IsCompareOperator = 1 << 16,

        /// <summary>
        /// Covers identifiers.
        /// </summary>
        IsIdentifier = 1 << 15,

        /// <summary>
        /// Covers NOT, AND, ALL, ANY, BETWEEN, EXISTS, IN, SOME, LIKE and OR
        /// </summary>
        IsLogicalOrSetOperator = 1 << 14,

        /// <summary>
        /// Covers binary, money, float and integer (hexadecimal). 
        /// </summary>
        IsNumber = 1 << 13,

        /// <summary>
        /// Covers dot ".", comma "," and semicolon ";".
        /// </summary>
        IsPunctuation = 1 << 12,

        /// <summary>
        /// Covers strings ('string' or N'string').
        /// </summary>
        IsString = 1 << 11,

        /// <summary>
        /// Unused
        /// </summary>
        // IsUnused = 1 << 10,
        // IsUnused = 1 << 9,

        /// <summary>
        /// Covers /* ... */ block as well as -- line comment.
        /// </summary>
        IsComment = 1 << 8,

        #endregion

        AssignOperatorCount = 9,
        #region IsAssignOperator: =, |=, &=, ^=, +=, -=, /=, *= and %=.
        /// <summary>
        /// Single equal character (=).
        /// </summary>
        Assign = IsAssignOperator | OpLevel02 | 1,

        /// <summary>
        /// Bitwise Or assignment (|=).
        /// </summary>
        BitwiseOrAssign = IsAssignOperator | OpLevel02 | 2,

        /// <summary>
        /// Bitwise And assignment (&=).
        /// </summary>
        BitwiseAndAssign = IsAssignOperator | OpLevel02 | 3,

        /// <summary>
        /// Xor binary (^) operator assignment (^=).
        /// </summary>
        BitwiseXOrAssign = IsAssignOperator | OpLevel02 | 4,

        /// <summary>
        /// Add assignment (+=).
        /// </summary>
        PlusAssign = IsAssignOperator | OpLevel02 | 5,

        /// <summary>
        /// Substract assignment (-=).
        /// </summary>
        MinusAssign = IsAssignOperator | OpLevel02 | 6,

        /// <summary>
        /// Divide assignment (/=).
        /// </summary>
        DivideAssign = IsAssignOperator | OpLevel02 | 7,

        /// <summary>
        /// Multiplication assignment (*=).
        /// </summary>
        MultAssign = IsAssignOperator | OpLevel02 | 8,

        /// <summary>
        /// Modulo assignment (%=).
        /// </summary>
        ModuloAssign = IsAssignOperator | OpLevel02 | AssignOperatorCount,

        #endregion

        BasicOperatorCount = 9,
        #region IsBasicOperator: |, ^, &, +, -, /, *, % and the unary ~.

        /// <summary>
        /// Single pipe (|) bitwise OR operator.
        /// </summary>
        BitwiseOr = IsBasicOperator | OpLevel07 | 1,

        /// <summary>
        /// Xor binary (^) operator.
        /// </summary>
        BitwiseXOr = IsBasicOperator | OpLevel07 | 2,

        /// <summary>
        /// Single ampersand (&amp;) binary And operator.
        /// </summary>
        BitwiseAnd = IsBasicOperator | OpLevel07 | 3,

        /// <summary>
        /// Plus operator.
        /// </summary>
        Plus = IsBasicOperator | OpLevel07 | 4,

        /// <summary>
        /// Minus operator.
        /// </summary>
        Minus = IsBasicOperator | OpLevel07 | 5,

        /// <summary>
        /// Mult operator.
        /// </summary>
        Mult = IsBasicOperator | OpLevel08 | 6,

        /// <summary>
        /// Divide operator.
        /// </summary>
        Divide = IsBasicOperator | OpLevel08 | 7,
        /// <summary>
        /// Modulo.
        /// </summary>
        Modulo = IsBasicOperator | OpLevel08 | 8,
        /// <summary>
        /// Biwise Not (~).
        /// </summary>
        BitwiseNot = IsBasicOperator | OpLevel09 | 9,

        #endregion

        CompareOperatorCount = 9,
        #region IsCompareOperator: =, >, <, >=, <=, <>, !=, !> and !<.
        /// <summary>
        /// = character.
        /// </summary>
        Equal = IsCompareOperator | OpLevel06 | 1,
        /// <summary>
        /// One single &gt; character.
        /// </summary>
        Greater = IsCompareOperator | OpLevel06 | 2,
        /// <summary>
        /// One single &lt; character.
        /// </summary>
        Less = IsCompareOperator | OpLevel06 | 3,
        /// <summary>
        /// Greater than or equal (&gt;)
        /// </summary>
        GreaterOrEqual = IsCompareOperator | OpLevel06 | 4,
        /// <summary>
        /// Less than or equal (&lt;=) 
        /// </summary>
        LessOrEqual = IsCompareOperator | OpLevel06 | 5,
        /// <summary>
        /// &lt;&gt; (Not Equal To)
        /// </summary>
        NotEqualTo = IsCompareOperator | OpLevel06 | 6,
        
        /// <summary>
        /// C-like difference operator !=.
        /// </summary>
        Different = IsCompareOperator | OpLevel06 | 7,
        /// <summary>
        /// !&gt; (Not Greater Than)
        /// </summary>
        NotGreaterThan = IsCompareOperator | OpLevel06 | 8,
        /// <summary>
        /// !lt; (Not Less Than)
        /// </summary>
        NotLessThan = IsCompareOperator | OpLevel06 | 9,

        #endregion

        LogicalOrSetCount = 9,
        #region IsLogicalOrSet: not, or, and, all, any (same as "some"), between, exists, in and like.
        /// <summary>
        /// NOT operator.
        /// </summary>
        Not = IsLogicalOrSetOperator | OpLevel05 | 1,
        /// <summary>
        /// Logical OR operator.
        /// </summary>
        Or = IsLogicalOrSetOperator | OpLevel03 | 2,
        /// <summary>
        /// Logical AND operator.
        /// </summary>
        And = IsLogicalOrSetOperator | OpLevel04 | 3,
        /// <summary>
        /// ALL operator.
        /// </summary>
        All = IsLogicalOrSetOperator | OpLevel03 | 4,
        /// <summary>
        /// ANY operator (synonym of SOME).
        /// </summary>
        Any = IsLogicalOrSetOperator | OpLevel03 | 5,
        /// <summary>
        /// BETWEEN operator.
        /// </summary>
        Between = IsLogicalOrSetOperator | OpLevel03 | 6,
        /// <summary>
        /// EXISTS operator.
        /// </summary>
        Exists = IsLogicalOrSetOperator | OpLevel03 | 7,
        /// <summary>
        /// IN operator.
        /// </summary>
        In = IsLogicalOrSetOperator | OpLevel03 | 8,
        /// <summary>
        /// LIKE operator.
        /// </summary>
        Like = IsLogicalOrSetOperator | OpLevel03 | 9,
        #endregion

        /// <summary>
        /// String literal like 'string'.
        /// </summary>
        String = IsString | 1,

        /// <summary>
        /// Unicode string literal like N'string'.
        /// </summary>
        UnicodeString = IsString | 2,

        /// <summary>
        /// Binary string constant like 0x12Ef or 0x69048AEFDD010E
        /// is a kind of Number.
        /// </summary>
        Binary = IsNumber | 1,

        /// <summary>
        /// Integer constants like 1894 or 2.
        /// </summary>
        /// <remarks>
        /// Bits are integer 0 and 1.
        /// </remarks>
        Integer = IsNumber | 2,

        /// <summary>
        /// Decimal literals like 1894.1204 or 2.0. 
        /// "Decimal" is the ISO name of Sql Server specific "numeric".
        /// </summary>
        Decimal = IsNumber | 3,
 
        /// <summary>
        /// Float and real literals like 101.5E5 or .5e-2.
        /// </summary>
        Float = IsNumber | 4,

        /// <summary>
        /// Money literals like $12 or $542023.14.
        /// </summary>
        Money = IsNumber | 5,

        /// <summary>
        /// Identifier token (not "quoted" nor [quoted]).
        /// </summary>
        IdentifierNaked = IsIdentifier,

        /// <summary>
        /// Denotes a "quoted identifier".
        /// </summary>
        IsIdentifierQuoted = 1 << 7,

        /// <summary>
        /// Denotes a [Quoted identifier].
        /// </summary>
        IsIdentifierQuotedBracket = 1 << 6,

        /// <summary>
        /// Mask that covers IdentifierTypeXXX values.
        /// </summary>
        IdentifierTypeMask = (1 << 6) - 1,

        /// <summary>
        /// Mask that covers IsIdentifierQuoted and IsIdentifierQuotedBracket bits.
        /// </summary>
        IdentifierQuoteMask = IsIdentifierQuoted | IsIdentifierQuotedBracket,

        /// <summary>
        /// Identifier "Quoted token".
        /// </summary>
        IdentifierQuoted = IsIdentifier | IsIdentifierQuoted,

        /// <summary>
        /// Identifier [Quoted token].
        /// </summary>
        IdentifierQuotedBracket = IsIdentifier | IsIdentifierQuotedBracket,

        /// <summary>
        /// Reserved keyword. See <see cref="SqlReservedKeyword"/>.
        /// </summary>
        IdentifierTypeReservedKeyword = IsIdentifier | 1,

        /// <summary>
        /// Variable token: identifier that starts with @.
        /// </summary>
        IdentifierTypeVariable = IsIdentifier | 2,


        IdentifierTypeVariant = IsIdentifier | 3,
        IdentifierTypeXml = IsIdentifier | 4,
        IdentifierTypeDateTimeOffset = IsIdentifier | 5,
        IdentifierTypeDateTime2 = IsIdentifier | 6,
        IdentifierTypeDateTime = IsIdentifier | 7,
        IdentifierTypeSmallDateTime = IsIdentifier | 8,
        IdentifierTypeDate = IsIdentifier | 9,
        IdentifierTypeTime = IsIdentifier | 10,
        IdentifierTypeFloat = IsIdentifier | 11,
        IdentifierTypeReal = IsIdentifier | 12,
        IdentifierTypeDecimal = IsIdentifier | 13,
        IdentifierTypeMoney = IsIdentifier | 14,
        IdentifierTypeSmallMoney = IsIdentifier | 15,
        IdentifierTypeBigInt = IsIdentifier | 16,
        IdentifierTypeInt = IsIdentifier | 17,
        IdentifierTypeSmallInt = IsIdentifier | 18,
        IdentifierTypeTinyInt = IsIdentifier | 19,
        IdentifierTypeBit = IsIdentifier | 20,
        IdentifierTypeNText = IsIdentifier | 21,
        IdentifierTypeText = IsIdentifier | 22,
        IdentifierTypeImage = IsIdentifier | 23,
        IdentifierTypeTimestamp = IsIdentifier | 24,
        IdentifierTypeUniqueIdentifier = IsIdentifier | 25,
        IdentifierTypeNVarChar = IsIdentifier | 26,
        IdentifierTypeNChar = IsIdentifier | 27,
        IdentifierTypeVarChar = IsIdentifier | 28,
        IdentifierTypeChar = IsIdentifier | 29,
        IdentifierTypeVarBinary = IsIdentifier | 30,
        IdentifierTypeBinary = IsIdentifier | 31,

        /// <summary>
        /// Star comment: /*...*/
        /// </summary>
        StarComment = IsComment | 1,

        /// <summary>
        /// Line comment: --... 
        /// </summary>
        LineComment = IsComment | 2,

        Dot = IsPunctuation | OpLevel10 | 1,
        Comma = IsPunctuation | 2,
        SemiColon = IsPunctuation | 3,

        RoundBracket = IsBracket | 1,
        CurlyBracket = IsBracket | 4,
        OpenBracket = IsBracket | 8,
        CloseBracket = IsBracket | 16,

        OpenPar = RoundBracket | OpenBracket | OpLevel10,
        ClosePar = RoundBracket | CloseBracket,
        OpenCurly = CurlyBracket | OpenBracket,
        CloseCurly = CurlyBracket | CloseBracket,

    }

}
