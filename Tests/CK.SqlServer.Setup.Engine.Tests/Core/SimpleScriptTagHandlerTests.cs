using CK.Core;
using FluentAssertions;
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
        p.Expand( TestHelper.Monitor, true ).Should().BeTrue();
        p.ScriptCount.Should().Be( 1, "Only one script detected." );
        var s = p.SplitScript();
        s.Count.Should().Be( 3 );

        s[0].IsScriptTag.Should().BeFalse();
        s[0].Body.Should().Contain( "Before begin appears." );

        s[1].IsScriptTag.Should().BeTrue();
        s[1].Body.Should().Contain( "This nested tag is ignored." )
                                .And.Contain( "This appears." )
                                .And.Contain( "This appears too." );

        s[2].IsScriptTag.Should().BeFalse();
        s[2].Body.Should().EndWith( "We must have this line as a the third script." );

        DumpScripts( "NestedBeginScripts", p, s );
    }

    [Test]
    public void UnbalancedScripts()
    {
        {
            var p = new SimpleScriptTagHandler( "--[beginscript]" );
            p.Expand( TestHelper.Monitor, true ).Should().BeFalse();
        }
        {
            var p = new SimpleScriptTagHandler(
@"
--[endscript]
--[beginscript]
--[endscript]
" );
            p.Expand( TestHelper.Monitor, true ).Should().BeFalse();
        }
        {
            var p = new SimpleScriptTagHandler(
@"
--[beginscript]
--[endscript]
--[endscript]
" );
            p.Expand( TestHelper.Monitor, true ).Should().BeFalse();
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
            p.Expand( TestHelper.Monitor, false ).Should().BeFalse( "There should be no scripts." );
        }
        {
            var p = new SimpleScriptTagHandler(
@"
--[beginscript]
--[endscript]
--[beginscript]
--[endscript]
" );
            p.Expand( TestHelper.Monitor, true ).Should().BeTrue( "Multilple scripts are okay." );
            p.ScriptCount.Should().Be( 2, "One can reject them if wanted." );
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
            p.Expand( TestHelper.Monitor, scriptAllowed: true, goInsideScriptAllowed: true ).Should().BeTrue();
            p.ScriptCount.Should().Be( 1, "Only one script detected." );
            var s = p.SplitScript();
            s.Should().HaveCount( 5 );

            s.All( o => o.IsScriptTag == false )
                .Should().BeTrue( "When GO are allowed and occur inside the begin/endscript, there is no notion of ScriptTag." );

            s[0].Body.Should().Contain( "Before" );

            s[1].Body.Should().Contain( "n°1" );
            s[2].Body.Should().Contain( "n°2" );
            s[3].Body.Should().Contain( "n°3" );
            s[4].Body.Should().Contain( "n°4" );

        }
        {
            var p = new SimpleScriptTagHandler( script1 );
            p.Expand( TestHelper.Monitor, scriptAllowed: true, goInsideScriptAllowed: false )
                .Should().BeFalse();
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
            p.Expand( TestHelper.Monitor, scriptAllowed: true, goInsideScriptAllowed: true ).Should().BeTrue();
            p.ScriptCount.Should().Be( 1, "Only one script detected." );
            var s = p.SplitScript();
            s.Should().HaveCount( 5 );
            s.Select( o => o.IsScriptTag ).All( t => t == false )
                .Should().BeTrue( "When GO are allowed and occur inside the begin/endscript, there is no notion of ScriptTag." );

            s[0].Body.Should().Contain( "Before" );
            s[1].Body.Should().Contain( "n°1" );
            s[2].Body.Should().Contain( "n°2" );
            s[3].Body.Should().Contain( "n°3" );
            s[4].Body.Should().Contain( "n°4" );
        }
        {
            var p = new SimpleScriptTagHandler( script2 );
            p.Expand( TestHelper.Monitor, scriptAllowed: true, goInsideScriptAllowed: false )
                .Should().BeFalse();
        }
    }

    [Test]
    public void EmptyScript()
    {
        {
            var p = new SimpleScriptTagHandler( @"" );
            p.Expand( TestHelper.Monitor, true ).Should().BeTrue();
            var s = p.SplitScript();
            s.Should().HaveCount( 0 );
        }
        {
            var p = new SimpleScriptTagHandler( @"    " );
            p.Expand( TestHelper.Monitor, true ).Should().BeTrue();
            var s = p.SplitScript();
            s.Should().HaveCount( 0 );
        }
        {
            var p = new SimpleScriptTagHandler(
@"  

go" );
            p.Expand( TestHelper.Monitor, true ).Should().BeTrue();
            var s = p.SplitScript();
            s.Should().HaveCount( 0 );
        }
        {
            var p = new SimpleScriptTagHandler(
@"  

go  

go 
go

" );
            p.Expand( TestHelper.Monitor, true ).Should().BeTrue();
            var s = p.SplitScript();
            s.Should().HaveCount( 0 );
        }
    }

    [Test]
    public void LabeledScripts()
    {
        Action<SimpleScriptTagHandler> tester = p =>
        {
            p.Expand( TestHelper.Monitor, true ).Should().BeTrue();
            p.ScriptCount.Should().Be( 4, "4 script tags detected." );
            var s = p.SplitScript();
            s.Count.Should().Be( 5, "Five scripts to execute." );

            s.Take( 4 ).All( t => t.IsScriptTag == true ).Should().BeTrue();
            s[4].IsScriptTag.Should().BeFalse();

            String.Join( " ", s.Select( t => t.Label ?? "<null>" ) ).Should().Be( "DOES IT WORK WELL <null>" );
            s.Select( t => t.Body ).Select( ( t, i ) => t.Contains( "n°" + (i + 1) ) ).All( o => o )
                .Should().BeTrue();

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
            p.Expand( TestHelper.Monitor, true ).Should().BeFalse();
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
            p.Expand( TestHelper.Monitor, true ).Should().BeTrue();
            var s = p.SplitScript();
            p.ScriptCount.Should().Be( 1 );
            s[0].Body.Should().Contain(
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
            p.Expand( TestHelper.Monitor, true ).Should().BeFalse();
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
