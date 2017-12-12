using CK.Core;
using System;

namespace CK.Testing
{
    /// <summary>
    /// Combines <see cref="IMonitorTestHelperCore"/> and <see cref="IBasicTestHelper"/>.
    /// </summary>
    public interface IMonitorTestHelper : IMonitorTestHelperCore, IBasicTestHelper
    {
    }
}
