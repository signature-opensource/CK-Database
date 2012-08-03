using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Tests.IntoTheWild
{
    public abstract class AbstractBase
    {
    }

    public class Base : AbstractBase
    {
    }

    public class Ambiant : Base, IAmbiantContract
    {
    }

    public class AmbiantChild : Ambiant
    {
    }

    public abstract class AmbiantChildAbstractTail : AmbiantChild
    {
    }

    public abstract class AbstractAmbiant : AbstractBase, IAmbiantContract
    {
    }

    public class AmbiantScoped : Ambiant, IAmbiantContract<int>
    {
    }

    public class AmbiantScopedChild : AmbiantScoped
    {
    }

    public class ScopedBaseDefiner : Base, IAmbiantContractDefiner<int>
    {
    }

    public class ByDefiner : ScopedBaseDefiner
    {
    }

    public class ScopedOtherFromDefiner : ByDefiner, IAmbiantContract<long>
    {
    }

    [TestFixture]
    public class AmbiantContractTests
    {
        [Test]
        public void NonAmbiantContextRegistration()
        {
            AmbiantContractCollector c = new AmbiantContractCollector();
            Assert.That( c.RegisterClass( typeof( Base ) ), Is.False, "Base is not an AmbiantContract at all." );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 0 ) );

            var r = c.GetResult().Default;
            Assert.That( r.AbstractClasses.Count, Is.EqualTo( 0 ) );
            Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.Mappings.Count, Is.EqualTo( 0 ) );
        }

        [Test]
        public void OneAmbiantContextRegistration()
        {
            AmbiantContractCollector c = new AmbiantContractCollector();
            Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.True, "Ambiant is an AmbiantContract." );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 1 ) );

            var r = c.GetResult().Default;
            Assert.That( r.AbstractClasses.Count, Is.EqualTo( 0 ) );
            Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 1 ) );
            Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
            CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambiant ), typeof( Ambiant ) ) );
        }

        [Test]
        public void OneAbstractAmbiantContextRegistration()
        {
            AmbiantContractCollector c = new AmbiantContractCollector();
            Assert.That( c.RegisterClass( typeof( AbstractAmbiant ) ), Is.True, "AbstractAmbiant is an AmbiantContract." );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 1 ) );

            var r = c.GetResult().Default;
            Assert.That( r.AbstractClasses.Count, Is.EqualTo( 1 ) );
            Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
            CheckLocalMappings( r.Mappings );
        }

        [Test]
        public void ChildRegistration()
        {
            Action<AmbiantContractCollector> check = c =>
                {
                    Assert.That( c.RegisteredTypeCount, Is.EqualTo( 2 ) );
                    var r = c.GetResult().Default;
                    Assert.That( r.AbstractClasses.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 1 ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambiant ), typeof( AmbiantChild ) ), Tuple.Create( typeof( AmbiantChild ), typeof( AmbiantChild ) ) );
                };
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbiantChild ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.False, "Already registered by its Child." );
                check( c );
            }
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbiantChild ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void AbstractTail()
        {
            Action<AmbiantContractCollector> check = c =>
            {
                Assert.That( c.RegisteredTypeCount, Is.EqualTo( 3 ) );
                var r = c.GetResult().Default;
                Assert.That( r.AbstractClasses.Count, Is.EqualTo( 0 ) );
                Assert.That( r.AbstractTails.Count, Is.EqualTo( 1 ), "AmbiantChild => AmbiantChildAbstractTail is the abstract tail." );
                Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 1 ), "AmbiantChild is the Concrete class." );
                Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambiant ), typeof( AmbiantChild ) ), Tuple.Create( typeof( AmbiantChild ), typeof( AmbiantChild ) ) );
            };
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbiantChildAbstractTail ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbiantChild ) ), Is.False, "Already registered by its Child." );
                Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.False, "Already registered by its Child." );
                check( c );
            }
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbiantChild ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbiantChildAbstractTail ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void WithScope()
        {
            Action<AmbiantContractCollector> check = c =>
            {
                Assert.That( c.RegisteredTypeCount, Is.EqualTo( 3 + 2 ) );
                var rAll = c.GetResult();
                {
                    var r = rAll.Default;
                    Assert.That( r.AbstractClasses.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 1 ) );
                    Assert.That( r.ConcreteClasses.Count == 1 && r.ConcreteClasses[0].SequenceEqual( new[] { typeof( Ambiant ), typeof( AmbiantChild ) } ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambiant ), typeof( AmbiantChild ) ), Tuple.Create( typeof( AmbiantChild ), typeof( AmbiantChild ) ) );
                }
                {
                    var r = rAll[typeof( int )];
                    Assert.That( r.AbstractClasses.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.ConcreteClasses.Count == 1 && r.ConcreteClasses[0].SequenceEqual( new[] { typeof( Ambiant ), typeof( AmbiantScoped ) } ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambiant ), typeof( AmbiantScoped ) ), Tuple.Create( typeof( AmbiantScoped ), typeof( AmbiantScoped ) ) );
                }
            };
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbiantScoped ) ), Is.True, "Registered in Scope <int>" );
                Assert.That( c.RegisterClass( typeof( AmbiantChildAbstractTail ) ), Is.True );
                check( c );
            }
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbiantChildAbstractTail ) ), Is.True, "Registered in the default context." );
                Assert.That( c.RegisterClass( typeof( AmbiantScoped ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.False, "Registered by previous AmbiantChildAbstractTail." );
                check( c );
            }
        }

        [Test]
        public void ScopedChild()
        {
            Action<AmbiantContractCollector> check = c =>
            {
                Assert.That( c.RegisteredTypeCount, Is.EqualTo( 3 ) );
                var rAll = c.GetResult();
                CheckEmpty( rAll.Default );
                {
                    var r = rAll[typeof( int )];
                    Assert.That( r.AbstractClasses.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.ConcreteClasses.Count == 1 && r.ConcreteClasses[0].SequenceEqual( new[] { typeof( Ambiant ), typeof( AmbiantScoped ), typeof( AmbiantScopedChild ) } ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings,
                        Tuple.Create( typeof( Ambiant ), typeof( AmbiantScopedChild ) ),
                        Tuple.Create( typeof( AmbiantScoped ), typeof( AmbiantScopedChild ) ),
                        Tuple.Create( typeof( AmbiantScopedChild ), typeof( AmbiantScopedChild ) ) );
                }
            };
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbiantScopedChild ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void DefinerAlone()
        {
            Action<AmbiantContractCollector> check = c =>
            {
                Assert.That( c.RegisteredTypeCount, Is.EqualTo( 1 ) );
                var rAll = c.GetResult();
                CheckEmpty( rAll.Default );
                var rInt = rAll[typeof( int )];
                Assert.That( rInt.PureDefiners.Count, Is.EqualTo( 1 ) );
            };
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( ScopedBaseDefiner ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void ScopeRedefinitionByContract()
        {
            Action<AmbiantContractCollector> check = c =>
            {
                var rAll = c.GetResult();
                CheckEmpty( rAll.Default );
                Assert.That( rAll[typeof( int )], Is.Null );
                var rLong = rAll[typeof( long )];
                Assert.That( rLong, Is.Not.Null );
                Assert.That( rLong.ConcreteClasses[0], Is.EqualTo( typeof( ScopedOtherFromDefiner ) ) );
                CheckLocalMappings( rLong.Mappings,
                    Tuple.Create( typeof( ByDefiner ), typeof( ScopedOtherFromDefiner ) ),
                    Tuple.Create( typeof( ScopedOtherFromDefiner ), typeof( ScopedOtherFromDefiner ) ) );
            };
            // Context redefinition supports requires a
            // deep change in the way ContextResultCollector<AmbiantContractCollectorContextResult> registers the types:
            // types that are redefined in other contexts must be "hidden" or a first registration
            // must keep all types regardless of their context and then processes only the longest ones
            // (in GetResult call).
            // For the moment, we just forbid such context redefinition.

            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.Throws<CKException>( () => c.RegisterClass( typeof( ScopedOtherFromDefiner ) ) );
                //Assert.That( c.RegisterClass( typeof( ScopedOtherFromDefiner ) ), Is.True );
                //check( c );
            }
            {
                // ByDefiner is registered in <int>.
                // ScopedOtherFromDefiner reroutes the registration in <long> context.
                //ContextResultCollector<AmbiantContractCollectorContextResult> c = new ContextResultCollector<AmbiantContractCollectorContextResult>();
                //Assert.That( c.RegisterClass( typeof( ByDefiner ) ), Is.True );
                //Assert.That( c.RegisterClass( typeof( ScopedOtherFromDefiner ) ), Is.True );
                //check( c );
            }
        }

        private static void CheckEmpty( AmbiantContractResult r )
        {
            Assert.That( r.AbstractClasses.Count, Is.EqualTo( 0 ) );
            Assert.That( r.AbstractClasses, Is.Empty );
            Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
            Assert.That( r.AbstractTails, Is.Empty );
            Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ConcreteClasses, Is.Empty );
            Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ClassAmbiguities, Is.Empty );
            Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.InterfaceAmbiguities, Is.Empty );
            Assert.That( r.PureDefiners.Count, Is.EqualTo( 0 ) );
            Assert.That( r.PureDefiners, Is.Empty );
            CheckLocalMappings( r.Mappings );
        }

        static void CheckLocalMappings( IAmbiantTypeMapper actual, params Tuple<Type, Type>[] expected )
        {
            foreach( var e in expected )
            {
                Assert.That( actual.IsMapped( e.Item1 ), "Key type: " + e.Item1 + " exists." );
                Assert.That( actual[e.Item1], Is.EqualTo( e.Item2 ), "Type : " + e.Item1 + " is mapped to " + e.Item2 );
            }
            Assert.That( actual.Count, Is.EqualTo( expected.Count() ), "No extra mappings exist." );
        }

    }
}
