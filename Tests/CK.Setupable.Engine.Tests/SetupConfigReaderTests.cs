using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Setupable.Engine.Tests;

[TestFixture]
public class SetupConfigReaderTests
{
    [TestCase( "SetupConfig: {}", true, true )]
    [TestCase( "no config here", false, true )]
    [TestCase( "SetupConfig: []", true, false )]
    [TestCase( @"SetupConfig: { ""Generalization"": }", true, false )]
    [TestCase( @"SetupConfig: { ""Generalization"": ""A full name"" }", true, true )]
    [TestCase( @"SetupConfig: { ""Generalization"": ""A full name"", """" : 78 }", true, false )]
    [TestCase( @"SetupConfig: { ""Generalization"": ""A full name"", ""Unknown"" : 78 }", true, false )]
    public void simple_read( string text, bool found, bool success )
    {
        var o = new DynamicContainerItem();
        var reader = new SetupConfigReader( o );
        reader.Apply( TestHelper.Monitor, text, out bool isHere ).Should().Be( success );
        isHere.Should().Be( found );
        if( success && o.Generalization != null )
        {
            o.Generalization.FullName.Should().Be( "A full name" );
        }
    }
}
