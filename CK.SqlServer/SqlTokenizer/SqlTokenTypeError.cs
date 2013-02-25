using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public enum SqlTokenTypeError
    {
        None = 0,

        /// <summary>
        /// Sign bit (bit n°31) is 1 to indicate an error or the end of the input.
        /// This allows easy and efficient error/end test: any negative token value marks the end.
        /// </summary>
        IsErrorOrEndOfInput = -2147483648,

        /// <summary>
        /// The end of input has only the most significant bit set.
        /// </summary>
        EndOfInput = IsErrorOrEndOfInput,

        /// <summary>
        /// Error bit (all kind of errors).
        /// </summary>
        IsError = 1 << 30,

        /// <summary>
        /// Error mask for any errors: all kind of errors have these 2 bits set.
        /// </summary>
        ErrorMask = IsErrorOrEndOfInput | IsError,

        /// <summary>
        /// Error mask for errors raised at the <see cref="SqlTokenizer"/> level: the 3 bits - n°29, 30 &amp; 31 - are set.
        /// </summary>
        ErrorTokenizerMask = IsErrorOrEndOfInput | IsError | (1 << 29),

        /// <summary>
        /// Invalid character.
        /// </summary>
        ErrorInvalidChar = ErrorTokenizerMask | 1,

        /// <summary>
        /// Whenever a non terminated string is encountered.
        /// </summary>
        ErrorStringUnterminated = ErrorTokenizerMask | 2,

        /// <summary>
        /// Whenever a non terminated identifier is encountered.
        /// </summary>
        ErrorIdentifierUnterminated = ErrorTokenizerMask | 3,

        /// <summary>
        /// Unterminated number.
        /// </summary>
        ErrorNumberUnterminatedValue = ErrorTokenizerMask | 4,

        /// <summary>
        /// Invalid number value.
        /// </summary>
        ErrorNumberValue = ErrorTokenizerMask | 5,

        /// <summary>
        /// Number value is immediately followed by an identifier: 45D for example.
        /// </summary>
        ErrorNumberIdentifierStartsImmediately = ErrorTokenizerMask | 6,

    }
}
