using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SqlActorPackage.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.CKDatabaseLocalTestHelper;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class SpecialTableTests
    {
        [Test]
        public void transformers_worked()
        {
            var a = TestHelper.AutomaticServices.GetRequiredService<ActorHome>();
            var text = a.Database.GetObjectDefinition( "CK.sActorCreate" );
            text.Should().Contain( "--FirstSpecialTable" ).And.Contain( "--SecondSpecialTable" );
        }

        [Test]
        public void special_tables_set_the_SpecialName_from_the_TableName()
        {
            var t1 = TestHelper.AutomaticServices.GetRequiredService<SpecialItemType.FirstSpecialTable>();
            var t2 = TestHelper.AutomaticServices.GetRequiredService<SpecialItemType.SecondSpecialTable>();
            t1.SpecialName.Should().Be( "First" );
            t2.SpecialName.Should().Be( "Second" );
        }
    }
}
