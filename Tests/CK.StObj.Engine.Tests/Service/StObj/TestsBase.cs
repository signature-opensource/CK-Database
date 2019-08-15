using CK.Core;
using CK.Setup;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

using static CK.Testing.MonitorTestHelper;

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

        public static StObjCollector CreateStObjCollector( params Type[] types )
        {
            var c = CreateStObjCollector();
            c.RegisterTypes( types );
            return c;
        }

        public static StObjCollectorResult CheckSuccess( StObjCollector c )
        {
            c.RegisteringFatalOrErrorCount.Should().Be( 0, "There must be no registration error (AmbientTypeCollector must be successful)." );
            var r = c.GetResult();
            r.HasFatalError.Should().Be( false, "There must be no error." );
            return r;
        }

        public static (StObjCollectorResult Result, IStObjMap Map) CheckSuccessAndEmit( StObjCollector c )
        {
            var r = CheckSuccess( c );
            var assemblyName = DateTime.Now.ToString( "Service_yyMdHmsffff" );
            var assemblyPath = Path.Combine( AppContext.BaseDirectory, assemblyName + ".dll" );
            var codeGen = r.GenerateFinalAssembly( TestHelper.Monitor, assemblyPath, true, null, false );
            codeGen.Success.Should().BeTrue( "CodeGeneration should work." );
            var a = Assembly.Load( new AssemblyName( assemblyName ) );
            return (r, StObjContextRoot.Load( a, null, TestHelper.Monitor ));
        }

        public static (StObjCollectorResult Result, IStObjMap Map, StObjContextRoot.ServiceRegister ServiceRegisterer) CheckSuccessAndConfigureServices( StObjCollector c, SimpleServiceContainer startupServices = null )
        {
            var r = CheckSuccessAndEmit( c );
            r.Map.Should().NotBeNull();
            var reg = new StObjContextRoot.ServiceRegister( TestHelper.Monitor, new ServiceCollection(), startupServices );
            reg.AddStObjMap( r.Map ).Should().BeTrue( "Service configuration succeed." );
            return (r.Result, r.Map, reg);
        }

        public static (StObjCollectorResult Result, IStObjMap Map, IServiceProvider Services) CheckSuccessAndBuildServices( StObjCollector c, SimpleServiceContainer startupServices = null )
        {
            var r = CheckSuccessAndConfigureServices( c, startupServices );
            return (r.Result, r.Map, r.ServiceRegisterer.Services.BuildServiceProvider());
        }

        public static StObjContextRoot.ServiceRegister CheckFailureConfigurationServices( StObjCollector c, SimpleServiceContainer startupServices = null )
        {
            var r = CheckSuccessAndEmit( c );
            r.Item2.Should().NotBeNull();
            var reg = new StObjContextRoot.ServiceRegister( TestHelper.Monitor, new ServiceCollection(), startupServices );
            reg.AddStObjMap( r.Item2 ).Should().BeFalse( "Service configuration failed." );
            return reg;
        }

        public static StObjCollectorResult CheckFailure( StObjCollector c )
        {
            if( c.RegisteringFatalOrErrorCount != 0 )
            {
                TestHelper.Monitor.Error( $"CheckFailure: {c.RegisteringFatalOrErrorCount} fatal or error during registration." );
                return null;
            }
            var r = c.GetResult();
            r.HasFatalError.Should().Be( true, "CheckFailure: There must be at least one fatal error." );
            return r;
        }

    }
}
