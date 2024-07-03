using System.Linq;
using CK.Setup;
using CK.Testing.SetupableSetup;

namespace CK.Testing
{
    /// <summary>
    /// Standard implementation of <see cref="ISetupableSetupTestHelperCore"/>.
    /// Just adds <see cref="SetupableAspectConfiguration"/> on <see cref="StObjSetup.IStObjSetupTestHelperCore.StObjSetupRunning"/>.
    /// </summary>
    public class SetupableSetupTestHelper : ISetupableSetupTestHelperCore
    {
        readonly IStObjSetupTestHelper _stObjSetup;

        internal SetupableSetupTestHelper( TestHelperConfiguration config, IStObjSetupTestHelper stObjSetup )
        {
            _stObjSetup = stObjSetup;
            _stObjSetup.StObjSetupRunning += OnStObjSetupRunning;
        }

        void OnStObjSetupRunning( object sender, StObjSetup.StObjSetupRunningEventArgs e )
        {
            if( !e.EngineConfiguration.Aspects.Any( c => c is SetupableAspectConfiguration ) )
            {
                SetupableAspectConfiguration conf = new SetupableAspectConfiguration();
                conf.RevertOrderingNames = _stObjSetup.StObjRevertOrderingNames;
                conf.TraceDependencySorterInput = conf.TraceDependencySorterInput = _stObjSetup.StObjTraceGraphOrdering;
                e.EngineConfiguration.AddAspect( conf );
            }
        }

        /// <summary>
        /// Gets the <see cref="ISetupableSetupTestHelper"/> default implementation.
        /// </summary>
        public static ISetupableSetupTestHelper TestHelper => TestHelperResolver.Default.Resolve<ISetupableSetupTestHelper>();

    }
}
