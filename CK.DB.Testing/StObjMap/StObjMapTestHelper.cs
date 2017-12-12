using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CK.Core;
using CK.Text;

namespace CK.Testing
{

    /// <summary>
    /// Provides default implementation of <see cref="IStObjMapTestHelperCore"/>.
    /// </summary>
    public class StObjMapTestHelper : IStObjMapTestHelperCore
    {
        readonly ITestHelperConfiguration _config;
        readonly IMonitorTestHelperCore _monitor;
        readonly string _generatedAssemblyName;
        IStObjMap _map;

        public StObjMapTestHelper( ITestHelperConfiguration config, IMonitorTestHelperCore monitor )
        {
            _config = config;
            _monitor = monitor;
            _generatedAssemblyName = _config.Get( "StObjMap/GeneratedAssemblyName", StObjEngineConfiguration.DefaultGeneratedAssemblyName );
        }


        string IStObjMapTestHelperCore.GeneratedAssemblyName => _generatedAssemblyName;

        IStObjMap IStObjMapTestHelperCore.StObjMap
        {
            get
            {
                return _map ?? (_map = StObjMapTestHelperExtensions.DoLoadStObjMap( _monitor, _generatedAssemblyName ) );
            }
        }
        /// <summary>
        /// Gets the <see cref="IStObjMapTestHelper"/> default implementation.
        /// </summary>
        public static IStObjMapTestHelper TestHelper { get; } = TestHelperResolver.Default.Resolve<IStObjMapTestHelper>();

    }
}
