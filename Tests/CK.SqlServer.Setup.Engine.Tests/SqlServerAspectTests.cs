using CK.Core;
using CK.Setup;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests;

[TestFixture]
public class SqlServerAspectTests
{
    static IConfiguration Section( string json )
    {
        return ImmutableConfigurationSection.CreateFromJson( "Aspects", "SqlServer", json );
    }

    [Test]
    public void the_section_is_read()
    {
        var a = new SqlServerAspect( Section( """
            {
              "DefaultDatabaseConnectionString": "Server=.;Database=Db",
              "Databases": { "histoDB": { "ConnectionString": "Server=.;Database=Histo", "AutoCreate": true } },
              "GlobalResolution": true
            }
            """ ) );
        a.DefaultDatabaseConnectionString.ShouldBe( "Server=.;Database=Db" );
        a.Databases.Single().LogicalDatabaseName.ShouldBe( "histoDB" );
        a.Databases.Single().AutoCreate.ShouldBeTrue();
        a.GlobalResolution.ShouldBeTrue();
        a.IgnoreMissingDependencyIsError.ShouldBeFalse();
        new SqlServerAspect( null ).DefaultDatabaseConnectionString.ShouldBeNull();
    }

    [TestCase( """{ "DefaultConnectionString": "x" }""", "Unknown \"DefaultConnectionString\"" )]
    [TestCase( """{ "Databases": { "h": { "AutoCreate": true } } }""", "\"Databases:h:ConnectionString\" is required." )]
    [TestCase( """{ "Databases": { "h": { "ConnectionString": "x", "Size": 1 } } }""", "Unknown \"Databases:h:Size\"" )]
    [TestCase( """{ "Databases": { "db": { "ConnectionString": "x" } } }""", "is the default one" )]
    [TestCase( """{ "GlobalResolution": "yes" }""", "Invalid \"GlobalResolution\"" )]
    public void an_invalid_section_throws( string json, string message )
    {
        Util.Invokable( () => new SqlServerAspect( Section( json ) ) ).ShouldThrow<CKException>().Message.ShouldContain( message );
    }

    [Test]
    public void Apply_configures_Setupable_and_SqlSetup_and_projects_the_connection_string()
    {
        var engine = new EngineConfiguration();
        var app = new AppConfiguration( "Back",
                                        "C:/Back.App",
                                        "C:/Back.Host",
                                        ImmutableConfigurationSection.CreateFromJson( "Apps", "Back", "{}" ),
                                        engine.FirstBinPath );
        var context = new AppBuilderContext( engine, [app] );
        var a = new SqlServerAspect( Section( """{ "DefaultDatabaseConnectionString": "Server=.;Database=Db" }""" ) );

        a.Apply( TestHelper.Monitor, context, [app] ).ShouldBeTrue();

        engine.Aspects.OfType<SetupableAspectConfiguration>().ShouldHaveSingleItem();
        engine.Aspects.OfType<SqlSetupAspectConfiguration>().Single().DefaultDatabaseConnectionString.ShouldBe( "Server=.;Database=Db" );
        app.ProjectedValues["SqlServer:DefaultDatabaseConnectionString"].ShouldBe( "Server=.;Database=Db" );
    }

    [Test]
    public void without_connection_string_Apply_fails()
    {
        var engine = new EngineConfiguration();
        var app = new AppConfiguration( "Back", "C:/Back.App", "C:/Back.Host", ImmutableConfigurationSection.CreateFromJson( "Apps", "Back", "{}" ), engine.FirstBinPath );
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            new SqlServerAspect( null ).Apply( TestHelper.Monitor, new AppBuilderContext( engine, [app] ), [app] ).ShouldBeFalse();
            texts.ShouldContain( t => t.Contains( "has no \"DefaultDatabaseConnectionString\"" ) );
        }
    }
}
