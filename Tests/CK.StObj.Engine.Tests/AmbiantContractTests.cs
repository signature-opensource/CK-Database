#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\AmbiantContractTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.StObj.Engine.Tests;

namespace CK.Setup.Tests
{

    [PreventAutoImplementation]
    public abstract class AbstractBase
    {
    }

    public class Base : AbstractBase
    {
    }

    public class Ambient : Base, IAmbientContract
    {
    }

    public class AmbientChild : Ambient
    {
    }

    [PreventAutoImplementation]
    public abstract class AmbientChildAbstractTail : AmbientChild
    {
    }

    [PreventAutoImplementation]
    public abstract class AbstractAmbient : AbstractBase, IAmbientContract
    {
    }

    [AddContext( "int" )]
    [RemoveDefaultContext]
    public class AmbientScoped : Ambient
    {
    }

    public class AmbientScopedChild : AmbientScoped
    {
    }

    public class ScopedBaseDefiner : Base, IAmbientContractDefiner
    {
    }

    [AddContext( "int" )]
    [RemoveContext( "" )]
    public class ByDefiner : ScopedBaseDefiner
    {
    }

    [AddContext( "long" )]
    [RemoveContext( "int" )]
    public class ScopedOtherFromDefiner : ByDefiner
    {
    }

    public class AmbientRoot : AmbientTypeMap<ContextForTypes>
    {
        protected override IContextualTypeMap CreateContext<T,TC>( IActivityMonitor monitor, string context )
        {
            return new ContextForTypes( this, context );
        }
    }

    public class ContextForTypes : AmbientContextualTypeMap<TypeInfo, TypeInsideContext>
    {
        public ContextForTypes( IContextualRoot<IContextualTypeMap> owner, string c )
            : base( owner, c )
        {
        }
    }

    public class TypeInfo : AmbientTypeInfo
    {
        public TypeInfo( TypeInfo parent, Type t )
            : base( parent, t )
        {
        }

        protected override TC CreateContextTypeInfo<T, TC>( TC generalization, IContextualTypeMap context )
        {
            return (TC)(object)(new TypeInsideContext( this, (TypeInsideContext)(object)generalization, context ));
        }
    }

    public class TypeInsideContext : AmbientContextualTypeInfo<TypeInfo, TypeInsideContext>
    {
        public TypeInsideContext( TypeInfo t, TypeInsideContext generalization, IContextualTypeMap context )
            : base( t, generalization, context )
        {
        }
    }

    public class DefaultAmbientContractCollector : AmbientContractCollector<ContextForTypes,TypeInfo,TypeInsideContext>
    {
        public DefaultAmbientContractCollector( IActivityMonitor monitor = null, IAmbientContractDispatcher contextDispatcher = null )
            : base( 
                  monitor ?? new ActivityMonitor(), 
                  l => new AmbientRoot(), 
                  ( l, p, t ) => new TypeInfo( p, t ), 
                  new DynamicAssembly(), 
                  null, 
                  contextDispatcher )
        {
        }
    }

    [TestFixture]
    public class AmbientContractTests
    {
        [Test]
        public void NonAmbientContextRegistration()
        {
            DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
            Assert.That( c.RegisterClass( typeof( Base ) ), Is.True );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 2 ), "AbstractBase, Base" );

