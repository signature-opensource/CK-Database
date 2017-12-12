using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Testing
{
    /// <summary>
    /// Combines <see cref="ICKSqlServerTestHelperCore"/> and <see cref="ISqlServerTestHelper"/>.
    /// </summary>
    public interface ICKSqlServerTestHelper : ITestHelper, ICKSqlServerTestHelperCore, ISqlServerTestHelper
    {
    }
}
