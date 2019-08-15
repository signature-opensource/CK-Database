using System;


namespace CK.Setup
{
    /// <summary>
    /// Marks an assembly as being a a Model.
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    public class IsModelAttribute : Attribute
    {
    }
}
