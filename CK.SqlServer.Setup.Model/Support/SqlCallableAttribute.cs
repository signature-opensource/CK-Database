using CK.Core;
using CK.SqlServer;
using System;

namespace CK.Setup;


/// <summary>
/// Abstract base class to define callable objects (stored procedure and functions).
/// </summary>
[AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
public abstract class SqlCallableAttributeBase : SetupObjectItemMemberAttributeBase
{
    /// <summary>
    /// Initializes a new <see cref="SqlCallableAttributeBase"/>.
    /// </summary>
    /// <param name="callableName">The object name.</param>
    /// <param name="objectType">The object type ("Procedure", "Function").</param>
    /// <param name="noCall">True to not generate call support code.</param>
    protected SqlCallableAttributeBase( string callableName, string objectType, bool noCall = false )
        : base( callableName, "CK.SqlServer.Setup.SqlCallableAttributeImpl, CK.SqlServer.Setup.Runtime" )
    {
        ObjectType = objectType;
        NoCall = noCall;
    }

    /// <summary>
    /// Gets or sets whether call must be supported or not.
    /// Defaults to false: specialized attributes like <see cref="SqlProcedureNoExecuteAttribute"/> sets it to true.
    /// </summary>
    public bool NoCall { get; set; }

    /// <summary>
    /// Gets or sets the object type.
    /// </summary>
    public string ObjectType { get; set; }

    /// <summary>
    /// Gets or sets the time out in seconds of the command execution.
    /// Defaults to 30 seconds (this is the same as the hard-coded default value
    /// of the <see cref="Microsoft.Data.SqlClient.SqlCommand.CommandTimeout"/>).
    /// A value of 0 indicates no limit.
    /// Note that the centralized <see cref="SqlHelper.CommandTimeoutFactor"/> applies to this configuration.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
