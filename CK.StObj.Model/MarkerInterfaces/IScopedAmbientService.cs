using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface marker states that a class or an interface instance
    /// must be a unique Service in a scope.
    /// <para>
    /// It is not required to be this exact type: any empty interface (no members)
    /// named "IScopedAmbientService" defined in any namespace will be considered as
    /// a valid marker, regardless of the fact that it specializes any interface
    /// named "IAmbientService".
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that even if an implementation only relies on other singletons or contracts,
    /// this interface forces the service to be scoped.
    /// </para>
    /// <para>
    /// If there is no specific constraint, the <see cref="IAmbientService"/> marker
    /// should be used so that its scoped vs. singleton lifetime is either determined
    /// by the final implementation or automatically detected based on its constructor
    /// dependencies.
    /// </para>
    /// </remarks>
    public interface IScopedAmbientService : IAmbientService
    {
    }

}
