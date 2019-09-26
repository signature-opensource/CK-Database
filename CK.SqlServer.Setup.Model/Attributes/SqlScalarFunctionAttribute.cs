namespace CK.Core
{

    /// <summary>
    /// Declares a scalar function.
    /// Signature is checked and code required to call it is generated.
    /// </summary>
    public class SqlScalarFunctionAttribute : Setup.SqlCallableAttributeBase
    {
        /// <summary>
        /// Initializes a new <see cref="SqlScalarFunctionAttribute"/>.
        /// </summary>
        /// <param name="functionName">
        /// Name of the scalar function. May start with "transform:" to declare a transformer
        /// of the already existing function and "replace:" to fully override the existing definition.
        /// </param>
        public SqlScalarFunctionAttribute( string functionName )
            : base( functionName, "Function" )
        {
        }
    }
}
