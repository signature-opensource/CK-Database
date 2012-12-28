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
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="p">The property to implement.</param>
        /// <param name="b">The type builder to use.</param>
        /// <param name="isVirtual">True to implement a virtual property. False to seal it.</param>
        /// <returns>False to indicate that an error occured and that continuing to generate type is useless.</returns>
        bool Implement( IActivityLogger logger, PropertyInfo p, TypeBuilder b, bool isVirtual );
    }

}
