using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;
using NUnit.Framework;
using CK.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setupable.Engine.Tests
{
    [TestFixture]
    public class SetupConfigReaderTests
    {

        [TestCase( @" .. ... ""SetupConfig"":{""Generalization"":""papa!"",""Requires"": [ ""n1"" , ""[ctx]db^name"" ] , ""RequiredBy"":[""d1""], ""Groups"":[""g1"",""g2""]  , ""Children"" : [""c1"",""c2"",""c3""]  } ... ... ..." )]
        [TestCase( @" .. ... 
                    SetupConfig :
                    {
                            ""Requires"" : [""n1"", ""[ctx]db^name""], 
                            ""RequiredBy"": [""d1""], 
                            ""Groups"": [""g1"",""g2""], 
                            ""Children"" : [""c1"",""c2"",""c3""],
                            ""Generalization"" : ""papa!""
                    } ... 
                    ... ..." )]
        public void Simple_application_on_container( string text )
        {
            var o = new DynamicContainerItem( "Test" );
            bool foundConfig;
            new SetupConfigReader().Apply( TestHelper.ConsoleMonitor, text, o, out foundConfig );
            Assert.That( o.Requires.Select( n => n.FullName ), Is.EquivalentTo( new[] { "n1", "[ctx]db^name" } ) );
            Assert.That( o.RequiredBy.Select( n => n.FullName ), Is.EquivalentTo( new[] { "d1" } ) );
            Assert.That( o.Groups.Select( n => n.FullName ), Is.EquivalentTo( new[] { "g1", "g2" } ) );
            Assert.That( o.Children.Select( n => n.FullName ), Is.EquivalentTo( new[] { "c1", "c2", "c3" } ) );
            Assert.That( o.Generalization.FullName, Is.EqualTo( "papa!" ) );
        }

        class Extended : SetupConfigReader
        {
            public Dictionary<string, object> ExtendedProperyies;

            protected override void OnUnknownProperty( StringMatcher m, string propName, ISetupObjectTransformerItem transformer, IMutableSetupBaseItem target )
            {
                object o;
                if( !m.TryMatchJSONValue( out o ) ) m.SetError( $"value for {propName}." );
                else
                {
                    if( ExtendedProperyies == null ) ExtendedProperyies = new Dictionary<string, object>();
                    ExtendedProperyies.Add( propName, o );
                }
            }
        }

        [Test]
        public void test_SetupConfigReader_extension()
        {
            string text = @"SetupConfig:{ ""K"":12, ""Requires"": [""r""] }";
            var o = new DynamicContainerItem( "Test" );
            var r = new Extended();
            bool foundConfig;
            Assert.That( r.Apply( TestHelper.ConsoleMonitor, text, o, out foundConfig ) );
            Assert.That( o.Requires.Select( n => n.FullName ), Is.EquivalentTo( new[] { "r" } ) );
            Assert.That( r.ExtendedProperyies["K"], Is.EqualTo( 12.0 ) );
        }

        class TransformerTest : SetupObjectItem, ISetupObjectTransformerItem
        {
            public SetupObjectItem Source { get; set; }

            public SetupObjectItem Target { get; set; }

            protected override object StartDependencySort() => null;
        }

        [Test]
        public void applying_to_transformer_and_its_target()
        {
            string text = @"SetupConfig:
                        { 
                            ""TargetContainer"":""container"", 
                            ""RemoveRequires"": [""R""],
                            ""RemoveRequiredBy"": [""RBy""],
                            ""RemoveGroups"": [""G""],
                            ""RemoveChildren"": [""C""]
                            ""AddRequires"": [""newR""],
                            ""AddRequiredBy"": [""newRBy""],
                            ""AddGroups"": [""newG""],
                            ""AddChildren"": [""newC""],

                            ""TransformerContainer"": ""from config..."",
                            ""TransformerRequires"": [""TR""],
                            ""TransformerRequiredBy"": [""TRBy""],
                            ""TransformerGroups"": [""TG""],

                            ""Requires"": ""Will be an unknown property!"",
                            ""RequiredBy"": ""Will be an unknown property!"",
                            ""Murfn"": ""Will be an unknown property!"",
                        }";
            var o = new DynamicContainerItem( "Test" );
            o.Container = new NamedDependentItemContainerRef( "Will be removed by transformer." );
            o.Requires.Add( "R" );
            o.Requires.Add( "RX" );
            o.RequiredBy.Add( "RBy" );
            o.RequiredBy.Add( "RByX" );
            o.Groups.Add( "G" );
            o.Groups.Add( "GX" );
            o.Children.Add( "C" );
            o.Children.Add( "CX" );
            var t = new TransformerTest();
            t.Container = new NamedDependentItemContainerRef( "Will be removed by configuration." );
            var r = new Extended();
            bool foundConfig;
            Assert.That( r.Apply( TestHelper.ConsoleMonitor, text, t, o, out foundConfig ) );

            Assert.That( o.Container.FullName, Is.EqualTo( "container" ) );
            Assert.That( o.Requires.Select( n => n.FullName ), Is.EquivalentTo( new[] { "RX", "newR" } ) );
            Assert.That( o.RequiredBy.Select( n => n.FullName ), Is.EquivalentTo( new[] { "RByX", "newRBy" } ) );
            Assert.That( o.Groups.Select( n => n.FullName ), Is.EquivalentTo( new[] { "GX", "newG" } ) );
            Assert.That( o.Children.Select( n => n.FullName ), Is.EquivalentTo( new[] { "CX", "newC" } ) );

            Assert.That( t.Container.FullName, Is.EqualTo( "from config..." ) );
            Assert.That( t.Requires.Select( n => n.FullName ), Is.EquivalentTo( new[] { "TR" } ) );
            Assert.That( t.RequiredBy.Select( n => n.FullName ), Is.EquivalentTo( new[] { "TRBy" } ) );
            Assert.That( t.Groups.Select( n => n.FullName ), Is.EquivalentTo( new[] { "TG" } ) );

            Assert.That( r.ExtendedProperyies["Requires"], Is.EqualTo( "Will be an unknown property!" ) );
            Assert.That( r.ExtendedProperyies["RequiredBy"], Is.EqualTo( "Will be an unknown property!" ) );
            Assert.That( r.ExtendedProperyies["Murfn"], Is.EqualTo( "Will be an unknown property!" ) );


        }

    }
}
