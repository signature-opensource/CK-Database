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
using System.Reflection;
using System.Runtime.Loader;

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
            //// Since we are in the Engine context, we can use 
            //SqlMapper.SetAbstractTypeMap( type =>
            //{
            //    var f = PocoDirectory_CK.Instance.Find( type );
            //    if( f != null ) return f.PocoClassType;
            //    return null;
            //} );

            //// If the CK.Dapper assembly is used, configures the type mapping
            //// to support IPoco through Dapper.
            //if( AssemblyLoadContext.Default.Assemblies.Any( a => a.GetName().Name == "CK.Dapper" ) )
            //{
            //    Dapper.SqlMapper.AddAbstractTypeMap( current =>
            //    {
            //        return type =>
            //        {
            //            var m = current?.Invoke( type );
            //            if( m == null )
            //            {
            //                var f = PocoDirectory_CK.Instance.Find( type );
            //                if( f != null ) m = f.PocoClassType;
            //            }
            //            return m;
            //        }
            //    } );
            //}

            var db = TestHelper.AutomaticServices.GetRequiredService<SqlDefaultDatabase>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var controller = ctx.GetConnectionController( db );
                var list = controller.Query<ISimpleInfo>( "select Name = 'Albert', Power = 42 union select Name = 'Einstein', Power = 3712;" ).ToList();
                list.Should().HaveCount( 2 );
                list[0].Name.Should().Be( "Albert" );
                list[0].Power.Should().Be( 42 );
                list[1].Name.Should().Be( "Einstein" );
                list[1].Power.Should().Be( 3712 );

                var listFromC = controller.Connection.Query<ISimpleInfo>( "select Name = 'Hip', Power = 42 union select Name = 'Hop', Power = 3712;" ).ToList();
                listFromC.Should().HaveCount( 2 );
                listFromC[0].Name.Should().Be( "Hip" );
                listFromC[0].Power.Should().Be( 42 );
                listFromC[1].Name.Should().Be( "Hop" );
                listFromC[1].Power.Should().Be( 3712 );

                var first = controller.QueryFirstOrDefault<ISimpleInfo>( "select Name = 'Albert', Power = 42 union select Name = 'Einstein', Power = 3712;" );
                first.Name.Should().Be( "Albert" );
                first.Power.Should().Be( 42 );
            }
        }

        [Test]
        public void Dapper_QueryFirstOrDefault_with_IPoco()
        {
            var db = TestHelper.AutomaticServices.GetRequiredService<SqlDefaultDatabase>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var controller = ctx.GetConnectionController( db );
                var first = controller.QueryFirstOrDefault<ISimpleInfo>( "select Name = 'Albert', Power = 42 union select Name = 'Einstein', Power = 3712;" );
                first.Name.Should().Be( "Albert" );
                first.Power.Should().Be( 42 );
            }
        }

    }
}
