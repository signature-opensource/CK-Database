namespace SqlCallDemo.ComplexType;

/// <summary>
/// This type is used as a <see cref="CK.Core.ParameterSourceAttribute"/>.
/// Its properties' type must exactly match the one of Sql parameter.
/// <para>
/// TODO: just like for <see cref="OutputTypeCastWithDefault.ParamTinyInt"/>, we could
/// support here less strict type BUT in the other direction since these properties' value will
/// be set on the Sql parameters, they must be "smaller" that the Sql (whereas on the OutputTypeCastWithDefault,
/// the properties' type must be "bigger" then the Sql).
/// </para>
/// <para>
/// Since the fact that this type is used as an input (ParameterSource - contravariant) or as an output
/// (returned type - covariant) or both (no variance at all) is call site dependent, this must be handled
/// independently for each methods.
/// </para>
/// </summary>
public class InputTypeCastWithDefault
{
    public int ParamInt { get; set; }

    public short ParamSmallInt { get; set; }

    /// <summary>
    /// Exposing an "int" here is an error.
    /// To support this, we must handle chain of casts: have a look at
    /// ISqlServerExtensions extensions IsTypeCompatible and SafeAssignableCastChain
    /// in CK.SqlServer.Setup.Runtime. This is a TODO.
    /// </summary>
    public byte ParamTinyInt { get; set; }

    public string Result { get; set; }
}
