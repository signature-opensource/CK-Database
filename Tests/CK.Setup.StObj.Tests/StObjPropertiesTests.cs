using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Reflection;

namespace CK.Setup.StObj.Tests
{
    [TestFixture]
    public class StObjPropertiesTests
    {
        public class StObjPropertySetAttribute : Attribute, IStObjStructuralConfigurator
        {
            public string PropertyName { get; set; }

            public object PropertyValue { get; set; }

            public void Configure( IActivityLogger logger, IStObjMutableItem o )
            {
                o.SetStObjPropertyValue( logger, PropertyName, PropertyValue );
            }
        }

        [StObjPropertySetAttribute( PropertyName = "OneIntValue", PropertyValue = 3712 )]
        [StObj( ItemKind = DependentItemKindSpec.Container )]
        public class SimpleContainer : IAmbientContract
        {
        }

        [Test]
        public void OneObject()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterClass( typeof( SimpleContainer ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.OrderedStObjs.First().GetStObjProperty( "OneIntValue" ), Is.EqualTo( 3712 ) );
            }
        }

        #region Mergeable & Propagation

        [StObj( ItemKind = DependentItemKindSpec.Container )]
        public class SpecializedContainer : SimpleContainer
        {
        }

        [StObj( Container = typeof( SpecializedContainer ), ItemKind = DependentItemKindSpec.Item )]
        public class BaseObject : IAmbientContract
        {
        }

        [StObj( ItemKind = DependentItemKindSpec.Item )]
        public class SpecializedObject : BaseObject
        {
        }

        [StObj( Container = typeof( SpecializedContainer ), ItemKind = DependentItemKindSpec.Item )]
        public class SpecializedObjectWithExplicitContainer : SpecializedObject
        {
        }

        class SchmurtzConfigurator : IStObjStructuralConfigurator
        {
            public void Configure( IActivityLogger logger, IStObjMutableItem o )
            {
                if( o.ObjectType == typeof( SimpleContainer ) ) o.SetStObjPropertyValue( logger, "SchmurtzProp", new SchmurtzProperty( "Root" ) );
                if( o.ObjectType == typeof( SpecializedContainer ) ) o.SetStObjPropertyValue( logger, "SchmurtzProp", new SchmurtzProperty( "SpecializedContainer specializes Root" ) );
                if( o.ObjectType == typeof( BaseObject ) ) o.SetStObjPropertyValue( logger, "SchmurtzProp", new SchmurtzProperty( "BaseObject belongs to SpecializedContainer" ) );
                if( o.ObjectType == typeof( SpecializedObject ) ) o.SetStObjPropertyValue( logger, "SchmurtzProp", new SchmurtzProperty( "Finally: SpecializedObject inherits from BaseObject" ) );
                if( o.ObjectType == typeof( SpecializedObjectWithExplicitContainer ) ) o.SetStObjPropertyValue( logger, "SchmurtzProp", new SchmurtzProperty( "SpecializedObjectWithExplicitContainer inherits from BaseObject BUT is directly associated to SpecializedContainer" ) );
            }
        }

        [Test]
        public void SchmurtzPropagation()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Logger, null, new SchmurtzConfigurator() );
            collector.RegisterClass( typeof( SimpleContainer ) );
            collector.RegisterClass( typeof( SpecializedContainer ) );
            collector.RegisterClass( typeof( BaseObject ) );
            collector.RegisterClass( typeof( SpecializedObject ) );
            collector.RegisterClass( typeof( SpecializedObjectWithExplicitContainer ) );
            StObjCollectorResult result = collector.GetResult();

            Assert.That( result.OrderedStObjs.First( s => s.ObjectType == typeof( BaseObject ) ).GetStObjProperty( "SchmurtzProp" ).ToString(),
                Is.EqualTo( "Root => SpecializedContainer specializes Root => BaseObject belongs to SpecializedContainer" ) );

            Assert.That( result.OrderedStObjs.First( s => s.ObjectType == typeof( SpecializedObject ) ).GetStObjProperty( "SchmurtzProp" ).ToString(),
                Is.EqualTo( "Root => SpecializedContainer specializes Root => BaseObject belongs to SpecializedContainer => Finally: SpecializedObject inherits from BaseObject" ), 
                "Here, we follow the Generalization link, since there is NO direct Container." );

            Assert.That( result.OrderedStObjs.First( s => s.ObjectType == typeof( SpecializedObjectWithExplicitContainer ) ).GetStObjProperty( "SchmurtzProp" ).ToString(),
                Is.EqualTo( "Root => SpecializedContainer specializes Root => SpecializedObjectWithExplicitContainer inherits from BaseObject BUT is directly associated to SpecializedContainer" ),
                "Here, we DO NOT follow the Generalization link, since the Container is set, the Container has the priority." );
        }

        #endregion
    }
}
