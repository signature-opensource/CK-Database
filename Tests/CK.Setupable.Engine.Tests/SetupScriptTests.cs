using CK.Core;
using CK.Setup;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setupable.Engine.Tests
{
    [TestFixture]
    public class SetupScriptTests
    {

        ScriptsCollection GetSetupScripts( bool withAlwaysScript )
        {
            var X = new ContextLocName( "", "", "X" );
            var c = new ScriptsCollection();

            var s1 = SourceCodeSetupScript.CreateFromSourceCode( X, $"Script 1.0.0", "sql", SetupCallGroupStep.Init, null, new Version( 1, 0, 0 ) );
            c.Add( TestHelper.ConsoleMonitor, s1 );

            for( int i = 0; i < 10; ++i )
            {
                Version fromVersion = new Version( 1, 0, i );
                Version version = new Version( 1, 0, i + 1 );
                var s = SourceCodeSetupScript.CreateFromSourceCode( X, $"{i - 1} => {i}", "sql", SetupCallGroupStep.Init, fromVersion, version );
                c.Add( TestHelper.ConsoleMonitor, s );
            }
            var s10to2 = SourceCodeSetupScript.CreateFromSourceCode( X, "1.0.10 => 2.0.0", "sql", SetupCallGroupStep.Init, new Version( 1, 0, 10 ), new Version( 2, 0, 0 ) );
            c.Add( TestHelper.ConsoleMonitor, s10to2 );

            var s100to105 = SourceCodeSetupScript.CreateFromSourceCode( X, "1.0.0 => 1.0.5", "sql", SetupCallGroupStep.Init, new Version( 1, 0, 0 ), new Version( 1, 0, 5 ) );
            c.Add( TestHelper.ConsoleMonitor, s100to105 );

            var s107to109 = SourceCodeSetupScript.CreateFromSourceCode( X, "1.0.7 => 1.0.9", "sql", SetupCallGroupStep.Init, new Version( 1, 0, 7 ), new Version( 1, 0, 9 ) );
            c.Add( TestHelper.ConsoleMonitor, s107to109 );

            var s2 = SourceCodeSetupScript.CreateFromSourceCode( X, " Script 2.0.0", "sql", SetupCallGroupStep.Init, null, new Version( 2, 0, 0 ) );
            c.Add( TestHelper.ConsoleMonitor, s2 );

            if( withAlwaysScript )
            {
                var always = SourceCodeSetupScript.CreateFromSourceCode( X, $"Always", "sql", SetupCallGroupStep.Init, null, null );
                c.Add( TestHelper.ConsoleMonitor, always );
            }
            return c;
        }

        [TestCase( null, "2.0.0", "2.0.0" )]
        [TestCase( null, "1.0.7", "1.0.0,1.0.0.to.1.0.5,1.0.5.to.1.0.6,1.0.6.to.1.0.7" )]
        [TestCase( "1.0.4", "1.0.7", "1.0.4.to.1.0.5,1.0.5.to.1.0.6,1.0.6.to.1.0.7" )]
        [TestCase( "1.0.0", "1.0.5", "1.0.0.to.1.0.5" )]
        [TestCase( "1.0.5", "2.0.0", "1.0.5.to.1.0.6,1.0.6.to.1.0.7,1.0.7.to.1.0.9,1.0.9.to.1.0.10,1.0.10.to.2.0.0" )]
        public void check_vector_script( string from, string to, string results )
        {
            var vFrom = from == null ? null : new Version( from );
            var vTo = to == null ? null : new Version( to );
            var vResults = results.Split( ',' );

            var scripts = GetSetupScripts( false );

            var vector = scripts.GetScriptVector( SetupCallGroupStep.Init, vFrom, vTo );
            var vectorResult = vector.Scripts.Select( cv => VersionedName( cv.Script ) );

            Assert.That( vectorResult.Count(), Is.EqualTo( vResults.Length ) );
            for( int i = 0; i < vResults.Length; ++ i )
            {
                Assert.That( vectorResult.ElementAt(i), Is.EqualTo( vResults[i] ) );
            }

            string VersionedName( ISetupScript s )
            {
                var n = s.Name;
                if( n.FromVersion != null && n.Version != null )
                {
                    return $"{n.FromVersion}.to.{n.Version}";
                }
                if( n.Version != null ) return n.Version.ToString();
                return "always";
            }
        }

    }
}
