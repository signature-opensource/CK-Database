using CK.Core;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using SqlCallDemo.ProviderDemo;
using System;

namespace SqlCallDemo.Tests.ProviderDemo;


[TestFixture]
public class ProviderDemoTests
{
    Func<IDisposableAllCallContext> _contextOdd = () => new SqlFinalApplicationContext( 1, 3, 5 );
    Func<IDisposableAllCallContext> _contextEven = () => new SqlFinalApplicationContext( 2, 4, 6 );

    /// <summary>
    /// This service will only call methods that require a <see cref="IActorCallContext"/>.
    /// It accepts a provider of <see cref="IDisposableActorCallContext"/>.
    /// </summary>
    class ActorDependentOnlyService
    {
        readonly Func<IDisposableActorCallContext> _provider;
        readonly ProviderDemoPackage _p;

        public ActorDependentOnlyService( Func<IDisposableActorCallContext> c, ProviderDemoPackage p )
        {
            _provider = c;
            _p = p;
        }

        /// <summary>
        /// Calls sActoronly and checks the result.
        /// Returns true if the actorId is 1 (_contextOdd is used).
        /// </summary>
        /// <returns>True if ActorId is 1, false for 2.</returns>
        public bool DoSomething()
        {
            using( var ctx = _provider() )
            {
                return CallActorOnly( _p.ActorOnly( ctx ) );
            }
        }
    }

    static bool CallActorOnly( string s )
    {
        s.Should().MatchRegex( "@ActorId = (1|2)" );
        return s == "@ActorId = 1";
    }

    /// <summary>
    /// This service will call methods that require a <see cref="IActorCallContext"/> or a <see cref="ICultureCallContext"/> or 
    /// both of them thanks to the composed <see cref="IActorCultureCallContext"/>.
    /// It accepts a provider of <see cref="IDisposableActorCultureCallContext"/> and will be able to call all the required methods.
    /// </summary>
    class ActorAndCultureDependentService
    {
        readonly Func<IDisposableActorCultureCallContext> _provider;
        readonly ProviderDemoPackage _p;

        public ActorAndCultureDependentService( Func<IDisposableActorCultureCallContext> c, ProviderDemoPackage p )
        {
            _provider = c;
            _p = p;
        }

        public bool DoSomething()
        {
            using( var ctx = _provider() )
            {
                bool isOne = CallActorOnly( _p.ActorOnly( ctx ) );
                Assert.That( CallCultureOnly( _p.CultureOnly( ctx ) ), Is.EqualTo( isOne ) );
                _p.ActorCulture( ctx );
                return isOne;
            }
        }
    }

    static bool CallCultureOnly( string s )
    {
        s.Should().MatchRegex( "@CultureId = (3|4)" );
        return s == "@CultureId = 3";
    }

    [Test]
    public void using_a_provider()
    {
        var demoPackage = SharedEngine.Map.StObjs.Obtain<ProviderDemoPackage>();
        Throw.DebugAssert( demoPackage != null );

        var s1 = new ActorDependentOnlyService( _contextOdd, demoPackage );
        Assert.That( s1.DoSomething(), Is.True );

        var s2 = new ActorAndCultureDependentService( _contextOdd, demoPackage );
        Assert.That( s2.DoSomething(), Is.True );

        var s1Even = new ActorDependentOnlyService( _contextEven, demoPackage );
        Assert.That( s1Even.DoSomething(), Is.False );

        var s2Even = new ActorAndCultureDependentService( _contextEven, demoPackage );
        Assert.That( s2Even.DoSomething(), Is.False );
    }

}
