using CK.Core;
using CK.Testing;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;


namespace CK.DB.Tests
{
    /// <summary>
    /// Ensures that the <see cref="CK.Setup.SqlSetupAspectConfiguration"/> is availble for the <see cref="SharedEngine"/>.
    /// Makes each NUnit tests log as groups.
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
    public class CKTestSupportAttribute : Attribute, ITestAction
    {
        readonly Stack<IDisposableGroup> _groups;

        public CKTestSupportAttribute()
        {
            _groups = new Stack<IDisposableGroup>();
            TestHelper.OnlyOnce( RegisterSqlServerAspect );
        }

        static void RegisterSqlServerAspect()
        {
            SharedEngine.AutoConfigure += c => c.EnsureSqlServerConfigurationAspect();
        }

        public void BeforeTest( ITest test )
        {
            _groups.Push( TestHelper.Monitor.OpenInfo( $"Running '{test.Name}'." ) );
        }

        public void AfterTest( ITest test )
        {
            _groups.Pop().Dispose();
        }

        public ActionTargets Targets => ActionTargets.Test | ActionTargets.Suite;

    }
}
