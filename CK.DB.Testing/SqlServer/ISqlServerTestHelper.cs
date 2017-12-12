using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Testing
{
    /// <summary>
    /// Combines <see cref="ISqlServerTestHelperCore"/> and <see cref="IMonitorTestHelper"/>.
    /// </summary>
    public interface ISqlServerTestHelper : ISqlServerTestHelperCore, IMonitorTestHelper
    {
    }
}
