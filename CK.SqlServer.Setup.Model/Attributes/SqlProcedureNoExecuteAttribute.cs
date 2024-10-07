using System;

namespace CK.Core;


/// <summary>
/// Allows to declare a procedure signature and check the parameters 
/// without code generation support to call it.
/// </summary>
[AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
public class SqlProcedureNoExecuteAttribute : Setup.SqlCallableAttributeBase
{
    /// <summary>
    /// Initializes a new <see cref="SqlProcedureNoExecuteAttribute"/>.
    /// </summary>
    /// <param name="procedureName">
    /// Name of the procedure. May start with "transform:" to declare a transformer
    /// of the already existing procedure and "replace:" to fully override the existing definition.
    /// </param>
    public SqlProcedureNoExecuteAttribute( string procedureName )
        : base( procedureName, "Procedure", noCall: true )
    {
    }

}
