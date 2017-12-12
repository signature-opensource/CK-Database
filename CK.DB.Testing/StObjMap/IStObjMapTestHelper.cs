using CK.Core;
using System;

namespace CK.Testing
{
    /// <summary>
    /// Combines <see cref="IStObjMapTestHelperCore"/> and <see cref="IMonitorTestHelper"/>.
    /// </summary>
    public interface IStObjMapTestHelper : IStObjMapTestHelperCore, IMonitorTestHelper
    {
    }
}
