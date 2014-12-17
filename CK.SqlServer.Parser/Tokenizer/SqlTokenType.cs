#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Tokenizer\SqlTokenType.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.SqlServer.Parser
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
    /// 5       Intersect                                                   Intersect, union and except have the same level as comma in msdn (it is not true: intersect &gt; except &gt; union [all]).                     
    /// 4       Except                                                      They act as binary operators between "Select Specification".                  
    /// 3       Union                                                                        
    /// 2       Order, For                                                  Consider them as operators (where left side is ISelectSpecification).
    /// 1        ,                                                          List separator (comma)
    /// 
    /// (1) For '=' token, disambiguation between Comparison and Assignment requires a context hint: we need to know if we are in a "assignment context" or not.
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

        #region Operator precedence bits n°28 to 24 (5 bits - levels from 0 to 15, bit n°28 currently unused).
        OpLevelShift = 24,
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
        /// Combines all IsXXXXOperator (Assign, Basic, Compare).
        /// </summary>
        AllOperatorMask = IsAssignOperator | IsBasicOperator | IsCompareOperator,

        /// <summary>
        /// Mask that covers operators, punctuations and brackets: the token is fully defined by 
        /// the <see cref="SqlTokenType"/> itself (no associated value is necessary).
        /// </summary>
        TerminalMask = AllOperatorMask | IsBracket | IsPunctuation,

        /// <summary>
        /// Mask that covers literals: IsString &amp; IsNumber.
        /// </summary>
        LitteralMask = IsString | IsNumber,

        /// <summary>
        /// Mask that covers IsXXX discriminators (including <see cref="IsComment"/>).
        /// </summary>
        TokenDiscriminatorMask = IsAssignOperator
                                    | IsBasicOperator
                                    | IsBracket
                                    | IsCompareOperator
                                    | IsIdentifier
                                    | IsNumber
                                    | IsPunctuation
                                    | IsString
                                    | IsComment,

        #region Token discriminators bits n°23 to 15 (IsAssignOperator to IsComment).
        /// <summary>
        /// Covers = |= &amp;= ^= += -= /= *= and %=.
        /// </summary>
        IsAssignOperator = 1 << 23,

        /// <summary>
        /// Covers binary operators |, ^, &amp;, +, -, /, *, % and the unary ~ (bitwise not).
        /// </summary>
        IsBasicOperator = 1 << 22,

        /// <summary>
        /// Covers [], () and {}.
        /// </summary>
        IsBracket = 1 << 21,

        /// <summary>
        /// Covers = &gt; &lt; &gt;= &lt;= &lt;&gt; != !&gt; and !&lt;.
        /// </summary>
        IsCompareOperator = 1 << 20,

        /// <summary>
        /// Covers identifiers.
        /// </summary>
        IsIdentifier = 1 << 19,

        /// <summary>
        /// Covers binary, money, float and integer (hexadecimal). 
        /// </summary>
        IsNumber = 1 << 18,

        /// <summary>
        /// Covers dot ".", comma "," and semicolon ";".
        /// </summary>
        IsPunctuation = 1 << 17,

        /// <summary>
        /// Covers strings ('string' or N'string').
        /// </summary>
        IsString = 1 << 16,

        /// <summary>
        /// Covers /* ... */ block as well as -- line comment.
        /// </summary>
        IsComment = 1 << 15,

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

        CompareOperatorCount = 9,
        #region IsCompareOperator: =, >, <, >=, <=, <>, !=, !> and !<.
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

        #region Identifiers
        /// <summary>
        /// IdentifierTypeMask covers bits n°14 to 11 (16 possible types) and bit n°19 (<see cref="IsIdentifier"/>).
        /// </summary>
        IdentifierTypeMask = IsIdentifier | 15 << 11,
        
        /// <summary>
        /// IdentifierValueMask covers bits n°7 to 0.
        /// </summary>
        IdentifierValueMask = 0xFF,

        /// <summary>
        /// Not reserved keywords that can start a statement like “throw”, “get”, “move”, “receive”, etc.
        /// </summary>
        IdentifierStandardStatement = IsIdentifier | 0,

        /// <summary>
        /// Reserved keywords that starts a statement: “select”, “create”, “declare “set”, etc.
        /// </summary>
        IdentifierReservedStatement = IsIdentifier | 1 << 11,

        /// <summary>
        /// Any identifier like “max”, a table name, but not a reserved keyword like keyword like “when”, “select” or “else”
        /// nor an <see cref="IdentifierDbType"/>.
        /// </summary>
        IdentifierStandard = IsIdentifier | 2 << 11,
        
        /// <summary>
        /// Identifiers that are reserved keywords (like “identity_insert”, “clustered”, “rule”, “as”, etc.) but cannot start a statement.
        /// </summary>
        IdentifierReserved = IsIdentifier | 3 << 11,

        /// <summary>
        /// Denotes a "quoted identifier".
        /// </summary>
        IdentifierQuoted = IsIdentifier | 4 << 11,
        
        /// <summary>
        /// Denotes a [Quoted identifier].
        /// </summary>
        IdentifierQuotedBracket = IsIdentifier | 5 << 11,
        
        /// <summary>
        /// Special identifiers like star (in “select t.* from t)”, $identity, $Partition, etc.
        /// </summary>
        IdentifierSpecial = IsIdentifier | 6 << 11,

        /// <summary>
        /// SqlDbType like int, smallint, datetime, xml, etc.
        /// </summary>
        IdentifierDbType = IsIdentifier | 7 << 11,

        /// <summary>
        /// Variable token like @myVariableName or @@SystemFunctions like @@RowCount or @@Error.
        /// </summary>
        IdentifierVariable = IsIdentifier | 8 << 11,

        #region IdentifierStandardStatement values
        Throw = IdentifierStandardStatement | 1,
        Move = IdentifierStandardStatement | 2,
        Get = IdentifierStandardStatement | 3,
        Receive = IdentifierStandardStatement | 4,
        Send = IdentifierStandardStatement | 5,
        #endregion

        #region IdentifierStandard values
        Try             = IdentifierStandard | 1,
        Catch           = IdentifierStandard | 2,
        Dialog          = IdentifierStandard | 3,
        Conversation    = IdentifierStandard | 4,
        Returns         = IdentifierStandard | 5,
        Max             = IdentifierStandard | 6,
        Readonly        = IdentifierStandard | 7,
        Output          = IdentifierStandard | 8,
        /// <summary>
        /// Rows is the official ISO identifier. Sql Server also accepts Row.
        /// </summary>
        Rows            = IdentifierStandard | 9,
        Offset          = IdentifierStandard | 10,
        First           = IdentifierStandard | 11,
        Next            = IdentifierStandard | 12,
        Only            = IdentifierStandard | 13,
        Cast            = IdentifierStandard | 14,
        #endregion

        #region IdentifierSpecial values
        /// <summary>
        /// Star (*) token considered as an identifier instead of <see cref="Mult"/>.
        /// This token type is not produced by <see cref="SqlTokenizer"/> (transforming the token
        /// requires more knowledge of the syntactic context).
        /// </summary>
        IdentifierStar = IdentifierSpecial | 1,

        #endregion

        #region IdentifierReserved values
        #region Logical operators: not, or, and (Keywords all, any - same as "some" - and exists are identifiers handled as KoCall).
        /// <summary>
        /// NOT operator.
        /// </summary>
        Not = OpLevel10 | IdentifierReserved | 1,
        /// <summary>
        /// Logical OR operator.
        /// </summary>
        Or = OpLevel08 | IdentifierReserved | 2,
        /// <summary>
        /// Logical AND operator.
        /// </summary>
        And = OpLevel09 | IdentifierReserved | 3,
        #endregion

        #region Select operators: Union, Except, Intersect, Order and For.
        /// <summary>
        /// Union between select specification (lowest precedence).
        /// </summary>
        Union = OpLevel03 | IdentifierReserved | 4,
        /// <summary>
        /// Except between select specification.
        /// </summary>
        Except = OpLevel04 | IdentifierReserved | 5,
        /// <summary>
        /// Intersect between select specification (highest precedence).
        /// </summary>
        Intersect = OpLevel05 | IdentifierReserved | 6,
        /// <summary>
        /// Order By is considered as an operator.
        /// </summary>
        Order = OpLevel02 | IdentifierReserved | 7,
        /// <summary>
        /// For (xml, browse...) is considered as an operator.
        /// </summary>
        For = OpLevel02 | IdentifierReserved | 8,
        /// <summary>
        /// Collate is an operator that has a high precedence (the same as bitwise ~).
        /// </summary>
        Collate = OpLevel14 | IdentifierReserved | 9,

        #endregion

        #region Between, Like, In, Is (act as comparison operators).
        /// <summary>
        /// BETWEEN operator.
        /// </summary>
        Between = OpLevel11 | IdentifierReserved | 9,
        /// <summary>
        /// LIKE operator.
        /// </summary>
        Like = OpLevel11 | IdentifierReserved | 10,
        /// <summary>
        /// IN operator.
        /// </summary>
        In = OpLevel11 | IdentifierReserved | 11,
        /// <summary>
        /// IS operator.
        /// </summary>
        Is = OpLevel11 | IdentifierReserved | 12,
        #endregion

        IdentifierReservedFirstNonOperator = IdentifierReserved | 13,
        Case        = IdentifierReservedFirstNonOperator,
        Null        = IdentifierReservedFirstNonOperator + 1,
        When        = IdentifierReservedFirstNonOperator + 2,
        By          = IdentifierReservedFirstNonOperator + 3,
        All         = IdentifierReservedFirstNonOperator + 4,
        Then        = IdentifierReservedFirstNonOperator + 5,
        Else        = IdentifierReservedFirstNonOperator + 6,
        Transaction = IdentifierReservedFirstNonOperator + 7,
        With        = IdentifierReservedFirstNonOperator + 8,
        Procedure   = IdentifierReservedFirstNonOperator + 9,
        Function    = IdentifierReservedFirstNonOperator + 10,
        View        = IdentifierReservedFirstNonOperator + 11,
        Table       = IdentifierReservedFirstNonOperator + 12,
        Trigger     = IdentifierReservedFirstNonOperator + 13,
        As          = IdentifierReservedFirstNonOperator + 14,
        Asc         = IdentifierReservedFirstNonOperator + 15,
        Desc        = IdentifierReservedFirstNonOperator + 16,
        Exists      = IdentifierReservedFirstNonOperator + 17,
        On          = IdentifierReservedFirstNonOperator + 18,
        To          = IdentifierReservedFirstNonOperator + 19,
        Of          = IdentifierReservedFirstNonOperator + 20,
        Top         = IdentifierReservedFirstNonOperator + 21,
        Escape      = IdentifierReservedFirstNonOperator + 22,
        Into        = IdentifierReservedFirstNonOperator + 23,
        From        = IdentifierReservedFirstNonOperator + 24,
        Where       = IdentifierReservedFirstNonOperator + 25,
        Group       = IdentifierReservedFirstNonOperator + 26,
        Option      = IdentifierReservedFirstNonOperator + 27,
        Add         = IdentifierReservedFirstNonOperator + 28,
        Database    = IdentifierReservedFirstNonOperator + 29,
        External    = IdentifierReservedFirstNonOperator + 30,
        Over        = IdentifierReservedFirstNonOperator + 31,

        Cross       = IdentifierReservedFirstNonOperator + 32,
        Foreign     = IdentifierReservedFirstNonOperator + 33,
        Clustered   = IdentifierReservedFirstNonOperator + 34,
        Left        = IdentifierReservedFirstNonOperator + 35,
        Percent     = IdentifierReservedFirstNonOperator + 36,
        Values      = IdentifierReservedFirstNonOperator + 37,
        Distinct    = IdentifierReservedFirstNonOperator + 38,
        Pivot       = IdentifierReservedFirstNonOperator + 39,
        Having      = IdentifierReservedFirstNonOperator + 40,
        #endregion

        #region IdentifierReservedStatement values
        Select      = IdentifierReservedStatement | 1,
        Begin       = IdentifierReservedStatement | 2,
        End         = IdentifierReservedStatement | 3,
        Create      = IdentifierReservedStatement | 4,
        Drop        = IdentifierReservedStatement | 5,
        Alter       = IdentifierReservedStatement | 6,
        Declare     = IdentifierReservedStatement | 7,
        Break       = IdentifierReservedStatement | 8,
        Continue    = IdentifierReservedStatement | 9,
        Goto        = IdentifierReservedStatement | 10,
        While       = IdentifierReservedStatement | 11,
        If          = IdentifierReservedStatement | 12,
        Deallocate  = IdentifierReservedStatement | 13,
        Close       = IdentifierReservedStatement | 14,
        Open        = IdentifierReservedStatement | 15,
        Fetch       = IdentifierReservedStatement | 16,
        Return      = IdentifierReservedStatement | 17,
        Execute     = IdentifierReservedStatement | 18,
        #endregion

        #region IdentifierDbType values
        IdentifierTypeXml = IdentifierDbType | 0,
        IdentifierTypeDateTimeOffset = IdentifierDbType | 1,
        IdentifierTypeDateTime2 = IdentifierDbType | 2,
        IdentifierTypeDateTime = IdentifierDbType | 3,
        IdentifierTypeSmallDateTime = IdentifierDbType | 4,
        IdentifierTypeDate = IdentifierDbType | 5,
        IdentifierTypeTime = IdentifierDbType | 6,
        IdentifierTypeFloat = IdentifierDbType | 7,
        IdentifierTypeReal = IdentifierDbType | 8,
        IdentifierTypeDecimal = IdentifierDbType | 9,
        IdentifierTypeMoney = IdentifierDbType | 10,
        IdentifierTypeSmallMoney = IdentifierDbType | 11,
        IdentifierTypeBigInt = IdentifierDbType | 12,
        IdentifierTypeInt = IdentifierDbType | 13,
        IdentifierTypeSmallInt = IdentifierDbType | 14,
        IdentifierTypeTinyInt = IdentifierDbType | 15,
        IdentifierTypeBit = IdentifierDbType | 16,
        IdentifierTypeNText = IdentifierDbType | 17,
        IdentifierTypeText = IdentifierDbType | 18,
        IdentifierTypeImage = IdentifierDbType | 19,
        IdentifierTypeTimestamp = IdentifierDbType | 20,
        IdentifierTypeUniqueIdentifier = IdentifierDbType | 21,
        IdentifierTypeNVarChar = IdentifierDbType | 22,
        IdentifierTypeNChar = IdentifierDbType | 23,
        IdentifierTypeVarChar = IdentifierDbType | 24,
        IdentifierTypeChar = IdentifierDbType | 25,
        IdentifierTypeVarBinary = IdentifierDbType | 26,
        IdentifierTypeBinary = IdentifierDbType | 27,
        IdentifierTypeVariant = IdentifierDbType | 28,
        #endregion

        #endregion

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
