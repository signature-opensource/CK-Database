using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;

using static CK.Testing.MonitorTestHelper;

namespace CK.StObj.Engine.Tests.Service.TypeCollector
{
    [TestFixture]
    public class AmbientTypeKindDetectorTests
    {
        class Nop { }

        class Obj : IRealObject { }

        class Serv : IAutoService { }

        class Scoped : IScopedAutoService { }

        class Singleton : ISingletonAutoService { }

        [Test]
        public void basic_type_detection()
        {
            var a = new AutoRealTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( Nop ) ).Should().Be( AutoRealTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( Obj ) ).Should().Be( AutoRealTypeKind.RealObject );
            a.GetKind( TestHelper.Monitor, typeof( Serv ) ).Should().Be( AutoRealTypeKind.IsAutoService );
            a.GetKind( TestHelper.Monitor, typeof( Scoped ) ).Should().Be( AutoRealTypeKind.AutoScoped );
            a.GetKind( TestHelper.Monitor, typeof( Singleton ) ).Should().Be( AutoRealTypeKind.AutoSingleton );
        }

        class SpecObj : Obj { }
        class SpecServ : Serv { }
        class SpecScoped : Scoped { }
        class SpecSingleton : Singleton { }

        [Test]
        public void specialized_type_detection()
        {
            var a = new AutoRealTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( SpecObj ) ).Should().Be( AutoRealTypeKind.RealObject );
            a.GetKind( TestHelper.Monitor, typeof( SpecServ ) ).Should().Be( AutoRealTypeKind.IsAutoService );
            a.GetKind( TestHelper.Monitor, typeof( SpecScoped ) ).Should().Be( AutoRealTypeKind.AutoScoped );
            a.GetKind( TestHelper.Monitor, typeof( SpecSingleton ) ).Should().Be( AutoRealTypeKind.AutoSingleton );
        }

        [AmbientDefiner] class ObjDefiner : IRealObject { }
        [AmbientDefiner] class ServDefiner : IAutoService { }
        [AmbientDefiner] class ScopedDefiner : IScopedAutoService { }
        [AmbientDefiner] class SingletonDefiner : ISingletonAutoService { }

        [Test]
        public void Definers_are_marked_with_IAmbientDefiner_and_are_not_ambient()
        {
            var a = new AutoRealTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( ObjDefiner ) ).Should().Be( AutoRealTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( ServDefiner ) ).Should().Be( AutoRealTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( ScopedDefiner ) ).Should().Be( AutoRealTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( SingletonDefiner ) ).Should().Be( AutoRealTypeKind.None );
        }

        class SpecObjDefiner : ObjDefiner { }
        class SpecServDefiner : ServDefiner { }
        class SpecScopedDefiner : ScopedDefiner { }
        class SpecSingletonDefiner : SingletonDefiner { }

        [Test]
        public void specialization_of_Definers_are_ambient()
        {
            var a = new AutoRealTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( SpecObjDefiner ) ).Should().Be( AutoRealTypeKind.RealObject );
            a.GetKind( TestHelper.Monitor, typeof( SpecServDefiner ) ).Should().Be( AutoRealTypeKind.IsAutoService );
            a.GetKind( TestHelper.Monitor, typeof( SpecScopedDefiner ) ).Should().Be( AutoRealTypeKind.AutoScoped );
            a.GetKind( TestHelper.Monitor, typeof( SpecSingletonDefiner ) ).Should().Be( AutoRealTypeKind.AutoSingleton );
        }


        [AmbientDefiner] class ObjDefinerLevel2 : ObjDefiner { }
        [AmbientDefiner] class ServDefinerLevel2 : ServDefiner { }
        [AmbientDefiner] class ScopedDefinerLevel2 : ScopedDefiner { }
        [AmbientDefiner] class SingletonDefinerLevel2 : SingletonDefiner { }

        [Test]
        public void Definers_can_be_specialized_as_another_layer_of_Definers_and_are_still_not_ambient()
        {
            var a = new AutoRealTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( ObjDefinerLevel2 ) ).Should().Be( AutoRealTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( ServDefinerLevel2 ) ).Should().Be( AutoRealTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( ScopedDefinerLevel2 ) ).Should().Be( AutoRealTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( SingletonDefinerLevel2 ) ).Should().Be( AutoRealTypeKind.None );
        }

        class SpecObjDefinerLevel2 : ObjDefinerLevel2 { }
        class SpecServDefinerLevel2 : ServDefinerLevel2 { }
        class SpecScopedDefinerLevel2 : ScopedDefinerLevel2 { }
        class SpecSingletonDefinerLevel2 : SingletonDefinerLevel2 { }

        [Test]
        public void specialization_of_DefinersLevel2_are_ambient()
        {
            var a = new AutoRealTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( SpecObjDefinerLevel2 ) ).Should().Be( AutoRealTypeKind.RealObject );
            a.GetKind( TestHelper.Monitor, typeof( SpecServDefinerLevel2 ) ).Should().Be( AutoRealTypeKind.IsAutoService );
            a.GetKind( TestHelper.Monitor, typeof( SpecScopedDefinerLevel2 ) ).Should().Be( AutoRealTypeKind.AutoScoped );
            a.GetKind( TestHelper.Monitor, typeof( SpecSingletonDefinerLevel2 ) ).Should().Be( AutoRealTypeKind.AutoSingleton );
        }

        interface INotPossible0 : IAutoService, IRealObject { }
        interface INotPossible1 : IScopedAutoService, ISingletonAutoService { }

        class NotPossible0 : ObjDefinerLevel2, IAutoService { }
        class NotPossible1 : ScopedDefiner, IRealObject { }


        [Test]
        public void conflict_detection()
        {
            var a = new AutoRealTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( INotPossible0 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( TestHelper.Monitor, typeof( INotPossible1 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( TestHelper.Monitor, typeof( NotPossible1 ) ).GetAmbientKindCombinationError().Should().NotBeNull();

            // This is explictly allowed thanks to the parameter.
            a.GetKind( TestHelper.Monitor, typeof( NotPossible0 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( TestHelper.Monitor, typeof( NotPossible0 ) ).GetAmbientKindCombinationError( ambientObjectCanBeSingletonService:true ).Should().BeNull();
        }


    }
}
