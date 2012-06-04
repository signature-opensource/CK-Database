using System.IO;
using NUnit.Framework;
using CK.Core;
using System;

namespace CK.Setup.Database.Tests
{
    static class TestHelper
    {
        static IDefaultActivityLogger _logger;
        static ActivityLoggerConsoleSync _console;
        static string _scriptFolder;

        static TestHelper()
        {
            _console = new ActivityLoggerConsoleSync();
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

        public static string FolderScript
        {
            get { if( _scriptFolder == null ) InitalizePaths(); return _scriptFolder; }
        }

        public static string GetScriptsFolder( string testName)
        {
            return Path.Combine( FolderScript, testName );
        }

        private static void InitalizePaths()
        {
            string p = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            // Code base is like "file:///C:/Users/Spi/Documents/Dev4/CK-Database/Output/Tests/Debug/CK.Setup.Database.Tests.DLL"
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
            // ==> Tests/CK.Setup.Database.Tests/Scripts
            _scriptFolder = Path.Combine( p, "Tests", "CK.Setup.Database.Tests", "Scripts" );
        }


    }
}
