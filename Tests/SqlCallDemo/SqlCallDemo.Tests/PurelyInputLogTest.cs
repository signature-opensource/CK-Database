using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using CK.SqlServer.Setup;
using NUnit.Framework;
using System.Globalization;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class PurelyInputLogTest
    {
        [Test]
        public async Task async_call_simple_log()
        {
            var p = TestHelper.StObjMap.Default.Obtain<PurelyInputLogPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                await p.SimpleLog( ctx, "First async test call ever." );
                p.Database.AssertScalarEquals( "First async test call ever. - SimpleLog", "select top 1 LogText from CK.tPurelyInputLog order by Id desc" );
            }
        }

        [Test]
        public async Task async_call_with_bit_parameter()
        {
            var p = TestHelper.StObjMap.Default.Obtain<PurelyInputLogPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                await p.Log( ctx, false, "Second async test call ever." );
                p.Database.AssertScalarEquals( "Second async test call ever. - @OneMore = 0", "select top 1 LogText from CK.tPurelyInputLog order by Id desc" );
                await p.Log( ctx, true, "Second n°2 async test call ever." );
                p.Database.AssertScalarEquals( "Second n°2 async test call ever. - @OneMore = 1", "select top 1 LogText from CK.tPurelyInputLog order by Id desc" );
                await p.Log( ctx, null, "Second n°3 async test call ever." );
                p.Database.AssertScalarEquals( "Second n°3 async test call ever. - @OneMore is null", "select top 1 LogText from CK.tPurelyInputLog order by Id desc" );
            }
        }

        [Test]
        public async Task async_call_with_the_default_value_for_bit_parameter()
        {
            var p = TestHelper.StObjMap.Default.Obtain<PurelyInputLogPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                await p.LogWithDefaultBitValue( ctx, "Third async test call ever." );
                p.Database.AssertScalarEquals( "Third async test call ever. - @OneMore = 1", "select top 1 LogText from CK.tPurelyInputLog order by Id desc" );
            }
        }

        [Test]
        public void async_call_with_cancellation_token_works()
        {
            var p = TestHelper.StObjMap.Default.Obtain<PurelyInputLogPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                {
                    Task t = p.Log( ctx, false, "Testing Cancellation." );
                    t.Wait();
                    p.Database.AssertScalarEquals( "Testing Cancellation. - @OneMore = 0", "select top 1 LogText from CK.tPurelyInputLog order by Id desc" );
                }
                {
                    CancellationTokenSource source = new CancellationTokenSource();
                    source.CancelAfter( 750 );
                    Task t = p.LogWait( ctx, "This one must pass.", 10, source.Token );
                    t.Wait();
                    p.Database.AssertScalarEquals( "This one must pass. - @OneMore = 1", "select top 1 LogText from CK.tPurelyInputLog order by Id desc" );
                }
                {
                    CancellationTokenSource source = new CancellationTokenSource();
                    source.CancelAfter( 100 );
                    Task t = null;
                    try
                    {
                        t = p.LogWait( ctx, "This will never be logged...", 1000, source.Token );
                        t.Wait();
                    }
                    catch( AggregateException ex )
                    {
                        Assert.That( ex.InnerException is SqlException );
                        // Does someone has a better (yet simple) solution?
                        Assert.That( ex.InnerException.Message, 
                                        Does.EndWith( "Operation cancelled by user." )
                                        .Or.EndWith( "Opération annulée par l'utilisateur." ) );
                        TestHelper.Monitor.Info().Send( ex, "Cancellation: the inner exception is a SqlException with a message that contains 'Operation cancelled by user.' suffix." );
                    }
                    p.Database.AssertScalarEquals( "This one must pass. - @OneMore = 1", "select top 1 LogText from CK.tPurelyInputLog order by Id desc" );
                }
            }
        }

    }
}
