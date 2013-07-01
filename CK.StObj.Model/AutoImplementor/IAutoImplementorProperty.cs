using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace CK.Core
{
    /// <summary>
    /// Classes that implement this interface are able to implement a property.
    /// </summary>
    public interface IAutoImplementorProperty
    {
        /// <summary>
        /// Implements the given property on the given <see cref="TypeBuilder"/>.
        /// Implementations can rely on the <paramref name="dynamicAssemblyMemory"/> to store shared information if needed.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="p">The property to implement.</param>
        /// <param name="dynamicAssembly">Dynamic assembly being implemented.</param>
        /// <param name="b">The type builder to use.</param>
        /// <param name="isVirtual">True if a virtual property must be implemented. False if it must be sealed.</param>
        /// <returns>
        /// True if the property is actually implemented, false if, for any reason, anpther implementation (empty for instance) must be generated 
        /// (for instance, whenever the property is not ready to be implemented). Any error must be logged into the <paramref name="logger"/>.
        /// </returns>
        bool Implement( IActivityLogger logger, PropertyInfo p, IDynamicAssembly dynamicAssembly, TypeBuilder b, bool isVirtual );
    }

}