            var r = c.GetResult().Default;
            CheckEmpty( r );
        }

        [Test]
        public void OneAmbientContextRegistration()
        {
            DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
            Assert.That( c.RegisterClass( typeof( Ambient ) ), Is.True );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 3 ), "AbstractBase, Base, Ambient" );

            var r = c.GetResult().Default;
            Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 1 ) );
            Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
            Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
            CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambient ), typeof( Ambient ) ) );
        }

        [Test]
        public void OneAbstractAmbientContextRegistration()
        {
            DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
            Assert.That( c.RegisterClass( typeof( AbstractAmbient ) ), Is.True );
            Assert.That( c.RegisteredTypeCount, Is.EqualTo( 2 ), "AbstractBase, AbstractAmbient" );

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
            Action<DefaultAmbientContractCollector> check = c =>
                {
                    Assert.That( c.RegisteredTypeCount, Is.EqualTo( 4 ), "AbstractBase, Base, Ambient, AmbientChild" );
                    var r = c.GetResult().Default;
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 1 ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambient ), typeof( AmbientChild ) ), Tuple.Create( typeof( AmbientChild ), typeof( AmbientChild ) ) );
                };
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbientChild ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( Ambient ) ), Is.False, "Already registered by its Child." );
                check( c );
            }
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( Ambient ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbientChild ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void AbstractTail()
        {
            Action<DefaultAmbientContractCollector> check = c =>
            {
                Assert.That( c.RegisteredTypeCount, Is.EqualTo( 5 ), "AbstractBase, Base, Ambient, AmbientChild, AmbientChildAbstractTail" );
                var r = c.GetResult().Default;
                Assert.That( r.AbstractTails.Count, Is.EqualTo( 1 ), "AmbientChild => AmbientChildAbstractTail is the abstract tail." );
                Assert.That( r.ConcreteClasses.Count, Is.EqualTo( 1 ), "AmbientChild is the Concrete class." );
                Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambient ), typeof( AmbientChild ) ), Tuple.Create( typeof( AmbientChild ), typeof( AmbientChild ) ) );
            };
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbientChildAbstractTail ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbientChild ) ), Is.False, "Already registered by its Child." );
                Assert.That( c.RegisterClass( typeof( Ambient ) ), Is.False, "Already registered by its Child." );
                check( c );
            }
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( Ambient ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbientChild ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbientChildAbstractTail ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void WithContext()
        {
            Action<DefaultAmbientContractCollector> check = c =>
            {
                var rAll = c.GetResult();
                rAll.LogErrorAndWarnings( TestHelper.Monitor );
                {
                    var r = rAll.Default;
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 1 ) );
                    Assert.That( r.ConcreteClasses.Count == 1 && r.ConcreteClasses[0].Select( a => a.AmbientTypeInfo.Type ).SequenceEqual( new[] { typeof( Ambient ), typeof( AmbientChild ) } ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambient ), typeof( AmbientChild ) ), Tuple.Create( typeof( AmbientChild ), typeof( AmbientChild ) ) );
                }
                {
                    var r = rAll.FindContext( "int" );
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.ConcreteClasses.Count == 1 && r.ConcreteClasses[0].Select( a => a.AmbientTypeInfo.Type ).SequenceEqual( new[] { typeof( Ambient ), typeof( AmbientScoped ) } ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings, Tuple.Create( typeof( Ambient ), typeof( AmbientScoped ) ), Tuple.Create( typeof( AmbientScoped ), typeof( AmbientScoped ) ) );
                }
            };
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( Ambient ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbientScoped ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbientChildAbstractTail ) ), Is.True );
                check( c );
            }
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbientChildAbstractTail ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( AmbientScoped ) ), Is.True );
                Assert.That( c.RegisterClass( typeof( Ambient ) ), Is.False, "Registered by previous AmbientChildAbstractTail." );
                check( c );
            }
        }

        [Test]
        public void WithContextAndChild()
        {
            Action<DefaultAmbientContractCollector> check = c =>
            {
                var rAll = c.GetResult();
                Assert.That( rAll.Default.ConcreteClasses.Count == 1 && rAll.Default.ConcreteClasses[0][0].AmbientTypeInfo.Type == typeof( Ambient ), "Default context contains Ambient." );
                
                // Whereas int context contains Ambient, AmbientScoped and AmbientScopedChild.
                {
                    var r = rAll.FindContext(  "int" );
                    Assert.That( r.AbstractTails.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.ConcreteClasses.Count == 1 && r.ConcreteClasses[0].Select( a => a.AmbientTypeInfo.Type ).SequenceEqual( new[] { typeof( Ambient ), typeof( AmbientScoped ), typeof( AmbientScopedChild ) } ) );
                    Assert.That( r.ClassAmbiguities.Count, Is.EqualTo( 0 ) );
                    Assert.That( r.InterfaceAmbiguities.Count, Is.EqualTo( 0 ) );
                    CheckLocalMappings( r.Mappings,
                        Tuple.Create( typeof( Ambient ), typeof( AmbientScopedChild ) ),
                        Tuple.Create( typeof( AmbientScoped ), typeof( AmbientScopedChild ) ),
                        Tuple.Create( typeof( AmbientScopedChild ), typeof( AmbientScopedChild ) ) );
                }
            };
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( AmbientScopedChild ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void DefinerAlone()
        {
            Action<DefaultAmbientContractCollector> check = c =>
            {
                Assert.That( c.RegisteredTypeCount, Is.EqualTo( 3 ), "AbstractBase, Base, ScopedBaseDefiner" );
                var rAll = c.GetResult();
                CheckEmpty( rAll.Default );
                Assert.That( rAll.Contexts.Count, Is.EqualTo( 1 ) );
            };
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( ScopedBaseDefiner ) ), Is.True );
                check( c );
            }
        }

        [Test]
        public void WithContextAndChildMove()
        {
            Action<DefaultAmbientContractCollector> check = c =>
            {
                var rAll = c.GetResult();
                CheckEmpty( rAll.Default );

                var rInt = rAll.FindContext( "int" );
                Assert.That( rInt, Is.Not.Null );
                Assert.That( rInt.ConcreteClasses[0].Select( a => a.AmbientTypeInfo.Type ).SequenceEqual( new[] { typeof( ByDefiner ) } ) );

                var rLong = rAll.FindContext( "long" );
                Assert.That( rLong, Is.Not.Null );
                Assert.That( rLong.ConcreteClasses[0].Select( a => a.AmbientTypeInfo.Type ).SequenceEqual( new[] { typeof( ByDefiner ), typeof( ScopedOtherFromDefiner ) } ) );
                CheckLocalMappings( rLong.Mappings,
                    Tuple.Create( typeof( ByDefiner ), typeof( ScopedOtherFromDefiner ) ),
                    Tuple.Create( typeof( ScopedOtherFromDefiner ), typeof( ScopedOtherFromDefiner ) ) );
            };
            {
                DefaultAmbientContractCollector c = new DefaultAmbientContractCollector();
                Assert.That( c.RegisterClass( typeof( ScopedOtherFromDefiner ) ), Is.True );
                check( c );
            }
        }

        private static void CheckEmpty( AmbientContractCollectorContextualResult<ContextForTypes,TypeInfo,TypeInsideContext> r )
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

        static void CheckLocalMappings( IContextualTypeMap actual, params Tuple<Type, Type>[] expected )
        {
            foreach( var e in expected )
            {
                Assert.That( actual.IsMapped( e.Item1 ), "Key type: " + e.Item1 + " exists." );
                Assert.That( actual.ToLeafType( e.Item1 ), Is.EqualTo( e.Item2 ), "Type : " + e.Item1 + " is mapped to " + e.Item2 );
            }
            Assert.That( actual.MappedTypeCount, Is.EqualTo( expected.Count() ), "No extra mappings exist." );
        }

    }
}
