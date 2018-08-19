using CK.Core;
using CK.Setup;
using CK.Text;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static (StObjCollectorResult,IStObjMap) CheckSuccessAndEmit( StObjCollector c )
        {
            var r = CheckSuccess( c );
            var assemblyName = DateTime.Now.ToString( "Service_yyMdHmsf" );
            var assemblyPath = Path.Combine( AppContext.BaseDirectory, assemblyName + ".dll" );
            var codeGen = r.GenerateFinalAssembly( TestHelper.Monitor, assemblyPath, true, null );
            codeGen.Success.Should().BeTrue( "CodeGeneration should work." );
            var a = TestHelper.LoadAssemblyFromAppContextBaseDirectory( assemblyName );
            return (r, StObjContextRoot.Load( a, null, TestHelper.Monitor ) );
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
