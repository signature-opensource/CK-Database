using System.IO;
using NUnit.Framework;
using CK.Core;
using System;
using System.Diagnostics;

namespace CK.Setup.Dependency.Tests
{
    static class TestHelper
    {
        static IDefaultActivityLogger _logger;
        static ActivityLoggerConsoleSink _console;

        static TestHelper()
        {
            _console = new ActivityLoggerConsoleSink();
            _logger = DefaultActivityLogger.Create().Register( _console );
        }

        public static IActivityLogger Logger
        {
            get { return _logger; }
        }

        public static bool LogsToConsole
        {
            get { return _logger.RegisteredSinks.Contains( _console ); }
            set
            {
                if( value ) _logger.Register( _console );
                else _logger.Unregister( _console );
            }
        }
    }
}
