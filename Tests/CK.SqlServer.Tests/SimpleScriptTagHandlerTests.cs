using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Diagnostics;
using CK.Core;

namespace CK.SqlServer.Tests
{
    [TestFixture]
    public class SimpleScriptTagHandlerTests
    {
        [Test]
        public void NestedBeginScripts()
        {
            var p = new SimpleScriptTagHandler(
@"Before begin appears.
--[beginscript]
This nested tag is ignored.
--[beginscript]
This appears.
--[endscript]
This appears too.
--[endscript]
We must have this line as a the third script.
" );
            Assert.That( p.Expand( TestHelper.Logger, true ) );
            Assert.That( p.ScriptCount, Is.EqualTo( 1 ), "Only one script detected." );
            var s = p.SplitScript();
            Assert.That( s.Count, Is.EqualTo( 3 ) );
            
            Assert.That( s[0].IsScriptTag, Is.False );
            Assert.That( s[0].Body, Is.StringContaining( "Before begin appears." ) );

            Assert.That( s[1].IsScriptTag );
            Assert.That( s[1].Body, Is.StringContaining( "This nested tag is ignored." )
                                    .And.StringContaining( "This appears." )
                                    .And.StringContaining( "This appears too." ) );
            
            Assert.That( s[2].IsScriptTag, Is.False );
            Assert.That( s[2].Body, Is.EqualTo( "We must have this line as a the third script." ) );
        }

        [Test]
        public void UnbalancedScripts()
        {
            {
                var p = new SimpleScriptTagHandler( "--[beginscript]" );
                Assert.That( p.Expand( TestHelper.Logger, true ), Is.False );
            }            
            {
                var p = new SimpleScriptTagHandler(
    @"
--[endscript]
--[beginscript]
--[endscript]
" );
                Assert.That( p.Expand( TestHelper.Logger, true ), Is.False );
            }
            {
                var p = new SimpleScriptTagHandler(
@"
--[beginscript]
--[endscript]
--[endscript]
" );
                Assert.That( p.Expand( TestHelper.Logger, true ), Is.False );
            }

        }
        
        [Test]
        public void ForbiddenOrMultipleScripts()
        {
            {
                var p = new SimpleScriptTagHandler(
    @"
--[beginscript]
--[endscript]
" );
                Assert.That( p.Expand( TestHelper.Logger, false ), Is.False, "There should be no scripts." );
            }
            {
                var p = new SimpleScriptTagHandler(
    @"
--[beginscript]
--[endscript]
--[beginscript]
--[endscript]
" );
                Assert.That( p.Expand( TestHelper.Logger, true ), Is.True, "Multilple scripts are okay." );
                Assert.That( p.ScriptCount, Is.EqualTo( 2 ), "One can reject them if wanted." );
            }
        }

        [Test]
        public void ScriptsWithGoInside()
        {
            string script1 = @"
    Before...
    --[beginscript]
    n°1
go
    n°2
go  n°3
    --[endscript]
    n°4
";
            {
                var p = new SimpleScriptTagHandler( script1 );
                Assert.That( p.Expand( TestHelper.Logger, scriptAllowed: true, goInsideScriptAllowed: true ) );
                Assert.That( p.ScriptCount, Is.EqualTo( 1 ), "Only one script detected." );
                var s = p.SplitScript();
                Assert.That( s.Count, Is.EqualTo( 5 ) );
                
                Assert.That( s.Select( o => o.IsScriptTag ).All( t => t == false ), "When GO are allowed and occur inside the begin/endscript, there is no notion of ScriptTag." );
                
                Assert.That( s[0].Body, Is.StringContaining( "Before" ) );
                
                Assert.That( s[1].Body, Is.StringContaining( "n°1" ) );
                Assert.That( s[2].Body, Is.StringContaining( "n°2" ) );
                Assert.That( s[3].Body, Is.StringContaining( "n°3" ) );

                Assert.That( s[4].Body, Is.StringContaining( "n°4" ) );
            }
            {
                var p = new SimpleScriptTagHandler( script1 );
                Assert.That( p.Expand( TestHelper.Logger, scriptAllowed: true, goInsideScriptAllowed: false ), Is.False );
            }

            string script2 = @"
    Before...
    --[beginscript]
    n°1
go
go   

  
go
    n°2
go  n°3
    --[endscript]
go
    n°4
";
            {
                var p = new SimpleScriptTagHandler( script2 );
                Assert.That( p.Expand( TestHelper.Logger, scriptAllowed: true, goInsideScriptAllowed: true ) );
                Assert.That( p.ScriptCount, Is.EqualTo( 1 ), "Only one script detected." );
                var s = p.SplitScript();
                Assert.That( s.Count, Is.EqualTo( 5 ) );
                Assert.That( s.Select( o => o.IsScriptTag ).All( t => t == false ), "When GO are allowed and occur inside the begin/endscript, there is no notion of ScriptTag." );
                
                Assert.That( s[0].Body, Is.StringContaining( "Before" ) );
                Assert.That( s[1].Body, Is.StringContaining( "n°1" ) );
                Assert.That( s[2].Body, Is.StringContaining( "n°2" ) );
                Assert.That( s[3].Body, Is.StringContaining( "n°3" ) );
                Assert.That( s[4].Body, Is.StringContaining( "n°4" ) );
            }
            {
                var p = new SimpleScriptTagHandler( script2 );
                Assert.That( p.Expand( TestHelper.Logger, scriptAllowed: true, goInsideScriptAllowed: false ), Is.False );
            }
        }

        [Test]
        public void EmptyScript()
        {
            {
                var p = new SimpleScriptTagHandler( @"" );
                Assert.That( p.Expand( TestHelper.Logger, true ) );
                var s = p.SplitScript();
                Assert.That( s.Count, Is.EqualTo( 0 ) );
            }
            {
                var p = new SimpleScriptTagHandler( @"    " );
                Assert.That( p.Expand( TestHelper.Logger, true ) );
                var s = p.SplitScript();
                Assert.That( s.Count, Is.EqualTo( 0 ) );
            }
            {
                var p = new SimpleScriptTagHandler( 
@"  

go" );
                Assert.That( p.Expand( TestHelper.Logger, true ) );
                var s = p.SplitScript();
                Assert.That( s.Count, Is.EqualTo( 0 ) );
            }
            {
                var p = new SimpleScriptTagHandler( 
@"  

go  

go 
go

" );
                Assert.That( p.Expand( TestHelper.Logger, true ) );
                var s = p.SplitScript();
                Assert.That( s.Count, Is.EqualTo( 0 ) );
            }
        }

        [Test]
        public void LabeledScripts()
        {
            Action<SimpleScriptTagHandler> tester = p =>
            {
                Assert.That( p.Expand( TestHelper.Logger, true ) );
                Assert.That( p.ScriptCount, Is.EqualTo( 4 ), "4 script tags detected." );
                var s = p.SplitScript();
                Assert.That( s.Count, Is.EqualTo( 5 ), "Five scripts to execute." );

                Assert.That( s.Take( 4 ).All( t => t.IsScriptTag == true ) && s[4].IsScriptTag == false );

                Assert.That( String.Join( " ", s.Select( t => t.Label ?? "<null>" ) ), Is.EqualTo( "DOES IT WORK WELL <null>" ) );
                Assert.That( s.Select( t => t.Body ).Select( ( t, i ) => t.Contains( "n°" + (i + 1) ) ).All( o => o ) );
            };

            tester( new SimpleScriptTagHandler( @"
--[beginscript DOES]
n°1
--[endscript]
--[beginscript IT]
n°2
--[endscript]
--[beginscript WORK]
n°3
--[endscript]
--[beginscript WELL]
n°4
--[endscript]
n°5
" ) );

            tester( new SimpleScriptTagHandler( @"
--[beginscript DOES]
n°1
--[endscript DOES]
--[beginscript IT]
n°2
--[endscript IT]
--[beginscript WORK]
n°3
--[endscript WORK]
--[beginscript WELL]
n°4
--[endscript WELL]
n°5
" ) );
        }

        [Test]
        public void UnbalancedLabeledScripts()
        {
            {
                var p = new SimpleScriptTagHandler( @"
--[beginscript s1]
n°1
--[endscript s2]
" );
                Assert.That( p.Expand( TestHelper.Logger, true ), Is.False );
            }
            {
                var p = new SimpleScriptTagHandler( @"
--[beginscript s1]
nested will be skipped.
--[beginscript s1]
inner nested.
--[endscript s1]
it should work.
--[endscript s1]
" );
                Assert.That( p.Expand( TestHelper.Logger, true ), Is.True );
                var s = p.SplitScript();
                Assert.That( p.ScriptCount, Is.EqualTo( 1 ) );
                Assert.That( s[0].Body, Is.StringContaining( @"
nested will be skipped.
--[beginscript s1]
inner nested.
--[endscript s1]
it should work." ) );
            }
            {
                var p = new SimpleScriptTagHandler( @"
--[beginscript s1]
n°1
--[endscript s1]
--[beginscript s1]
n°1
--[endscript s1]
" );
                Assert.That( p.Expand( TestHelper.Logger, true ), Is.False );
            }
        }

    }
}
