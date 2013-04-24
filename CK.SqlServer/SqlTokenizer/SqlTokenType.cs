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
    /// 15       . (                                                        Dotted names, expression grouping.
    /// 14       ~                                                          Bitwise NOT.
    /// 13       * /  %                                                     Multiplication, division, modulo division.
    /// 12       + - &amp; ^ |                                              + (for "Positive", "Add" and "Concatenate"), - (for "Negative" and "Subtract"), Bitwise AND, Bitwise Exclusive OR, and Bitwise OR.
    /// 
    /// 11       =(1) &gt; &lt; &gt;= &lt;= &lt;&gt; != !&gt; !&lt;         Comparison operators (last 3 ones are not ISO). 
    ///          IS BETWEEN LIKE IN                                         I added the IS keyword to handle IS [NOT] NULL and moved BETWEEN, IN and LIKE to this level and category.
    ///          
    /// 10       NOT                                                        Logical NOT (NOT as a LED introduces Between and Like. NOT has a 10 -strongest- left binding power).
    /// 9        AND                                                        Logical AND.
    /// 
    /// 8        OR                                                         Logical OR.
    ///          (moved to "comparison": IN, BETWEEN and LIKE)              Note: BETWEEN, IN and LIKE are here in the doc. 
    ///                                                                     It's not the right level up to me since the they can be followed by expressions and then by AND or OR operators.
    ///                                                                     Such AND or OR operators must not take precedence on the LIKE/IN/BETWEEN operator.
    ///                                                                     Actually, only LIKE MUST be upgraded to "comparison" operator because:
    ///                                                                     - BETWEEN can use an explicit right binding power of "comparison level" for its Start and Stop elements.
    ///                                                                     - IN, thanks to its required parenthesis can not "eat" the AND/OR following tokens.
    ///                                                                     But, for the sake of coherency, IN and BETWEEN are considered just like LIKE.
    ///          (Set operators are considered as identifiers                                                                    
    ///          for KoCall: ALL, ANY, EXISTS, SOME)                        All these operators are "like" function call i.e.: exist(...) or any(...).
    ///                                                                     Note: Exists is enclosable (like other KoCall) whereas any, some and all are not enclosable.
    /// 
    /// 7        = += -= *= /= %= &amp;= |= ^=                              Assignments (IsAssignOperator).
    ///        
    /// 5       Intersect                                                   Intersect, union and except have the same level as comma in msdn (it is not true: inersect &gt; except &gt; union [all]).                     
    /// 4       Except                                                      They act as binary operators beween SelectSpecification.                  
    /// 3       Union                                                                        
    /// 2       Order, For                                                  Consider them as operators (where left side is ISelectSpecification).
    /// 1        ,                                                          List separator (comma)
    /// 
    /// (1) For '=' token, disambiguisation between Comparison and Assignment requires a context hint: we need to know if we are in a "assignment context" or not.
    ///     This must be done at a higher level than in <see cref="SqlTokenizer"/>.
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

        IsError = SqlTokenTypeError.IsError,
        ErrorMask = SqlTokenTypeError.ErrorMask,
        ErrorTokenizerMask = SqlTokenTypeError.ErrorTokenizerMask,

        ErrorInvalidChar = SqlTokenTypeError.ErrorInvalidChar,
        ErrorIdentifierUnterminated = SqlTokenTypeError.ErrorIdentifierUnterminated,
        ErrorStringUnterminated = SqlTokenTypeError.ErrorStringUnterminated,
        ErrorNumberUnterminatedValue = SqlTokenTypeError.ErrorNumberUnterminatedValue,
        ErrorNumberValue = SqlTokenTypeError.ErrorNumberValue,
        ErrorNumberIdentifierStartsImmediately = SqlTokenTypeError.ErrorNumberIdentifierStartsImmediately,
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
        OpLevel11 = 11 << OpLevelShift, OpComparisonLevel = OpLevel11, OpNotRightLevel = OpLevel11,
        OpLevel12 = 12 << OpLevelShift,
        OpLevel13 = 13 << OpLevelShift,
        OpLevel14 = 14 << OpLevelShift,
        OpLevel15 = 15 << OpLevelShift,
        #endregion

        /// <summary>
        /// Combines all IsXXXXOperator (Assign, Basic, Compare, LogicalOrSet and SelectPart).
        /// </summary>
        AllOperatorMask = IsAssignOperator | IsBasicOperator | IsCompareOperator | IsLogicalOrSetOperator | IsSelectPart,

        /// <summary>
        /// Mask that covers IsXXX discriminators (including <see cref="IsComment"/>).
        /// </summary>
        TokenDiscriminatorMask = AllOperatorMask
                                    | IsBracket
                                    | IsIdentifier
                                    | IsNumber
                                    | IsPunctuation
                                    | IsString
                                    | IsComment,

        /// <summary>
        /// Mask that covers operators, punctuations and brakets: the token is fully defined by 
        /// the <see cref="SqlTokenType"/> itself (no associated value is necessary).
        /// </summary>
        TerminalMask = AllOperatorMask | IsBracket | IsPunctuation,

        /// <summary>
        /// Mask that covers literals: IsString &amp; IsNumber.
        /// </summary>
        LitteralMask = IsString | IsNumber,

        #region Token discriminators bits n°19 to 9 (IsAssignOperator to IsComment) - (9 is unused).
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
        /// Covers = &gt; &lt; &gt;= &lt;= &lt;&gt; != !&gt; !&lt;
        /// but also BETWEEN LIKE and IS.
        /// </summary>
        IsCompareOperator = 1 << 16,

        /// <summary>
        /// Covers identifiers.
        /// </summary>
        IsIdentifier = 1 << 15,

        /// <summary>
        /// Covers NOT, OR, AND.
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
        /// Covers select related tokens: union, intersect, except.
        /// </summary>
        IsSelectPart = 1 << 10,

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
        Assign = IsAssignOperator | OpLevel07 | 1,

        /// <summary>
        /// Bitwise Or assignment (|=).
        /// </summary>
        BitwiseOrAssign = IsAssignOperator | OpLevel07 | 2,

        /// <summary>
        /// Bitwise And assignment (&=).
        /// </summary>
        BitwiseAndAssign = IsAssignOperator | OpLevel07 | 3,

        /// <summary>
        /// Xor binary (^) operator assignment (^=).
        /// </summary>
        BitwiseXOrAssign = IsAssignOperator | OpLevel07 | 4,

        /// <summary>
        /// Add assignment (+=).
        /// </summary>
        PlusAssign = IsAssignOperator | OpLevel07 | 5,

        /// <summary>
        /// Substract assignment (-=).
        /// </summary>
        MinusAssign = IsAssignOperator | OpLevel07 | 6,

        /// <summary>
        /// Divide assignment (/=).
        /// </summary>
        DivideAssign = IsAssignOperator | OpLevel07 | 7,

        /// <summary>
        /// Multiplication assignment (*=).
        /// </summary>
        MultAssign = IsAssignOperator | OpLevel07 | 8,

        /// <summary>
        /// Modulo assignment (%=).
        /// </summary>
        ModuloAssign = IsAssignOperator | OpLevel07 | AssignOperatorCount,

        #endregion

        BasicOperatorCount = 9,
        #region IsBasicOperator: |, ^, &, +, -, /, *, % and the unary ~.

        /// <summary>
        /// Single pipe (|) bitwise OR operator.
        /// </summary>
        BitwiseOr = IsBasicOperator | OpLevel12 | 1,

        /// <summary>
        /// Xor binary (^) operator.
        /// </summary>
        BitwiseXOr = IsBasicOperator | OpLevel12 | 2,

        /// <summary>
        /// Single ampersand (&amp;) binary And operator.
        /// </summary>
        BitwiseAnd = IsBasicOperator | OpLevel12 | 3,

        /// <summary>
        /// Plus operator.
        /// </summary>
        Plus = IsBasicOperator | OpLevel12 | 4,

        /// <summary>
        /// Minus operator.
        /// </summary>
        Minus = IsBasicOperator | OpLevel12 | 5,

        /// <summary>
        /// Mult operator.
        /// </summary>
        Mult = IsBasicOperator | OpLevel13 | 6,

        /// <summary>
        /// Divide operator.
        /// </summary>
        Divide = IsBasicOperator | OpLevel13 | 7,
        /// <summary>
        /// Modulo.
        /// </summary>
        Modulo = IsBasicOperator | OpLevel13 | 8,
        /// <summary>
        /// Biwise Not (~).
        /// </summary>
        BitwiseNot = IsBasicOperator | OpLevel14 | 9,

        #endregion

        CompareOperatorCount = 13,
        #region IsCompareOperator: =, >, <, >=, <=, <>, !=, !> and !<. Plus LIKE, IN, IS and BETWEEN.
        /// <summary>
        /// = character.
        /// </summary>
        Equal = IsCompareOperator | OpLevel11 | 1,
        /// <summary>
        /// One single &gt; character.
        /// </summary>
        Greater = IsCompareOperator | OpLevel11 | 2,
        /// <summary>
        /// One single &lt; character.
        /// </summary>
        Less = IsCompareOperator | OpLevel11 | 3,
        /// <summary>
        /// Greater than or equal (&gt;)
        /// </summary>
        GreaterOrEqual = IsCompareOperator | OpLevel11 | 4,
        /// <summary>
        /// Less than or equal (&lt;=) 
        /// </summary>
        LessOrEqual = IsCompareOperator | OpLevel11 | 5,
        /// <summary>
        /// &lt;&gt; (Not Equal To)
        /// </summary>
        NotEqualTo = IsCompareOperator | OpLevel11 | 6,
        
        /// <summary>
        /// C-like difference operator !=.
        /// </summary>
        Different = IsCompareOperator | OpLevel11 | 7,
        /// <summary>
        /// !&gt; (Not Greater Than)
        /// </summary>
        NotGreaterThan = IsCompareOperator | OpLevel11 | 8,
        /// <summary>
        /// !lt; (Not Less Than)
        /// </summary>
        NotLessThan = IsCompareOperator | OpLevel11 | 9,
        /// <summary>
        /// BETWEEN operator.
        /// </summary>
        Between = IsCompareOperator | OpLevel11 | 10,
        /// <summary>
        /// LIKE operator.
        /// </summary>
        Like = IsCompareOperator | OpLevel11 | 11,
        /// <summary>
        /// IN operator.
        /// </summary>
        In = IsCompareOperator | OpLevel11 | 12,       
        /// <summary>
        /// IS operator.
        /// </summary>
        Is = IsCompareOperator | OpLevel11 | 13,
        #endregion

        LogicalOrSetCount = 3,
        #region IsLogicalOrSet: not, or, and (Keywords all, any - same as "some" - and exists are identifiers handled as KoCall).
        /// <summary>
        /// NOT operator.
        /// </summary>
        Not = IsLogicalOrSetOperator | OpLevel10 | 1,
        /// <summary>
        /// Logical OR operator.
        /// </summary>
        Or = IsLogicalOrSetOperator | OpLevel08 | 2,
        /// <summary>
        /// Logical AND operator.
        /// </summary>
        And = IsLogicalOrSetOperator | OpLevel09 | 3,
        #endregion
        
        SelectPartCount = 5,
        #region IsSelectPart: union, except, intersect, order and for.
        /// <summary>
        /// Union between select specification (lowest precedence).
        /// </summary>
        Union = IsSelectPart | OpLevel03 | 1,
        /// <summary>
        /// Except between select specification.
        /// </summary>
        Except = IsSelectPart | OpLevel04 | 2,
        /// <summary>
        /// Intersect between select specification (highest precedence).
        /// </summary>
        Intersect = IsSelectPart | OpLevel05 | 3,
        /// <summary>
        /// Order By is considered as an operator.
        /// </summary>
        Order = IsSelectPart | OpLevel02 | 4,
        /// <summary>
        /// For (xml, browse...) is considered as an operator.
        /// </summary>
        For = IsSelectPart | OpLevel02 | 5,
        #endregion

        PunctuationCount = 5,
        #region Punctuations
        /// <summary>
        /// One dot.
        /// </summary>
        Dot = IsPunctuation | OpLevel15 | 1,
        /// <summary>
        /// The comma.
        /// </summary>
        Comma = IsPunctuation | OpLevel01 | 2,
        /// <summary>
        /// Statement terminator;
        /// </summary>
        SemiColon = IsPunctuation | 3,
        /// <summary>
        /// One single colon.
        /// </summary>
        Colon = IsPunctuation | 4,
        /// <summary>
        /// Two colons :: are used to call static CLR methods.
        /// </summary>
        DoubleColons = IsPunctuation | 5,
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
        /// Mask that covers IdentifierTypeXXX values (without IsIdentifier, IsIdentifierQuoted and IsIdentifierQuotedBracket bits).
        /// When one of IdentifierTypeMask bit is set, the identifier is a reserved word regardless of its quotes (like int or "int" or [int] that is IdentifierTypeInt, 
        /// or null, "null" or [null] that is a IdentifierReservedKeyword, or a @variable).
        /// Type Identifier: an identifier corresponds to a type like int, datetime2, etc. (SqlDbType) if and only if (t&amp;IsIdentifier) != 0 && (t&amp;IdentifierMask)&gt;3;
        /// </summary>
        IdentifierMask = (1 << 6) - 1,

        /// <summary>
        /// Reserved keyword. See <see cref="SqlReservedKeyword"/>.
        /// That is not a @variable nor a known type like IdentifierTypeDateTime or IdentifierTypeInt.
        /// When a token type is equal to this IdentifierReservedKeyword, it is the unquoted form.
        /// </summary>
        IdentifierReservedKeyword = IsIdentifier | 1,

        /// <summary>
        /// Variable token: identifier that starts with @ (it is necessarily not quoted).
        /// </summary>
        IdentifierVariable = IsIdentifier | 2,

        /// <summary>
        /// Star (*) token considered as an identifier instead of <see cref="Mult"/>.
        /// This token type is not produced by <see cref="SqlTokenizer"/> (transforming the token
        /// requires more knowledge of the syntactic context).
        /// </summary>
        IdentifierStar = IsIdentifier | 3,

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
        IdentifierTypeVariant = IsIdentifier | 32,

        /// <summary>
        /// Star comment: /*...*/
        /// </summary>
        StarComment = IsComment | 1,

        /// <summary>
        /// Line comment: --... 
        /// </summary>
        LineComment = IsComment | 2,

        RoundBracket = IsBracket | 1,
        CurlyBracket = IsBracket | 4,
        OpenBracket = IsBracket | 8,
        CloseBracket = IsBracket | 16,

        OpenPar = RoundBracket | OpenBracket | OpLevel15,
        ClosePar = RoundBracket | CloseBracket,
        OpenCurly = CurlyBracket | OpenBracket,
        CloseCurly = CurlyBracket | CloseBracket,

    }

}
