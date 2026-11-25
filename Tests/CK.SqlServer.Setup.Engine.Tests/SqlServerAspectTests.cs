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
    static ImmutableConfigurationSection Section( string json )
    {
        return ImmutableConfigurationSection.CreateFromJson( "Aspects", "SqlServer", json );
    }

    static UserMessageCollector Collector() => new UserMessageCollector( new CurrentCultureInfo( new TranslationService(), NormalizedCultureInfo.CodeDefault ) );

    // Reads the model as the App builder does, then creates the aspect.
    static SqlServerAspect Create( string? json )
    {
        var section = json != null ? Section( json ) : null;
        var c = Collector();
        ConfigurationModelReader.TryRead<SqlServerConfiguration>( section, "Aspects:SqlServer", c, out var m )
            .ShouldBeTrue( string.Join( ", ", c.UserMessages.Select( x => x.Text ) ) );
        return new SqlServerAspect( m!, section );
    }

    static string[] Errors( string json )
    {
        var c = Collector();
        ConfigurationModelReader.TryRead<SqlServerConfiguration>( Section( json ), "Aspects:SqlServer", c, out _ ).ShouldBeFalse();
        return c.UserMessages.Where( m => m.Level == UserMessageLevel.Error ).Select( m => m.Text ).ToArray();
    }

    [Test]
    public void the_section_is_read()
    {
        var a = Create( """
            {
              "DefaultDatabaseConnectionString": "Server=.;Database=Db",
              "Databases": { "histoDB": { "ConnectionString": "Server=.;Database=Histo", "AutoCreate": true } },
              "GlobalResolution": true
            }
            """ ).Configuration;
        a.DefaultDatabaseConnectionString!.Value.Value.ShouldBe( "Server=.;Database=Db" );
        a.Databases.Single().Key.ShouldBe( "histoDB" );
        a.Databases.Single().Value.AutoCreate.ShouldBeTrue();
        a.GlobalResolution.ShouldBeTrue();
        a.IgnoreMissingDependencyIsError.ShouldBeFalse();
        Create( null ).Configuration.DefaultDatabaseConnectionString.ShouldBeNull();
    }

    [TestCase( """{ "DefaultConnectionString": "x" }""", "Unknown key 'Aspects:SqlServer:DefaultConnectionString'" )]
    [TestCase( """{ "Databases": { "h": { "AutoCreate": true } } }""", "'Aspects:SqlServer:Databases:h:ConnectionString' is required." )]
    [TestCase( """{ "Databases": { "h": { "ConnectionString": "x", "Size": 1 } } }""", "Unknown key 'Aspects:SqlServer:Databases:h:Size'" )]
    [TestCase( """{ "Databases": { "db": { "ConnectionString": "x" } } }""", "is the default one" )]
    [TestCase( """{ "Databases": { "h": { "ConnectionString": " " } } }""", "'Aspects:SqlServer:Databases:h:ConnectionString' must not be empty." )]
    [TestCase( """{ "GlobalResolution": "yes" }""", "'Aspects:SqlServer:GlobalResolution' = 'yes'" )]
    [TestCase( """{ "DefaultDatabaseConnectionString": { "$SECRET": "Bad Name" } }""", "a value or a secret handle" )]
    [TestCase( """{ "DefaultDatabaseConnectionString": { "$SECRET": "A", "Other": 1 } }""", "a value or a secret handle" )]
    public void an_invalid_section_is_an_error( string json, string message )
    {
        Errors( json ).ShouldContain( e => e.Contains( message ) );
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
        var a = Create( """{ "DefaultDatabaseConnectionString": "Server=.;Database=Db" }""" );

        a.Apply( TestHelper.Monitor, context, [app] ).ShouldBeTrue();

        engine.Aspects.OfType<SetupableAspectConfiguration>().ShouldHaveSingleItem();
        engine.Aspects.OfType<SqlSetupAspectConfiguration>().Single().DefaultDatabaseConnectionString.ShouldBe( "Server=.;Database=Db" );
        app.ProjectedValues["SqlServer:DefaultDatabaseConnectionString"].ShouldBe( "Server=.;Database=Db" );
    }

    [Test]
    public void a_secret_connection_string_is_resolved_for_the_setup_and_its_handle_is_projected()
    {
        var engine = new EngineConfiguration();
        var app = new AppConfiguration( "Back", "C:/Back.App", "C:/Back.Host", ImmutableConfigurationSection.CreateFromJson( "Apps", "Back", "{}" ), engine.FirstBinPath );
        var secrets = new Dictionary<string, string> { { "Back:Db", "Server=.;Database=Db" }, { "Histo", "Server=.;Database=Histo" } };
        var context = new AppBuilderContext( engine, [app], null, secrets );
        var a = Create( """
            {
              "DefaultDatabaseConnectionString": { "$SECRET": "Back:Db" },
              "Databases": { "histoDB": { "ConnectionString": { "$SECRET": "Histo" } } }
            }
            """ );
        a.Configuration.DefaultDatabaseConnectionString!.Value.SecretName.ShouldBe( "Back:Db" );
        a.Configuration.Databases.Single().Value.ConnectionString.SecretName.ShouldBe( "Histo" );

        a.Apply( TestHelper.Monitor, context, [app] ).ShouldBeTrue();

        var sql = engine.Aspects.OfType<SqlSetupAspectConfiguration>().Single();
        sql.DefaultDatabaseConnectionString.ShouldBe( "Server=.;Database=Db" );
        sql.Databases.Single().ConnectionString.ShouldBe( "Server=.;Database=Histo" );
        app.ProjectedValues.Count.ShouldBe( 1 );
        app.ProjectedValues["SqlServer:DefaultDatabaseConnectionString:$SECRET"].ShouldBe( "Back:Db" );
        app.ProjectedValueOrigins["SqlServer:DefaultDatabaseConnectionString:$SECRET"].ShouldBe( "Aspects:SqlServer:DefaultDatabaseConnectionString:$SECRET" );
    }

    [Test]
    public void a_missing_secret_fails_the_Apply()
    {
        var engine = new EngineConfiguration();
        var app = new AppConfiguration( "Back", "C:/Back.App", "C:/Back.Host", ImmutableConfigurationSection.CreateFromJson( "Apps", "Back", "{}" ), engine.FirstBinPath );
        var a = Create( """{ "DefaultDatabaseConnectionString": { "$SECRET": "Back:Db" } }""" );
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            a.Apply( TestHelper.Monitor, new AppBuilderContext( engine, [app] ), [app] ).ShouldBeFalse();
            texts.ShouldContain( t => t.Contains( "The secret 'Back:Db' is required by the build" ) );
        }
    }

    [Test]
    public void without_connection_string_Apply_fails()
    {
        var engine = new EngineConfiguration();
        var app = new AppConfiguration( "Back", "C:/Back.App", "C:/Back.Host", ImmutableConfigurationSection.CreateFromJson( "Apps", "Back", "{}" ), engine.FirstBinPath );
        using( TestHelper.Monitor.CollectTexts( out var texts ) )
        {
            Create( null ).Apply( TestHelper.Monitor, new AppBuilderContext( engine, [app] ), [app] ).ShouldBeFalse();
            texts.ShouldContain( t => t.Contains( "has no \"DefaultDatabaseConnectionString\"" ) );
        }
    }
}
