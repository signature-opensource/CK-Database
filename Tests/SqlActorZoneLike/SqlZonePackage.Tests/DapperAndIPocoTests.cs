using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Linq;
using static CK.Testing.SqlServerTestHelper;

namespace SqlZonePackage.Tests;

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

        var db = SharedEngine.AutomaticServices.GetRequiredService<SqlDefaultDatabase>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( db );
            var list = controller.Query<ISimpleInfo>( "select Name = 'Albert', Power = 42 union select Name = 'Einstein', Power = 3712;" ).ToList();
            list.Count.ShouldBe( 2 );
            list[0].Name.ShouldBe( "Albert" );
            list[0].Power.ShouldBe( 42 );
            list[1].Name.ShouldBe( "Einstein" );
            list[1].Power.ShouldBe( 3712 );

            var listFromC = controller.Connection.Query<ISimpleInfo>( "select Name = 'Hip', Power = 42 union select Name = 'Hop', Power = 3712;" ).ToList();
            listFromC.Count.ShouldBe( 2 );
            listFromC[0].Name.ShouldBe( "Hip" );
            listFromC[0].Power.ShouldBe( 42 );
            listFromC[1].Name.ShouldBe( "Hop" );
            listFromC[1].Power.ShouldBe( 3712 );

            var first = controller.QueryFirstOrDefault<ISimpleInfo>( "select Name = 'Albert', Power = 42 union select Name = 'Einstein', Power = 3712;" );
            first.Name.ShouldBe( "Albert" );
            first.Power.ShouldBe( 42 );
        }
    }

    [Test]
    public void Dapper_QueryFirstOrDefault_with_IPoco()
    {
        var db = SharedEngine.AutomaticServices.GetRequiredService<SqlDefaultDatabase>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var controller = ctx.GetConnectionController( db );
            var first = controller.QueryFirstOrDefault<ISimpleInfo>( "select Name = 'Albert', Power = 42 union select Name = 'Einstein', Power = 3712;" );
            first.Name.ShouldBe( "Albert" );
            first.Power.ShouldBe( 42 );
        }
    }

}
