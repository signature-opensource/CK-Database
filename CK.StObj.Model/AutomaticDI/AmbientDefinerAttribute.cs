using System;

namespace CK.Core
{
    /// <summary>
    /// Attribute that marks a type as being a "Definer" of an Ambient type (<see cref="IRealObject"/> or <see cref="IAutoService"/>).
    /// This defines the decorated object as a "base type" of the ambient type: it is not itself an ambient type but
    /// its specializations are (unless they also are decorated with this attribute).
    /// </summary>
    /// <para>
    /// This attribute, just like <see cref="IRealObject"/>, <see cref="IAutoService"/>, <see cref="IScopedAutoService"/>
    /// and <see cref="ISingletonAutoService"/> can be created anywhere: as long as the name is "AmbientDefinerAttribute" 
    /// (regardless of the namespace), it will be honored.
    /// </para>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false )]
    public class AmbientDefinerAttribute : Attribute
    {
    }

}
