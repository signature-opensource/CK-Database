using System.IO;
using NUnit.Framework;
using CK.Core;
using System;
using System.Diagnostics;

namespace CK.SqlServer.Tests
{
    static class TestHelper
    {
        static IDefaultActivityLogger _logger;
        static ActivityLoggerConsoleSink _console;
        static string _scriptFolder;

        static TestHelper()
        {
            _console = new ActivityLoggerConsoleSink();
            _logger = new DefaultActivityLogger();
            _logger.Tap.Register( _console );
        }

        public static IActivityLogger Logger
        {
            get { return _logger; }
        }

        public static bool LogsToConsole
        {
            get { return _logger.Tap.RegisteredSinks.Contains( _console ); }
            set
            {
                if( value ) _logger.Tap.Register( _console );
                else _logger.Tap.Unregister( _console );
            }
        }

        public static string FolderScript
        {
            get { if( _scriptFolder == null ) InitalizePaths(); return _scriptFolder; }
        }

        public static string GetScriptsFolder( string testName )
        {
            return Path.Combine( FolderScript, testName );
        }
        
        private static void InitalizePaths()
        {
            string p = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            // Code base is like "file:///C:/Users/Spi/Documents/Dev4/CK-Database/Output/Tests/Debug/CK.Setup.SqlServer.Tests.DLL"
            StringAssert.StartsWith( "file:///", p, "Code base must start with file:/// protocol." );

            p = p.Substring( 8 ).Replace( '/', System.IO.Path.DirectorySeparatorChar );

            // => Debug/
            p = Path.GetDirectoryName( p );
            // => Tests/
            p = Path.GetDirectoryName( p );
            // => Output/
            p = Path.GetDirectoryName( p );
            // => CK-Database/
            p = Path.GetDirectoryName( p );
            // ==> Tests/CK.SqlServer.Tests/Scripts
            _scriptFolder = Path.Combine( p, "Tests", "CK.SqlServer.Tests", "Scripts" );
        }

    }
}
