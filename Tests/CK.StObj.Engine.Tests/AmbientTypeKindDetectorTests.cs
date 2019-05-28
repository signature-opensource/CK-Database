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
            a.GetKind( typeof( Nop ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( typeof( Obj ) ).Should().Be( AmbientTypeKind.AmbientObject );
            a.GetKind( typeof( Serv ) ).Should().Be( AmbientTypeKind.IsAmbientService );
            a.GetKind( typeof( Scoped ) ).Should().Be( AmbientTypeKind.AmbientScope );
            a.GetKind( typeof( Singleton ) ).Should().Be( AmbientTypeKind.AmbientSingleton );
        }

        class SpecObj : Obj { }
        class SpecServ : Serv { }
        class SpecScoped : Scoped { }
        class SpecSingleton : Singleton { }

        [Test]
        public void specialized_type_detection()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( typeof( SpecObj ) ).Should().Be( AmbientTypeKind.AmbientObject );
            a.GetKind( typeof( SpecServ ) ).Should().Be( AmbientTypeKind.IsAmbientService );
            a.GetKind( typeof( SpecScoped ) ).Should().Be( AmbientTypeKind.AmbientScope );
            a.GetKind( typeof( SpecSingleton ) ).Should().Be( AmbientTypeKind.AmbientSingleton );
        }

        [AmbientDefiner] class ObjDefiner : IAmbientObject { }
        [AmbientDefiner] class ServDefiner : IAmbientService { }
        [AmbientDefiner] class ScopedDefiner : IScopedAmbientService { }
        [AmbientDefiner] class SingletonDefiner : ISingletonAmbientService { }

        [Test]
        public void Definers_are_marked_with_IAmbientDefiner_and_are_not_ambient()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( typeof( ObjDefiner ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( typeof( ServDefiner ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( typeof( ScopedDefiner ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( typeof( SingletonDefiner ) ).Should().Be( AmbientTypeKind.None );
        }

        class SpecObjDefiner : ObjDefiner { }
        class SpecServDefiner : ServDefiner { }
        class SpecScopedDefiner : ScopedDefiner { }
        class SpecSingletonDefiner : SingletonDefiner { }

        [Test]
        public void specialization_of_Definers_are_ambient()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( typeof( SpecObjDefiner ) ).Should().Be( AmbientTypeKind.AmbientObject );
            a.GetKind( typeof( SpecServDefiner ) ).Should().Be( AmbientTypeKind.IsAmbientService );
            a.GetKind( typeof( SpecScopedDefiner ) ).Should().Be( AmbientTypeKind.AmbientScope );
            a.GetKind( typeof( SpecSingletonDefiner ) ).Should().Be( AmbientTypeKind.AmbientSingleton );
        }


        [AmbientDefiner] class ObjDefinerLevel2 : ObjDefiner { }
        [AmbientDefiner] class ServDefinerLevel2 : ServDefiner { }
        [AmbientDefiner] class ScopedDefinerLevel2 : ScopedDefiner { }
        [AmbientDefiner] class SingletonDefinerLevel2 : SingletonDefiner { }

        [Test]
        public void Definers_can_be_specialized_as_another_layer_of_Definers_and_are_still_not_ambient()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( typeof( ObjDefinerLevel2 ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( typeof( ServDefinerLevel2 ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( typeof( ScopedDefinerLevel2 ) ).Should().Be( AmbientTypeKind.None );
            a.GetKind( typeof( SingletonDefinerLevel2 ) ).Should().Be( AmbientTypeKind.None );
        }

        class SpecObjDefinerLevel2 : ObjDefinerLevel2 { }
        class SpecServDefinerLevel2 : ServDefinerLevel2 { }
        class SpecScopedDefinerLevel2 : ScopedDefinerLevel2 { }
        class SpecSingletonDefinerLevel2 : SingletonDefinerLevel2 { }

        [Test]
        public void specialization_of_DefinersLevel2_are_ambient()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( typeof( SpecObjDefinerLevel2 ) ).Should().Be( AmbientTypeKind.AmbientObject );
            a.GetKind( typeof( SpecServDefinerLevel2 ) ).Should().Be( AmbientTypeKind.IsAmbientService );
            a.GetKind( typeof( SpecScopedDefinerLevel2 ) ).Should().Be( AmbientTypeKind.AmbientScope );
            a.GetKind( typeof( SpecSingletonDefinerLevel2 ) ).Should().Be( AmbientTypeKind.AmbientSingleton );
        }

        interface INotPossible0 : IAmbientService, IAmbientObject { }
        interface INotPossible1 : IScopedAmbientService, ISingletonAmbientService { }

        class NotPossible0 : ObjDefinerLevel2, IAmbientService { }
        class NotPossible1 : ScopedDefiner, IAmbientObject { }


        [Test]
        public void conflict_detection()
        {
            var a = new AmbientTypeKindDetector();
            a.GetKind( typeof( INotPossible0 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( typeof( INotPossible1 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( typeof( NotPossible0 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
            a.GetKind( typeof( NotPossible1 ) ).GetAmbientKindCombinationError().Should().NotBeNull();
        }


    }
}
