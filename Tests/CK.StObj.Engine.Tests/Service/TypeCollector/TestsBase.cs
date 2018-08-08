using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            r.HasFatalError.Should().Be( false );
            r.LogErrorAndWarnings( TestHelper.Monitor );
            return r;
        }

        public static AmbientTypeCollectorResult CheckFailure( AmbientTypeCollector c )
        {
            var r = c.GetResult();
            r.HasFatalError.Should().Be( true );
            r.LogErrorAndWarnings( TestHelper.Monitor );
            return r;
        }

    }
}
