using CK.Core;
using CK.Testing;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
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
            _groups.Push( TestHelper.Monitor.UnfilteredOpenGroup( LogLevel.Info|LogLevel.IsFiltered, null, $"Running '{test.Name}'.", null ) );
        }

        public void AfterTest( ITest test )
        {
            var result = TestExecutionContext.CurrentContext.CurrentResult;
            var g = _groups.Pop();
            if( result.ResultState.Status != TestStatus.Passed )
            {
                g.ConcludeWith( () => result.ResultState.Status.ToString() );
                var message = result.Message;
                if( !string.IsNullOrWhiteSpace( message ) )
                {
                    TestHelper.Monitor.OpenError( message );
                    if( result.StackTrace != null ) TestHelper.Monitor.Trace( result.StackTrace );
                }
            }
            g.Dispose();
        }

        public ActionTargets Targets => ActionTargets.Test | ActionTargets.Suite;

    }
}
