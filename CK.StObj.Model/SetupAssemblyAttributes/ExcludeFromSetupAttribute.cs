using System;


namespace CK.Setup
{

    /// <summary>
    /// Marks an assembly that even if it depends on Models should not participate
    /// in Setup. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    public class ExcludeFromSetupAttribute : Attribute
    {
    }
}
