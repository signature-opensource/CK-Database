using System;

namespace CK.Testing
{
    /// <summary>
    /// Mixin that supports DBSetup based on <see cref="ICKSetupTestHelper"/>.
    /// </summary>
    public interface IDBSetupTestHelper : IMixinTestHelper, ISqlServerTestHelper, ICKSetupTestHelper, IStObjMapTestHelper, DBSetup.IDBSetupTestHelperCore
    {
    }
}
