using CK.Core;
using CK.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SqlActorPackage.Basic;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class SpecialTableTests
    {
        [Test]
        public void transformers_worked()
        {
            var a = SharedEngine.AutomaticServices.GetRequiredService<ActorHome>();
            var text = a.Database.GetObjectDefinition( "CK.sActorCreate" );
            text.Should().Contain( "--FirstSpecialTable" ).And.Contain( "--SecondSpecialTable" );
        }

        [Test]
        public void special_tables_set_the_SpecialName_from_the_TableName()
        {
            var t1 = SharedEngine.AutomaticServices.GetRequiredService<SpecialItemType.FirstSpecialTable>();
            var t2 = SharedEngine.AutomaticServices.GetRequiredService<SpecialItemType.SecondSpecialTable>();
            t1.SpecialName.Should().Be( "First" );
            t2.SpecialName.Should().Be( "Second" );
        }
    }
}
