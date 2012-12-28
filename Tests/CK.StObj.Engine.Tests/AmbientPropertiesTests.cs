using System;
using System.Linq;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public partial class AmbientPropertiesTests
    {
        public class AmbientPropertySetAttribute : Attribute, IStObjStructuralConfigurator
        {
            public string PropertyName { get; set; }

            public object PropertyValue { get; set; }

            public void Configure( IActivityLogger logger, IStObjMutableItem o )
            {
                o.SetAmbiantPropertyValue( logger, PropertyName, PropertyValue, "AmbientPropertySetAttribute" );
            }
        }

        public class DirectPropertySetAttribute : Attribute, IStObjStructuralConfigurator
        {
            public string PropertyName { get; set; }

            public object PropertyValue { get; set; }

            public void Configure( IActivityLogger logger, IStObjMutableItem o )
            {
                o.SetDirectPropertyValue( logger, PropertyName, PropertyValue, "DirectPropertySetAttribute" );
            }
        }

        [DirectPropertySet( PropertyName = "OneIntValue", PropertyValue = 3712 )]
        [StObj( ItemKind = DependentItemKindSpec.Container )]
        public class SimpleObjectDirect : IAmbientContract
        {
            public int OneIntValue { get; set; }
        }

        [AmbientPropertySet( PropertyName = "OneIntValue", PropertyValue = 3712 )]
        [StObj( ItemKind = DependentItemKindSpec.Container )]
        public class SimpleObjectAmbient : IAmbientContract
        {
            [AmbientProperty]
            public int OneIntValue { get; set; }
        }

        class StructuralConfigurator : IStObjStructuralConfigurator
        {
            readonly Action<IStObjMutableItem> _conf;

            public StructuralConfigurator( Action<IStObjMutableItem> conf )
            {
                _conf = conf;
            }

            public void Configure( IActivityLogger logger, IStObjMutableItem o )
            {
                _conf( o );
            }
        }

        class ConfiguratorOneIntValueSetTo42 : IStObjStructuralConfigurator
        {
            public void Configure( IActivityLogger logger, IStObjMutableItem o )
            {
                if( o.ObjectType == typeof( SimpleObjectDirect ) )
                {
                    o.SetDirectPropertyValue( logger, "OneIntValue", 42, "ConfiguratorOneIntValueSetTo42" );
                }
                if( o.ObjectType == typeof( SimpleObjectAmbient ) )
                {
                    o.SetAmbiantPropertyValue( logger, "OneIntValue", 42, "ConfiguratorOneIntValueSetTo42" );
                }
            }
        }


        #region Only one object.

        [Test]
        public void OneObjectDirectProperty()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterClass( typeof( SimpleObjectDirect ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.OrderedStObjs.FirstOrDefault(), Is.Not.Null, "We registered SimpleObjectDirect." );
                Assert.That( result.OrderedStObjs.First().Object, Is.InstanceOf<SimpleObjectDirect>() );
                Assert.That( ((SimpleObjectDirect)result.OrderedStObjs.First().Object).OneIntValue, Is.EqualTo( 3712 ), "Direct properties can be set by Attribute." );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new ConfiguratorOneIntValueSetTo42() );
                collector.RegisterClass( typeof( SimpleObjectDirect ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( ((SimpleObjectDirect)result.OrderedStObjs.First().Object).OneIntValue, Is.EqualTo( 42 ), "Direct properties can be set by any IStObjStructuralConfigurator participant (here the global one)." );
            }
        }

        [Test]
        public void OneObjectAmbiantProperty()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterClass( typeof( SimpleObjectAmbient ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.OrderedStObjs.FirstOrDefault(), Is.Not.Null, "We registered SimpleObjectAmbient." );
                Assert.That( result.OrderedStObjs.First().Object, Is.InstanceOf<SimpleObjectAmbient>() );
                Assert.That( ((SimpleObjectAmbient)result.OrderedStObjs.First().Object).OneIntValue, Is.EqualTo( 3712 ), "Same as Direct properties (above) regarding direct setting. The difference between Ambient and non-ambient lies in value propagation." );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new ConfiguratorOneIntValueSetTo42() );
                collector.RegisterClass( typeof( SimpleObjectAmbient ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( ((SimpleObjectAmbient)result.OrderedStObjs.First().Object).OneIntValue, Is.EqualTo( 42 ), "Same as Direct properties (above) regarding direct setting. The difference between Ambient and non-ambient lies in value propagation." );
            }
        }

        #endregion


        [DirectPropertySet( PropertyName = "OneIntValue", PropertyValue = 999 )]
        public class SpecializedObjectDirect : SimpleObjectDirect
        {
        }

        [AmbientPropertySet( PropertyName = "OneIntValue", PropertyValue = 999 )]
        public class SpecializedObjectAmbient : SimpleObjectAmbient
        {
        }


        [Test]
        public void AmbiantOrDirectPropertyDeclaredInBaseClassCanBeSet()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterClass( typeof( SpecializedObjectDirect ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.OrderedStObjs.Count, Is.EqualTo( 2 ), "SpecializedObjectDirect and SimpleObjectDirect." );
                Assert.That( result.Default.StObjMapper.GetObject<SpecializedObjectDirect>().OneIntValue, Is.EqualTo( 999 ), "Direct properties can be set by Attribute (or any IStObjStructuralConfigurator)." );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterClass( typeof( SpecializedObjectAmbient ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.OrderedStObjs.Count, Is.EqualTo( 2 ), "SpecializedObjectAmbient and SimpleObjectAmbient." );
                Assert.That( result.Default.StObjMapper.GetObject<SpecializedObjectAmbient>().OneIntValue, Is.EqualTo( 999 ), "Ambient properties can be set by Attribute (or any IStObjStructuralConfigurator)." );
            }
        }

        #region Propagation to container's children.

        [StObj( Container = typeof( SimpleObjectDirect ) )]
        public class SimpleObjectInsideDirect : IAmbientContract
        {
            [AmbientProperty]
            public int OneIntValue { get; set; }
        }

        [StObj( Container = typeof( SimpleObjectAmbient ) )]
        public class SimpleObjectInsideAmbiant : IAmbientContract
        {
            [AmbientProperty]
            public int OneIntValue { get; set; }
        }

        [Test]
        public void PropagationFromDirectPropertyDoesNotWork()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new ConfiguratorOneIntValueSetTo42() );
            collector.RegisterClass( typeof( SimpleObjectDirect ) );
            collector.RegisterClass( typeof( SimpleObjectInsideDirect ) );
            StObjCollectorResult result = collector.GetResult();
            Assert.That( result.Default.StObjMapper.GetObject<SimpleObjectInsideDirect>().OneIntValue, Is.EqualTo( 0 ), "A direct property (not an ambient property) CAN NOT be a source for ambient properties." );
            Assert.That( result.Default.StObjMapper.GetObject<SimpleObjectDirect>().OneIntValue, Is.EqualTo( 42 ), "...But it can be set by any IStObjStructuralConfigurator participant." );
        }

        [Test]
        public void PropagationFromAmbientProperty()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new ConfiguratorOneIntValueSetTo42() );
            collector.RegisterClass( typeof( SimpleObjectAmbient ) );
            collector.RegisterClass( typeof( SimpleObjectInsideAmbiant ) );
            StObjCollectorResult result = collector.GetResult();
            Assert.That( result.Default.StObjMapper.GetObject<SimpleObjectInsideAmbiant>().OneIntValue, Is.EqualTo( 42 ), "Of course, ambient properties propagate their values." );
        }

        #endregion

        #region Propagation to specialization.


        public class InheritedSimpleObject : SimpleObjectAmbient
        {
        }

        [AmbientPropertySet( PropertyName = "OneIntValue", PropertyValue = 9878654 )]
        public class InheritedSimpleObjectWithSet : SimpleObjectAmbient
        {
        }

        public class InheritedSimpleObjectWithoutSet : InheritedSimpleObjectWithSet
        {
        }

        [AmbientPropertySet( PropertyName = "OneIntValue", PropertyValue = 1111111 )]
        [StObj( ItemKind = DependentItemKindSpec.Container )]
        public class AnotherContainer : IAmbientContract
        {
            [AmbientProperty]
            public int OneIntValue { get; set; }
        }

        [Test]
        public void PropagationThroughSpecializationAndContainer()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new ConfiguratorOneIntValueSetTo42() );
                collector.RegisterClass( typeof( InheritedSimpleObject ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.Default.StObjMapper.GetObject<InheritedSimpleObject>().OneIntValue, Is.EqualTo( 42 ), "Since InheritedSimpleObject is a SimpleObjectAmbient, it has been configured." );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new ConfiguratorOneIntValueSetTo42() );
                collector.RegisterClass( typeof( InheritedSimpleObjectWithSet ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.Default.StObjMapper.GetObject<InheritedSimpleObjectWithSet>().OneIntValue, Is.EqualTo( 9878654 ), "More specialized InheritedSimpleObjectWithSet has been set." );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger, null,
                                                new StructuralConfigurator( o => { if( o.ObjectType.Name == "InheritedSimpleObjectWithSet" ) o.Container.Type = typeof( AnotherContainer ); } ) );
                collector.RegisterClass( typeof( AnotherContainer ) );
                collector.RegisterClass( typeof( InheritedSimpleObjectWithSet ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.Default.StObjMapper.Find<InheritedSimpleObjectWithSet>().Container.ObjectType.Name, Is.EqualTo( "AnotherContainer" ), "Container has changed." );
                Assert.That( result.Default.StObjMapper.GetObject<InheritedSimpleObjectWithSet>().OneIntValue, Is.EqualTo( 9878654 ), "Property does not change since it is set on the class itself." );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger, null,
                                                new StructuralConfigurator( o => { if( o.ObjectType.Name == "InheritedSimpleObjectWithoutSet" ) o.Container.Type = typeof( AnotherContainer ); } ) );
                collector.RegisterClass( typeof( AnotherContainer ) );
                collector.RegisterClass( typeof( InheritedSimpleObjectWithoutSet ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.Default.StObjMapper.Find<InheritedSimpleObjectWithSet>().Container, Is.Null, "Container of InheritedSimpleObjectWithSet has NOT changed (no container)." );
                Assert.That( result.Default.StObjMapper.Find<InheritedSimpleObjectWithoutSet>().Container.ObjectType.Name, Is.EqualTo( "AnotherContainer" ), "Container of InheritedSimpleObjectWithoutSet has changed." );

                Assert.That( result.Default.StObjMapper.GetObject<InheritedSimpleObjectWithoutSet>().OneIntValue, Is.EqualTo( 1111111 ), "Here, the container's value takes precedence since Property is NOT set on the class itself but on its Generalization." );
            }
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger, null,
                                                new StructuralConfigurator( o => { if( o.ObjectType.Name == "InheritedSimpleObjectWithSet" ) o.Container.Type = typeof( AnotherContainer ); } ) );
                collector.RegisterClass( typeof( AnotherContainer ) );
                collector.RegisterClass( typeof( InheritedSimpleObjectWithoutSet ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.Default.StObjMapper.Find<InheritedSimpleObjectWithSet>().Container.ObjectType.Name, Is.EqualTo( "AnotherContainer" ), "Container of InheritedSimpleObjectWithSet has changed." );
                Assert.That( result.Default.StObjMapper.Find<InheritedSimpleObjectWithoutSet>().Container.ObjectType.Name, Is.EqualTo( "AnotherContainer" ), "Container of InheritedSimpleObjectWithoutSet is inherited." );
                Assert.That( result.Default.StObjMapper.Find<InheritedSimpleObjectWithoutSet>().ConfiguredContainer, Is.Null, "Container is inherited, not directly configured for the object." );

                Assert.That( result.Default.StObjMapper.GetObject<InheritedSimpleObjectWithoutSet>().OneIntValue, Is.EqualTo( 9878654 ), "The inherited value is used since container is (also) inherited." );
            }
        }

        #endregion

        #region Potentially recursive resolution with type resolution

        class BaseForObject
        {
            [AmbientProperty]
            public TypeToMapBase Ambient { get; set; }
        }

        class TypeToMapBase
        {
        }

        class TypeToMap : TypeToMapBase, IAmbientContract
        {
        }

        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class C1 : BaseForObject, IAmbientContract
        {
        }

        [StObj( Container = typeof( C1 ) )]
        class O1InC1 : BaseForObject, IAmbientContract
        {
        }

        class C2 : C1
        {
        }

        [StObj( Container = typeof( C2 ) )]
        class O2InC2 : O1InC1
        {
        }

        class AmbientResolutionTypeSetter : IStObjStructuralConfigurator
        {
            public void Configure( IActivityLogger logger, IStObjMutableItem o )
            {
                if( o.ObjectType == typeof( C1 ) ) o.SetAmbiantPropertyConfiguration( logger, "Ambient", null, typeof(TypeToMap), StObjRequirementBehavior.ErrorIfNotStObj );
            }
        }


        [Test]
        public void TypeResolution()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new AmbientResolutionTypeSetter() );
            collector.RegisterClass( typeof( O2InC2 ) );
            collector.RegisterClass( typeof( C2 ) );
            collector.RegisterClass( typeof( TypeToMap ) );
            var result = collector.GetResult();
            Assert.That( result.HasFatalError, Is.False );
            TypeToMap o = result.Default.StObjMapper.GetObject<TypeToMap>();
            Assert.That( result.Default.StObjMapper.GetObject<C1>().Ambient, Is.SameAs( o ) );
            Assert.That( result.Default.StObjMapper.GetObject<O1InC1>().Ambient, Is.SameAs( o ) );

            Assert.That( result.Default.StObjMapper.GetObject<C2>(), Is.SameAs( result.Default.StObjMapper.GetObject<C1>() ) );
            Assert.That( result.Default.StObjMapper.GetObject<O2InC2>(), Is.SameAs( result.Default.StObjMapper.GetObject<O1InC1>() ) );
        }
        
        #endregion

    }
}
