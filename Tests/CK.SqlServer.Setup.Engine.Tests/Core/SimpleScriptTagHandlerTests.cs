using CK.Core;
using Shouldly;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CK.Testing.SqlServerTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests;

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
        p.Expand( TestHelper.Monitor, true ).ShouldBeTrue();
        p.ScriptCount.ShouldBe( 1, "Only one script detected." );
        var s = p.SplitScript();
        s.Count.ShouldBe( 3 );

        s[0].IsScriptTag.ShouldBeFalse();
        s[0].Body.ShouldContain( "Before begin appears." );

        s[1].IsScriptTag.ShouldBeTrue();
        s[1].Body.ShouldContain( "This nested tag is ignored." );
        s[1].Body.ShouldContain( "This appears." );
        s[1].Body.ShouldContain( "This appears too." );

        s[2].IsScriptTag.ShouldBeFalse();
        s[2].Body.ShouldEndWith( "We must have this line as a the third script." );

        DumpScripts( "NestedBeginScripts", p, s );
    }

    [Test]
    public void UnbalancedScripts()
    {
        {
            var p = new SimpleScriptTagHandler( "--[beginscript]" );
            p.Expand( TestHelper.Monitor, true ).ShouldBeFalse();
        }
        {
            var p = new SimpleScriptTagHandler(
@"
--[endscript]
--[beginscript]
--[endscript]
" );
            p.Expand( TestHelper.Monitor, true ).ShouldBeFalse();
        }
        {
            var p = new SimpleScriptTagHandler(
@"
--[beginscript]
--[endscript]
--[endscript]
" );
            p.Expand( TestHelper.Monitor, true ).ShouldBeFalse();
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
            p.Expand( TestHelper.Monitor, false ).ShouldBeFalse( "There should be no scripts." );
        }
        {
            var p = new SimpleScriptTagHandler(
@"
--[beginscript]
--[endscript]
--[beginscript]
--[endscript]
" );
            p.Expand( TestHelper.Monitor, true ).ShouldBeTrue( "Multilple scripts are okay." );
            p.ScriptCount.ShouldBe( 2, "One can reject them if wanted." );
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
            p.Expand( TestHelper.Monitor, scriptAllowed: true, goInsideScriptAllowed: true ).ShouldBeTrue();
            p.ScriptCount.ShouldBe( 1, "Only one script detected." );
            var s = p.SplitScript();
            s.Count.ShouldBe( 5 );

            s.All( o => o.IsScriptTag == false )
                .ShouldBeTrue( "When GO are allowed and occur inside the begin/endscript, there is no notion of ScriptTag." );

            s[0].Body.ShouldContain( "Before" );

            s[1].Body.ShouldContain( "n°1" );
            s[2].Body.ShouldContain( "n°2" );
            s[3].Body.ShouldContain( "n°3" );
            s[4].Body.ShouldContain( "n°4" );

        }
        {
            var p = new SimpleScriptTagHandler( script1 );
            p.Expand( TestHelper.Monitor, scriptAllowed: true, goInsideScriptAllowed: false )
                .ShouldBeFalse();
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
            p.Expand( TestHelper.Monitor, scriptAllowed: true, goInsideScriptAllowed: true ).ShouldBeTrue();
            p.ScriptCount.ShouldBe( 1, "Only one script detected." );
            var s = p.SplitScript();
            s.Count.ShouldBe( 5 );
            s.Select( o => o.IsScriptTag ).All( t => t == false )
                .ShouldBeTrue( "When GO are allowed and occur inside the begin/endscript, there is no notion of ScriptTag." );

            s[0].Body.ShouldContain( "Before" );
            s[1].Body.ShouldContain( "n°1" );
            s[2].Body.ShouldContain( "n°2" );
            s[3].Body.ShouldContain( "n°3" );
            s[4].Body.ShouldContain( "n°4" );
        }
        {
            var p = new SimpleScriptTagHandler( script2 );
            p.Expand( TestHelper.Monitor, scriptAllowed: true, goInsideScriptAllowed: false )
                .ShouldBeFalse();
        }
    }

    [Test]
    public void EmptyScript()
    {
        {
            var p = new SimpleScriptTagHandler( @"" );
            p.Expand( TestHelper.Monitor, true ).ShouldBeTrue();
            var s = p.SplitScript();
            s.Count.ShouldBe( 0 );
        }
        {
            var p = new SimpleScriptTagHandler( @"    " );
            p.Expand( TestHelper.Monitor, true ).ShouldBeTrue();
            var s = p.SplitScript();
            s.Count.ShouldBe( 0 );
        }
        {
            var p = new SimpleScriptTagHandler(
@"  

go" );
            p.Expand( TestHelper.Monitor, true ).ShouldBeTrue();
            var s = p.SplitScript();
            s.Count.ShouldBe( 0 );
        }
        {
            var p = new SimpleScriptTagHandler(
@"  

go  

go 
go

" );
            p.Expand( TestHelper.Monitor, true ).ShouldBeTrue();
            var s = p.SplitScript();
            s.Count.ShouldBe( 0 );
        }
    }

    [Test]
    public void LabeledScripts()
    {
        Action<SimpleScriptTagHandler> tester = p =>
        {
            p.Expand( TestHelper.Monitor, true ).ShouldBeTrue();
            p.ScriptCount.ShouldBe( 4, "4 script tags detected." );
            var s = p.SplitScript();
            s.Count.ShouldBe( 5, "Five scripts to execute." );

            s.Take( 4 ).All( t => t.IsScriptTag == true ).ShouldBeTrue();
            s[4].IsScriptTag.ShouldBeFalse();

            String.Join( " ", s.Select( t => t.Label ?? "<null>" ) ).ShouldBe( "DOES IT WORK WELL <null>" );
            s.Select( t => t.Body ).Select( ( t, i ) => t.Contains( "n°" + (i + 1) ) ).All( o => o )
                .ShouldBeTrue();

            DumpScripts( "Labeled script", p, s );
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
            p.Expand( TestHelper.Monitor, true ).ShouldBeFalse();
        }
        {
            var p = new SimpleScriptTagHandler(
@"--[beginscript s1]
nested will be skipped.
--[beginscript s1]
inner nested.
--[endscript s1]
it should work.
--[endscript s1]
".ReplaceLineEndings() );
            p.Expand( TestHelper.Monitor, true ).ShouldBeTrue();
            var s = p.SplitScript();
            p.ScriptCount.ShouldBe( 1 );
            s[0].Body.ShouldContain(
@"nested will be skipped.
--[beginscript s1]
inner nested.
--[endscript s1]
it should work.".ReplaceLineEndings() );
            DumpScripts( "Nested labeled scripts", p, s );
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
            p.Expand( TestHelper.Monitor, true ).ShouldBeFalse();
        }
    }

    private static void DumpScripts( string testName, SimpleScriptTagHandler p, List<SimpleScriptTagHandler.Script> s )
    {
        using( TestHelper.Monitor.OpenTrace( testName ) )
        {
            TestHelper.Monitor.Trace( p.OriginalScript );
            using( TestHelper.Monitor.OpenTrace( "Result" ) )
            {
                foreach( var one in s )
                {
                    using( TestHelper.Monitor.OpenTrace( $"Script Label: {one.Label}" ) )
                    {
                        TestHelper.Monitor.Trace( one.Body );
                    }
                }
            }
        }
    }

}
