namespace CK.Core;

/// <summary>
/// Declares a stored procedure.
/// Signature is checked and code required to call it is generated.
/// </summary>
public class SqlProcedureAttribute : Setup.SqlCallableAttributeBase
{
    /// <summary>
    /// Initializes a new <see cref="SqlProcedureAttribute"/>.
    /// </summary>
    /// <param name="procedureName">
    /// Name of the procedure. May start with "transform:" to declare a transformer
    /// of the already existing procedure and "replace:" to fully override the existing definition.
    /// </param>
    public SqlProcedureAttribute( string procedureName )
        : base( procedureName, "Procedure" )
    {
    }
}
