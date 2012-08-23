using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.Setup.StObj.Tests;

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

    [AddContext( typeof( int ) )]
    [RemoveContext( typeof( AmbiantContractCollector.DefaultContextType ) )]
    public class AmbiantScoped : Ambiant
    {
    }

    public class AmbiantScopedChild : AmbiantScoped
    {
    }

    public class ScopedBaseDefiner : Base, IAmbiantContractDefiner
    {
    }

    [AddContext( typeof( int ) )]
    [RemoveContext( typeof( AmbiantContractCollector.DefaultContextType ) )]
    public class ByDefiner : ScopedBaseDefiner
    {
    }

    [AddContext( typeof( long ) )]
    [RemoveContext( typeof( int ) )]
    public class ScopedOtherFromDefiner : ByDefiner
    {
    }

    [TestFixture]
    public class AmbiantContractTests
    {
        [Test]
        public void NonAmbiantContextRegistration()
        {
            AmbiantContractCollector c = new AmbiantContractCollector();
            Assert.That( c.RegisterClass( typeof( Base ) ), Is.True );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 2 ), "AbstractBase, Base" );

            var r = c.GetResult().Default;
            CheckEmpty( r );
        }

        [Test]
        public void OneAmbiantContextRegistration()
        {
            AmbiantContractCollector c = new AmbiantContractCollector();
            Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.True );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 3 ), "AbstractBase, Base, Ambiant" );

            var r = c.GetResult().Default;
            Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 1 ) );
            Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
            CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambiant ), typeof( Ambiant ) ) );
        }

        [Test]
        public void OneAbstractAmbiantContextRegistration()
        {
            AmbiantContractCollector c = new AmbiantContractCollector();
            Assert.That( c.RegisterClass( typeof( AbstractAmbiant ) ), Is.True );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 2 ), "AbstractBase, AbstractAmbiant" );

            var r = c.GetResult().Default;
            Assert.That( r.AbstractTails.Count, Is.EqualTo( 1 ) );
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
                    Assert.That( c.RegisteredTypeCount, Is.EqualTo( 4 ), "AbstractBase, Base, Ambiant, AmbiantChild" );
                    var r = c.GetResult().Default;
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
                Assert.That( c.RegisteredTypeCount, Is.EqualTo( 5 ), "AbstractBase, Base, Ambiant, AmbiantChild, AmbiantChildAbstractTail" );
                var r = c.GetResult().Default;
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
        public void WithContext()
        {
            Action<AmbiantContractCollector> check = c =>
            {
                var rAll = c.GetResult();
                rAll.LogErrorAndWarnings( TestHelper.Logger );
                {
                    var r = rAll.Default;
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 1 ) );
                    Assert.That( r.ConcreteClasses.Count == 1 && r.ConcreteClasses[0].SequenceEqual( new[] { typeof( Ambiant ), typeof( AmbiantChild ) } ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambiant ), typeof( AmbiantChild ) ), Tuple.Create( typeof( AmbiantChild ), typeof( AmbiantChild ) ) );
                }
                {
                    var r = rAll[typeof( int )];
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
                Assert.That( c.RegisterClass( typeof( AmbiantScoped ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbiantChildAbstractTail ) ), Is.True );
                check( c );
            }
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbiantChildAbstractTail ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbiantScoped ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( Ambiant ) ), Is.False, "Registered by previous AmbiantChildAbstractTail." );
                check( c );
            }
        }

        [Test]
        public void WithContextAndChild()
        {
            Action<AmbiantContractCollector> check = c =>
            {
                var rAll = c.GetResult();
                Assert.That( rAll.Default.ConcreteClasses.Count == 1 && rAll.Default.ConcreteClasses[0][0] == typeof( Ambiant ), "Default context contains Ambiant." );
                
                // Whereas int context contains Ambiant, AmbiantScoped and AmbiantScopedChild.
                {
                    var r = rAll[typeof( int )];
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
                Assert.That( c.RegisteredTypeCount, Is.EqualTo( 3 ), "AbstractBase, Base, ScopedBaseDefiner" );
                var rAll = c.GetResult();
                CheckEmpty( rAll.Default );
                Assert.That( rAll.Count, Is.EqualTo( 1 ) );
            };
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( ScopedBaseDefiner ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void WithContextAndChildMove()
        {
            Action<AmbiantContractCollector> check = c =>
            {
                var rAll = c.GetResult();
                CheckEmpty( rAll.Default );

                var rInt = rAll[typeof( int )];
                Assert.That( rInt, Is.Not.Null );
                Assert.That( rInt.ConcreteClasses[0].SequenceEqual( new[] { typeof( ByDefiner ) } ) );

                var rLong = rAll[typeof( long )];
                Assert.That( rLong, Is.Not.Null );
                Assert.That( rLong.ConcreteClasses[0].SequenceEqual( new[] { typeof( ByDefiner ), typeof( ScopedOtherFromDefiner ) } ) );
                CheckLocalMappings( rLong.Mappings,
                    Tuple.Create( typeof( ByDefiner ), typeof( ScopedOtherFromDefiner ) ),
                    Tuple.Create( typeof( ScopedOtherFromDefiner ), typeof( ScopedOtherFromDefiner ) ) );
            };
            {
                AmbiantContractCollector c = new AmbiantContractCollector();
                Assert.That( c.RegisterClass( typeof( ScopedOtherFromDefiner ) ), Is.True );
                check( c );
            }
        }

        private static void CheckEmpty( AmbiantContractCollectorContextualResult r )
        {
            Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
            Assert.That( r.AbstractTails, Is.Empty );
            Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ConcreteClasses, Is.Empty );
            Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.ClassAmbiguities, Is.Empty );
            Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.InterfaceAmbiguities, Is.Empty );
            CheckLocalMappings( r.Mappings );
        }

        static void CheckLocalMappings( IAmbiantTypeContextualMapper actual, params Tuple<Type, Type>[] expected )
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
