using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class AmbientTypeKindDetectorTests
    {
        class Nop { }

        class Obj : IAmbientObject { }

        class Serv : IAmbientService { }

        class Scoped : IScopedAmbientService { }

        class Singleton : ISingletonAmbientService { }

        [Test]
        public void basic_type_detection()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( Nop ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( Obj ) ).Should().Be( AmbientTypeKind.AmbientObject );
            a.GetKind( TestHelper.Monitor, typeof( Serv ) ).Should().Be( AmbientTypeKind.IsAmbientService );
            a.GetKind( TestHelper.Monitor, typeof( Scoped ) ).Should().Be( AmbientTypeKind.AmbientScope );
            a.GetKind( TestHelper.Monitor, typeof( Singleton ) ).Should().Be( AmbientTypeKind.AmbientSingleton );
        }

        class SpecObj : Obj { }
        class SpecServ : Serv { }
        class SpecScoped : Scoped { }
        class SpecSingleton : Singleton { }

        [Test]
        public void specialized_type_detection()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( SpecObj ) ).Should().Be( AmbientTypeKind.AmbientObject );
            a.GetKind( TestHelper.Monitor, typeof( SpecServ ) ).Should().Be( AmbientTypeKind.IsAmbientService );
            a.GetKind( TestHelper.Monitor, typeof( SpecScoped ) ).Should().Be( AmbientTypeKind.AmbientScope );
            a.GetKind( TestHelper.Monitor, typeof( SpecSingleton ) ).Should().Be( AmbientTypeKind.AmbientSingleton );
        }

        [AmbientDefiner] class ObjDefiner : IAmbientObject { }
        [AmbientDefiner] class ServDefiner : IAmbientService { }
        [AmbientDefiner] class ScopedDefiner : IScopedAmbientService { }
        [AmbientDefiner] class SingletonDefiner : ISingletonAmbientService { }

        [Test]
        public void Definers_are_marked_with_IAmbientDefiner_and_are_not_ambient()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( ObjDefiner ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( ServDefiner ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( ScopedDefiner ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( SingletonDefiner ) ).Should().Be( AmbientTypeKind.None );
        }

        class SpecObjDefiner : ObjDefiner { }
        class SpecServDefiner : ServDefiner { }
        class SpecScopedDefiner : ScopedDefiner { }
        class SpecSingletonDefiner : SingletonDefiner { }

        [Test]
        public void specialization_of_Definers_are_ambient()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( SpecObjDefiner ) ).Should().Be( AmbientTypeKind.AmbientObject );
            a.GetKind( TestHelper.Monitor, typeof( SpecServDefiner ) ).Should().Be( AmbientTypeKind.IsAmbientService );
            a.GetKind( TestHelper.Monitor, typeof( SpecScopedDefiner ) ).Should().Be( AmbientTypeKind.AmbientScope );
            a.GetKind( TestHelper.Monitor, typeof( SpecSingletonDefiner ) ).Should().Be( AmbientTypeKind.AmbientSingleton );
        }


        [AmbientDefiner] class ObjDefinerLevel2 : ObjDefiner { }
        [AmbientDefiner] class ServDefinerLevel2 : ServDefiner { }
        [AmbientDefiner] class ScopedDefinerLevel2 : ScopedDefiner { }
        [AmbientDefiner] class SingletonDefinerLevel2 : SingletonDefiner { }

        [Test]
        public void Definers_can_be_specialized_as_another_layer_of_Definers_and_are_still_not_ambient()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( ObjDefinerLevel2 ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( ServDefinerLevel2 ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( ScopedDefinerLevel2 ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( TestHelper.Monitor, typeof( SingletonDefinerLevel2 ) ).Should().Be( AmbientTypeKind.None );
        }

        class SpecObjDefinerLevel2 : ObjDefinerLevel2 { }
        class SpecServDefinerLevel2 : ServDefinerLevel2 { }
        class SpecScopedDefinerLevel2 : ScopedDefinerLevel2 { }
        class SpecSingletonDefinerLevel2 : SingletonDefinerLevel2 { }

        [Test]
        public void specialization_of_DefinersLevel2_are_ambient()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( SpecObjDefinerLevel2 ) ).Should().Be( AmbientTypeKind.AmbientObject );
            a.GetKind( TestHelper.Monitor, typeof( SpecServDefinerLevel2 ) ).Should().Be( AmbientTypeKind.IsAmbientService );
            a.GetKind( TestHelper.Monitor, typeof( SpecScopedDefinerLevel2 ) ).Should().Be( AmbientTypeKind.AmbientScope );
            a.GetKind( TestHelper.Monitor, typeof( SpecSingletonDefinerLevel2 ) ).Should().Be( AmbientTypeKind.AmbientSingleton );
        }

        interface INotPossible0 : IAmbientService, IAmbientObject { }
        interface INotPossible1 : IScopedAmbientService, ISingletonAmbientService { }

        class NotPossible0 : ObjDefinerLevel2, IAmbientService { }
        class NotPossible1 : ScopedDefiner, IAmbientObject { }


        [Test]
        public void conflict_detection()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( TestHelper.Monitor, typeof( INotPossible0 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( TestHelper.Monitor, typeof( INotPossible1 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( TestHelper.Monitor, typeof( NotPossible0 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( TestHelper.Monitor, typeof( NotPossible1 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
        }


    }
}
