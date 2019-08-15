using System;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Decorates parameters of call methods to indicate that the properties of the parameter object 
    /// must be used to obtain actual parameter values.
    /// </summary>
    [AttributeUsage( AttributeTargets.Parameter )]
    public class ParameterSourceAttribute : Attribute
    {
    }
}
