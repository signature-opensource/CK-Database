using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SqlActorPackage.Basic;
using System;
using System.Collections.Generic;
using Dapper;

using static CK.Testing.DBSetupTestHelper;
using System.Linq;

namespace SqlZonePackage.Tests
{
    public interface ISimpleInfo : IPoco
    {
        string Name { get; set; }

        int Power { get; set; }
    }

    [TestFixture]
    public class DapperAndIPocoTests
    {
        [Test]
        public void Dapper_Query_with_IPoco()
        {
            var db = TestHelper.AutomaticServices.GetRequiredService<SqlDefaultDatabase>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var controller = ctx.GetConnectionController( db );
                var list = controller.Query<ISimpleInfo>( "select Name = 'Albert', Power = 42 union select Name = 'Einstein', Power = 3712;" ).ToList();
                list.Should().HaveCount( 2 );
                list[0].Should().BeEquivalentTo( (Name: "Albert", Power: 42) );
                list[1].Should().BeEquivalentTo( (Name: "Einstein", Power: 3712) );
            }
        }

        [Test]
        public void Dapper_QueryFirstOrDefault_with_IPoco()
        {
            var db = TestHelper.AutomaticServices.GetRequiredService<SqlDefaultDatabase>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var controller = ctx.GetConnectionController( db );
                var first = controller.QuerySingleOrDefault<ISimpleInfo>( "select Name = 'Albert', Power = 42 union select Name = 'Einstein', Power = 3712;" );
                first.Should().BeEquivalentTo( (Name: "Albert", Power: 42) );
            }
        }

    }
}
