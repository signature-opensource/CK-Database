using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Marker interface that extends <see cref="IAttributeAmbientContextBound"/> in order to 
    /// be initialized with the <see cref="MemberInfo"/> that is decorated with the attribute.
    /// </summary>
    public interface IAttributeAmbientContextBoundInitializer : IAttributeAmbientContextBound
    {
        /// <summary>
        /// Called the first time the attribute is obtained.
        /// </summary>
        /// <param name="owner">The <see cref="ICKCustomAttributeTypeMultiProvider"/> that gives access to all the types' attributes.</param>
        /// <param name="m">The member that is decorated by this attribute.</param>
        void Initialize( ICKCustomAttributeTypeMultiProvider owner, MemberInfo m );
    }
    
}
