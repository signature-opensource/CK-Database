using System.IO;
using NUnit.Framework;
using CK.Core;
using System;
using System.Diagnostics;

namespace CK.Setup.Database.SqlServer.Tests
{
    public class StringImpl : IDefaultActivityLoggerSink, IDisposable
    {
        StringWriter _w;

        public StringImpl()
        {
            _w = new StringWriter();
        }

        public string GetText()
        {
            return _w.ToString();
        }

        public void OnEnterLevel( LogLevel level, string text )
        {
            _w.WriteLine();
            _w.Write( level.ToString() + ": " + text );
        }

        public void OnContinueOnSameLevel( LogLevel level, string text )
        {
            _w.Write( text );
        }

        public void OnLeaveLevel( LogLevel level )
        {
            _w.Flush();
        }

        public void OnGroupOpen( DefaultActivityLogger.Group g )
        {
            Debug.Assert( _w != null );
            _w.WriteLine();
            _w.NewLine += "> ";
            _w.Write( g.GroupText );
        }

        public void OnGroupClose( DefaultActivityLogger.Group g, string conclusion )
        {
            _w.WriteLine();
            _w.Write( conclusion );
            _w.NewLine = _w.NewLine.Remove( _w.NewLine.Length - 2 );
        }

        public void Dispose()
        {
            if( _w != null )
            {
                _w.Flush();
                _w.Close();
                _w.Dispose();
            }
        }

        public override string ToString()
        {
            return _w.ToString();
        }

    }


    static class TestHelper
    {
        static string _scriptFolder;

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
            // Code base is like "file:///C:/Users/Spi/Documents/Dev4/CK-Database/Output/Tests/Debug/CK.Setup.Database.SqlServer.Tests.DLL"
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
            // ==> Tests/CK.Setup.Database.SqlServer.Tests/Scripts
            _scriptFolder = Path.Combine( p, "Tests", "CK.Setup.Database.SqlServer.Tests", "Scripts" );
        }
    }
}
