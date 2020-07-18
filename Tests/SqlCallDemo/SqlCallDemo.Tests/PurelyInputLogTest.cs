using System;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using FluentAssertions;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class PurelyInputLogTest
    {
        [Test]
        public async Task async_call_simple_log()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<PurelyInputLogPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                await p.SimpleLog( ctx, "First async test call ever." ).ConfigureAwait( false );
                p.Database.ExecuteScalar( "select top 1 LogText from CK.tPurelyInputLog order by Id desc" )
                            .Should().Be( "First async test call ever. - SimpleLog" );
            }
        }

        [Test]
        public async Task async_call_with_bit_parameter()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<PurelyInputLogPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                await p.Log( ctx, false, "Second async test call ever." ).ConfigureAwait( false );
                p.Database.ExecuteScalar( "select top 1 LogText from CK.tPurelyInputLog order by Id desc" )
                            .Should().Be( "Second async test call ever. - @OneMore = 0" );

                await p.Log( ctx, true, "Second n°2 async test call ever." ).ConfigureAwait( false );
                p.Database.ExecuteScalar( "select top 1 LogText from CK.tPurelyInputLog order by Id desc" )
                            .Should().Be( "Second n°2 async test call ever. - @OneMore = 1" );

                await p.Log( ctx, null, "Second n°3 async test call ever." ).ConfigureAwait( false );
                p.Database.ExecuteScalar( "select top 1 LogText from CK.tPurelyInputLog order by Id desc" )
                            .Should().Be( "Second n°3 async test call ever. - @OneMore is null" );
            }
        }

        [Test]
        public async Task async_call_with_the_default_value_for_bit_parameter()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<PurelyInputLogPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                await p.LogWithDefaultBitValue( ctx, "Third async test call ever." ).ConfigureAwait( false );
                p.Database.ExecuteScalar( "select top 1 LogText from CK.tPurelyInputLog order by Id desc" )
                            .Should().Be( "Third async test call ever. - @OneMore = 1" );
            }
        }

        [Test]
        public void async_call_with_cancellation_token_works()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<PurelyInputLogPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                {
                    Task t = p.Log( ctx, false, "Testing Cancellation." );
                    t.Wait();
                    p.Database.ExecuteScalar( "select top 1 LogText from CK.tPurelyInputLog order by Id desc" )
                                .Should().Be( "Testing Cancellation. - @OneMore = 0" );
                }
                {
                    CancellationTokenSource source = new CancellationTokenSource();
                    source.CancelAfter( 1500 );
                    Task t = p.LogWait( ctx, "This one must pass.", 10, source.Token );
                    t.Wait();
                    p.Database.ExecuteScalar( "select top 1 LogText from CK.tPurelyInputLog order by Id desc" )
                                .Should().Be( "This one must pass. - @OneMore = 1" );
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
                        ex.InnerException.Should().BeOfType<SqlDetailedException>();
                        // Does someone has a better (yet simple) solution?
                        ex.InnerException.InnerException.Message
                               .Should().Match( m => m.EndsWith( "Operation cancelled by user." )
                                                     || m.EndsWith( "Opération annulée par l'utilisateur." ) );
                        TestHelper.Monitor.Info( "Cancellation: the inner exception is a SqlException with a message that contains 'Operation cancelled by user.' suffix.", ex );
                    }
                    p.Database.ExecuteScalar( "select top 1 LogText from CK.tPurelyInputLog order by Id desc" )
                                .Should().Be( "This one must pass. - @OneMore = 1" );
                }
            }
        }

    }
}
