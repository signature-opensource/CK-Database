using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Testing
{
    /// <summary>
    /// Mixin of <see cref="ICKSqlServerTestHelperCore"/> and <see cref="ISqlServerTestHelper"/>.
    /// </summary>
    public interface ICKSqlServerTestHelper : IMixinTestHelper, ICKSqlServerTestHelperCore, ISqlServerTestHelper
    {
    }
}
