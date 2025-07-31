using CK.Core;
using CK.Testing;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using static CK.Testing.MonitorTestHelper;
using CK.Setup;
using CK.Cris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CK.DB.Tests.NUnit;

/// <summary>
/// Ensures that the <see cref="CK.Setup.SqlSetupAspectConfiguration"/> is available for the <see cref="SharedEngine"/>.
/// </summary>
[AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
public class SqlServerConfigurationAspectAndCrisExecutionContext : Attribute, ITestAction
{
    public SqlServerConfigurationAspectAndCrisExecutionContext()
    {
        TestHelper.OnlyOnce( RegisterSqlServerAspectAndCrisExecutionContext );
    }

    static void RegisterSqlServerAspectAndCrisExecutionContext()
    {
        SharedEngine.AutoConfigure += c =>
        {
            c.EnsureSqlServerConfigurationAspect();
            c.GlobalTypes.Add( typeof( CrisExecutionContext ) );
        };
        SharedEngine.AutoConfigureServices += s =>
        {
            // We use TryAdd here because we are in a test context: if
            // other similar hooks wants to make a IActivityMonitor aware
            // SharedEngine bound to another monitor than the TestHelper.Monitor
            // they can, even if they comes before this one.
            s.TryAddScoped<IActivityMonitor>( sp => TestHelper.Monitor );
            s.TryAddScoped<IParallelLogger>( sp => TestHelper.Monitor.ParallelLogger );
        };
    }

    void ITestAction.BeforeTest( ITest test ) { }

    void ITestAction.AfterTest( ITest test ) { }

    ActionTargets ITestAction.Targets => ActionTargets.Default;

}
