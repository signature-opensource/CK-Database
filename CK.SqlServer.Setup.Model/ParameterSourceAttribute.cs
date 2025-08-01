using System;

namespace CK.Core;

// Cris marker to allow ParameterSourceAttribute to not necessarily be
// the IPoco closure of the IPoco command family.
internal interface IAllowUnclosedCommandAttribute { }

/// <summary>
/// Decorates parameters of call methods to indicate that the properties of the parameter object 
/// must be used to obtain actual parameter values.
/// </summary>
[AttributeUsage( AttributeTargets.Parameter )]
public class ParameterSourceAttribute : Attribute, IAllowUnclosedCommandAttribute
{
}
