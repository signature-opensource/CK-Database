using CK.Core;
using CK.Setup;
using FluentAssertions;
using System;
using System.Collections.Generic;

using static CK.Testing.MonitorTestHelper;

namespace CK.StObj.Engine.Tests.Service.TypeCollector
{
    public class TestsBase
    {

        public static AmbientTypeCollector CreateAmbientTypeCollector( Func<Type, bool> typeFilter = null )
        {
            Func<IActivityMonitor, Type, bool> f = null;
            if( typeFilter != null ) f = ( m, t ) => typeFilter( t );
            return new AmbientTypeCollector(
                        TestHelper.Monitor,
                        new SimpleServiceContainer(),
                        new DynamicAssembly( new Dictionary<string, object>() ),
                        f );
        }

        public static AmbientTypeCollectorResult CheckSuccess( AmbientTypeCollector c )
        {
            var r = c.GetResult();
            r.LogErrorAndWarnings( TestHelper.Monitor );
            r.HasFatalError.Should().Be( false, "There must be no error." );
            return r;
        }

        public static AmbientTypeCollectorResult CheckFailure( AmbientTypeCollector c )
        {
            var r = c.GetResult();
            r.LogErrorAndWarnings( TestHelper.Monitor );
            r.HasFatalError.Should().Be( true, "There must be at least one fatal error." );
            return r;
        }

    }
}
