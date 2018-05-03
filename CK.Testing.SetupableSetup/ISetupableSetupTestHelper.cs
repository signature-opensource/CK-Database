using System;

namespace CK.Testing
{
    /// <summary>
    /// Mixin based on <see cref="IStObjSetupTestHelper"/>.
    /// See <see cref="SetupableSetup.ISetupableSetupTestHelperCore"/>.
    /// </summary>
    public interface ISetupableSetupTestHelper : IMixinTestHelper, IStObjSetupTestHelper, SetupableSetup.ISetupableSetupTestHelperCore
    {
    }
}
