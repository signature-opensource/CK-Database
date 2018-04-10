using System;
using CK.Setup;

namespace CK.Testing.SetupableSetup
{
    /// <summary>
    /// Hooks the <see cref="StObjSetup.IStObjSetupTestHelperCore.StObjSetupRunning"/> to add
    /// the <see cref="SetupableAspectConfiguration"/>.
    /// <para>
    /// Note that this interface does not add any properties or methods to the TestHelper, the advanced options
    /// <see cref="SetupableAspectConfiguration.RevertOrderingNames"/>, <see cref="SetupableAspectConfiguration.TraceDependencySorterInput"/>
    /// and <see cref="SetupableAspectConfiguration.TraceDependencySorterOutput"/> are copied from <see cref="StObjSetup.IStObjSetupTestHelperCore"/>.
    /// </para>
    /// </summary>
    public interface ISetupableSetupTestHelperCore
    {
    }
}
