using CK.Core;
using CK.Testing;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;


namespace CK.DB.Tests.NUnit
{
    /// <summary>
    /// Ensures that the <see cref="CK.Setup.SqlSetupAspectConfiguration"/> is availble for the <see cref="SharedEngine"/>.
    /// Makes each NUnit tests log as groups.
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    public class SqlServerConfigurationAspectAttribute : Attribute, ITestAction
    {
        public SqlServerConfigurationAspectAttribute()
        {
            TestHelper.OnlyOnce( RegisterSqlServerAspect );
        }

        static void RegisterSqlServerAspect()
        {
            SharedEngine.AutoConfigure += c => c.EnsureSqlServerConfigurationAspect();
        }

        void ITestAction.BeforeTest( ITest test ) { }

        void ITestAction.AfterTest( ITest test ) { }

        ActionTargets ITestAction.Targets => ActionTargets.Default;

    }
}
