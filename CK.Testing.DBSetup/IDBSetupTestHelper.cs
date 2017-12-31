using System;

namespace CK.Testing
{
    public interface IDBSetupTestHelper : IMixinTestHelper, ICKSetupTestHelper, IStObjMapTestHelper, DBSetup.IDBSetupTestHelperCore
    {
    }
}
