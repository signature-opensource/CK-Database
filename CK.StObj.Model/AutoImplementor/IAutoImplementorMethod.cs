using System.Reflection;
using System.Reflection.Emit;

namespace CK.Core
{
    /// <summary>
    /// Classes that implement this interface are able to implement a method.
    /// </summary>
    public interface IAutoImplementorMethod
    {
        /// <summary>
        /// Implements the given method on the given <see cref="TypeBuilder"/>.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="m">The method to implement.</param>
        /// <param name="b">The type builder to use.</param>
        /// <param name="isVirtual">True if a virtual method must be implemented. False if it must be sealed.</param>
        /// <returns>
        /// True if the method is actually implemented, false if, for any reason, another implementation (empty for instance) must be generated 
        /// (for instance, whenver the method is not ready to be implemented). Any error must be logged into the <paramref name="logger"/>.
        /// </returns>
        bool Implement( IActivityLogger logger, MethodInfo m, TypeBuilder b, bool isVirtual );
    }

}
