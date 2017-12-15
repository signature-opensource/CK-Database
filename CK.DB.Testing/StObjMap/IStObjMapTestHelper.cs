using CK.Core;
using System;

namespace CK.Testing
{
    /// <summary>
    /// Mixin of <see cref="IStObjMapTestHelperCore"/> and <see cref="IMonitorTestHelper"/>.
    /// </summary>
    public interface IStObjMapTestHelper : IMixinTestHelper, IStObjMapTestHelperCore, IMonitorTestHelper
    {
    }
}
