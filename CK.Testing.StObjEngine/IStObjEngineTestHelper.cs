
namespace CK.Testing
{
    /// <summary>
    /// Mixin based on <see cref="IStObjSetupTestHelper"/>.
    /// See <see cref="SetupableSetup.ISetupableSetupTestHelperCore"/>.
    /// </summary>
    public interface IStObjEngineTestHelper : IMixinTestHelper, IMonitorTestHelper, StObjEngine.IStObjEngineTestHelperCore
    {
    }
}
