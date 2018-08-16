using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests.Service.StObj
{
    public class TestsBase
    {
        class TypeFilter : IStObjTypeFilter
        {
            readonly Func<Type, bool> _typeFilter;

            public TypeFilter( Func<Type, bool> typeFilter )
            {
                _typeFilter = typeFilter;
            }

            bool IStObjTypeFilter.TypeFilter( IActivityMonitor monitor, Type t )
            {
               return _typeFilter.Invoke( t );
            }
        }

        public static StObjCollector CreateStObjCollector( Func<Type, bool> typeFilter = null )
        {
            return new StObjCollector(
                        TestHelper.Monitor,
                        new SimpleServiceContainer(),
                        typeFilter: typeFilter != null ? new TypeFilter( typeFilter ) : null );
        }

        public static StObjCollectorResult CheckSuccess( StObjCollector c )
        {
            c.RegisteringFatalOrErrorCount.Should().Be( 0, "There must be no registration error (AmbientTypeCollector must be successful)." );
            var r = c.GetResult();
            r.HasFatalError.Should().Be( false, "There must be no error." );
            return r;
        }

        public static StObjCollectorResult CheckFailure( StObjCollector c )
        {
            if( c.RegisteringFatalOrErrorCount != 0 )
            {
                TestHelper.Monitor.Error( "Registration error (AmbientTypeCollector)." );
                return null;
            }
            var r = c.GetResult();
            r.HasFatalError.Should().Be( true, "There must be at least one fatal error." );
            return r;
        }

    }
}
