using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

using static CK.Testing.MonitorTestHelper;

namespace CK.StObj.Engine.Tests.Service.StObj
{
    public class FullServiceTests : TestsBase
    {
        /// <summary>
        /// This service is scoped: its implementation automatically injected
        /// by the B.ConfigureServices.
        /// </summary>
        public interface IAliceOrBobProvider
        {
            object Obtain( bool isAlice );
        }

        /// <summary>
        /// This is a IAmbientService that is, by default, implemented by B: it is
        /// then a ISingletonAmbientService.
        /// However, it may be implemented by a dedicated class that can perfectly be scoped as
        /// long as the new implementation either use <see cref="ReplaceAmbientServiceAttribute"/> or
        /// its constructor to supersed the initial Ambient Object implementation.
        /// </summary>
        public interface IAmbientServiceCanBeImplementedByAmbientObject : IAmbientService
        {
            void DoSometing( IActivityMonitor m );
        }

        /// <summary>
        /// This implementation, when registered, replaces the B's one.
        /// We don't even need to specify that this one is scoped because since it depends on
        /// an unknown service IAliceOrBobProvider, it is automatically considered as being a scoped service
        /// (unless IAliceOrBobProvider is explicitly registered as a Singleton).
        /// </summary>
        [ReplaceAmbientService( typeof( B ) )]
        //
        // Note that using the qualified name is valid:
        // [ReplaceAmbientService( "CK.StObj.Engine.Tests.Service.StObj.FullServiceTests+B, CK.StObj.Engine.Tests" )]
        //
        // Thanks to this you may replace implementation IF they exist in the context: the replaced target is
        // actually optional.
        //
        public class ScopedImplementation : IAmbientServiceCanBeImplementedByAmbientObject
        {
            readonly IAliceOrBobProvider _objectProvider;

            public ScopedImplementation( IAliceOrBobProvider objectProvider )
            {
                _objectProvider = objectProvider;
            }

            public void DoSometing( IActivityMonitor m )
            {
                var o = _objectProvider.Obtain( Environment.TickCount % 2 == 0 );
                m.Info( $"Based on IAliceOrBobProvider: I'm working with '{o}'." );
            }
        }

        /// <summary>
        /// This implementation, when registered, replaces the B's one, not because of
        /// any <see cref="ReplaceAmbientServiceAttribute"/> but because B appears in the
        /// constructor's parameters.
        /// Since B is singleton, nothing prevents this implementation to be singleton (this doesn't
        /// have to be spoecified).
        /// </summary>
        public class SingletonImplementation : IAmbientServiceCanBeImplementedByAmbientObject
        {
            readonly IAmbientServiceCanBeImplementedByAmbientObject _defaultImpl;

            public SingletonImplementation( B defaultImpl )
            {
                _defaultImpl = defaultImpl;
            }

            public void DoSometing( IActivityMonitor m )
            {
                m.Info( $"I'm wrapping the default B's implementation." );
                _defaultImpl.DoSometing( m );
            }
        }

        /// <summary>
        /// This must be registered in the startupService container since
        /// A.ConfigureServices uses it.
        /// </summary>
        public class TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem
        {
            public bool AlwaysUseAlice { get; set; }

            public bool EmitErrorLogSoThatConfigureServicesFails { get; set; }
        }

        /// <summary>
        /// ConfigureServices handles optional dependencies by using null parameter default.
        /// </summary>
        public interface IOptionalStartupService { }

        /// <summary>
        /// This interface is not an IAmbientObject: it will not be mapped.
        /// </summary>
        public interface IA0 { }

        /// <summary>
        /// This is an Ambient object (that extends the mere interface IA0).
        /// </summary>
        public interface IA1 : IA0, IAmbientObject { }

        /// <summary>
        /// This class implements IA1: it is an IAmbientObject and as such can participate to
        /// service configuration.
        /// </summary>
        public class A : IA1
        {
            void RegisterStartupServices( IActivityMonitor m, SimpleServiceContainer startupServices )
            {
                startupServices.GetService( typeof( SuperStartupService ) )
                    .Should().BeNull( "B depends on A: B.RegisterStartupServices is called after this one." );

                startupServices.GetService( typeof( IStObjObjectMap ) )
                    .Should().NotBeNull( "The StObj side of the map (that handles the Ambient objects) is available." );
            }

            /// <summary>
            /// Configure the services (does nothing here: this just tests the parameter
            /// injection of the startup services). 
            /// </summary>
            /// <param name="register">This one is the only required parameter. It may be marked as 'in' parameter or not.</param>
            /// <param name="ambientObjects">
            /// IStObjObjectMap is available: configuring services can rely on any IAmbientObject since they are already initialized (this
            /// is even available in the RegisterStartupServices).
            /// </param>
            /// <param name="superService">This is injected.</param>
            /// <param name="ext">This is injected.</param>
            /// <param name="optionalService">
            /// This is injected if it exists in the StartupServices: startup services can be optional.
            /// </param>
            void ConfigureServices(
                in StObjContextRoot.ServiceRegister register,
                IStObjObjectMap ambientObjects,
                SuperStartupService superService,
                TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem ext,
                IOptionalStartupService optionalService = null )
            {
                ambientObjects.Obtain<IA1>().Should().BeSameAs( this );
                superService.Should().NotBeNull();
                ext.Should().NotBeNull();
                superService.Talk( register.Monitor );
            }
        }

        public interface IB : IAmbientObject
        {
        }

        /// <summary>
        /// This startup service is registered by B. And consumed b y A.
        /// It couls also be used by B since all RegisterStartupServices are called
        /// before all ConfigureServices.
        /// </summary>
        public class SuperStartupService
        {
            readonly bool _mustFail;

            public SuperStartupService( bool mustFail )
            {
                _mustFail = mustFail;
            }

            public void Talk( IActivityMonitor m )
            {
                m.Info( "SuperStartupService is talking to you." );
                if( _mustFail ) m.Error( "But SuperStartupService has been told to fail miserably." );
            }
        }

        /// <summary>
        /// Ambient object.
        /// Note: being abstract makes this type without any constructor (a concrete type with no constructor
        /// has automatically the generated public default constructor) and this has to be handled since, normally
        /// a Service MUST have one and only one public constructor.
        /// An AmbientObject that implements a Service is an exception to this rule.
        /// </summary>
        public abstract class B : IB, IAmbientServiceCanBeImplementedByAmbientObject
        {
            void IAmbientServiceCanBeImplementedByAmbientObject.DoSometing( IActivityMonitor m )
            {
                m.Info( "I'm doing something from B." );
            }

            /// <summary>
            /// B depends on A. (Its RegisterStartupServices is called after A's one.)
            /// </summary>
            /// <param name="a">The dependency to the IA ambient object.</param>
            void StObjConstruct( IA1 a )
            {
            }

            void RegisterStartupServices( IActivityMonitor m, SimpleServiceContainer startupServices )
            {
                m.Info( $"Registering the Super Startup service." );
                bool willFail = startupServices.GetRequiredService<TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem>().EmitErrorLogSoThatConfigureServicesFails;
                startupServices.Add( new SuperStartupService( willFail ) );
            }

            class PrivateAliceOrBobProvider : IAliceOrBobProvider
            {
                public object Obtain( bool isAlice ) => isAlice ? "This is 'Alice'." : "This is 'Bob'.";
            }

            class PrivateAlwaysAliceProvider : IAliceOrBobProvider
            {
                public object Obtain( bool isAlice ) => "This is ALWAYS 'Alice'.";
            }

            void ConfigureServices( StObjContextRoot.ServiceRegister register, TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem conf )
            {
                var impl = conf.AlwaysUseAlice ? typeof( PrivateAlwaysAliceProvider ) : typeof( PrivateAliceOrBobProvider );
                register.Register( typeof( IAliceOrBobProvider ), impl, isScoped: true );
            }

        }

        [Test]
        public void startup_services_registration_on_Ambient_objects()
        {
            // Succesful run: TotallyExternalStartupService is available.
            {
                var collector = CreateStObjCollector();
                collector.RegisterType( typeof( A ) );
                collector.RegisterType( typeof( B ) );
                var startupServices = new SimpleServiceContainer();
                startupServices.Add( new TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem() );

                IReadOnlyList<ActivityMonitorSimpleCollector.Entry> logs = null;
                using( TestHelper.Monitor.CollectEntries( entries => logs = entries, LogLevelFilter.Trace, 1000 ) )
                {
                    var r = FullSuccessfulResolution( collector, startupServices );
                    var sp = r.Services.BuildServiceProvider();
                    sp.GetRequiredService<IA1>().Should().BeSameAs( sp.GetRequiredService<A>() );
                    sp.GetRequiredService<IB>().Should().BeSameAs( sp.GetRequiredService<B>() );
                    using( var scope = sp.CreateScope() )
                    {
                        sp.GetRequiredService<IA1>().Should().BeSameAs( scope.ServiceProvider.GetRequiredService<IA1>(), "Ambient object is Singleton." );
                        sp.GetRequiredService<IB>().Should().BeSameAs( scope.ServiceProvider.GetRequiredService<IB>(), "Ambient object is Singleton." );
                    }
                    // We are using here the default B's implementation.
                    sp.GetRequiredService<IAmbientServiceCanBeImplementedByAmbientObject>().DoSometing( TestHelper.Monitor );
                }
                logs.Should().NotContain( e => e.MaskedLevel >= LogLevel.Error );
                logs.Should().Contain( e => e.Text == "SuperStartupService is talking to you." );
                logs.Should().Contain( e => e.Text == "I'm doing something from B." );
            }
            // Failed (while Configuring Services): TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem is missing.
            {
                var collector = CreateStObjCollector();
                collector.RegisterType( typeof( A ) );
                collector.RegisterType( typeof( B ) );
                CheckFailureConfigurationServices( collector );
            }
        }

        [Test]
        public void Service_implemented_by_an_Ambient_object_can_be_overridden()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( ScopedImplementation ) );
            collector.RegisterType( typeof( A ) );
            collector.RegisterType( typeof( B ) );

            var startupServices = new SimpleServiceContainer();
            startupServices.Add( new TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem() );

            IReadOnlyList<ActivityMonitorSimpleCollector.Entry> logs = null;
            using( TestHelper.Monitor.CollectEntries( entries => logs = entries, LogLevelFilter.Trace, 1000 ) )
            {
                var r = FullSuccessfulResolution( collector, startupServices );
                var sp = r.Services.BuildServiceProvider();
                // We are using here the ScopedImplementation.
                var s = sp.GetRequiredService<IAmbientServiceCanBeImplementedByAmbientObject>();
                s.DoSometing( TestHelper.Monitor );
                // Just to be sure it's actually a Scoped service.
                using( var scoped = sp.CreateScope() )
                {
                    scoped.ServiceProvider.GetRequiredService<IAmbientServiceCanBeImplementedByAmbientObject>().Should().NotBeSameAs( s );
                }
            }
            logs.Should().NotContain( e => e.MaskedLevel >= LogLevel.Error );
            logs.Should().Contain( e => e.Text == "SuperStartupService is talking to you." );
            logs.Should().NotContain( e => e.Text == "I'm doing something from B." );
            logs.Should().Contain( e => e.Text == "Based on IAliceOrBobProvider: I'm working with 'This is 'Alice'.'."
                                        || e.Text == "Based on IAliceOrBobProvider: I'm working with 'This is 'Bob'.'." );
        }

        [Test]
        public void Initially_registered_StartupServices_may_be_used_as_configurator_or_options()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( ScopedImplementation ) );
            collector.RegisterType( typeof( A ) );
            collector.RegisterType( typeof( B ) );

            var startupServices = new SimpleServiceContainer();
            startupServices.Add( new TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem() { AlwaysUseAlice = true } );

            IReadOnlyList<ActivityMonitorSimpleCollector.Entry> logs = null;
            using( TestHelper.Monitor.CollectEntries( entries => logs = entries, LogLevelFilter.Trace, 1000 ) )
            {
                var r = FullSuccessfulResolution( collector, startupServices );
                var sp = r.Services.BuildServiceProvider();
                // We are using here the ScopedImplementation.
                var s = sp.GetRequiredService<IAmbientServiceCanBeImplementedByAmbientObject>();
                s.Should().BeOfType<ScopedImplementation>();
                s.DoSometing( TestHelper.Monitor );
            }
            logs.Should().NotContain( e => e.MaskedLevel >= LogLevel.Error );
            logs.Should().Contain( e => e.Text == "SuperStartupService is talking to you." );
            logs.Should().NotContain( e => e.Text == "I'm doing something from B." );
            logs.Should().Contain( e => e.Text == "Based on IAliceOrBobProvider: I'm working with 'This is ALWAYS 'Alice'.'." );
        }

        [Test]
        public void superseding_a_IAmbientObject_implemented_service_by_a_wrapper()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SingletonImplementation ) );
            collector.RegisterType( typeof( A ) );
            collector.RegisterType( typeof( B ) );

            var startupServices = new SimpleServiceContainer();
            startupServices.Add( new TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem() );

            IReadOnlyList<ActivityMonitorSimpleCollector.Entry> logs = null;
            using( TestHelper.Monitor.CollectEntries( entries => logs = entries, LogLevelFilter.Trace, 1000 ) )
            {
                var r = FullSuccessfulResolution( collector, startupServices );
                var sp = r.Services.BuildServiceProvider();
                sp.GetRequiredService<IAmbientServiceCanBeImplementedByAmbientObject>().DoSometing( TestHelper.Monitor );
                r.Services.Should().ContainSingle( s => s.ServiceType == typeof( IAmbientServiceCanBeImplementedByAmbientObject ) && s.Lifetime == ServiceLifetime.Singleton );
            }
            logs.Should().NotContain( e => e.MaskedLevel >= LogLevel.Error );
            logs.Should().Contain( e => e.Text == "SuperStartupService is talking to you." );
            logs.Should().Contain( e => e.Text == "I'm wrapping the default B's implementation." );
            logs.Should().Contain( e => e.Text == "I'm doing something from B." );
        }

        [Test]
        public void any_error_logged_during_Service_Configuration_make_AddStObjMap_returns_false()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( A ) );
            collector.RegisterType( typeof( B ) );

            var startupServices = new SimpleServiceContainer();
            startupServices.Add( new TotallyExternalStartupServiceThatActAsAConfiguratorOfTheWholeSystem() { EmitErrorLogSoThatConfigureServicesFails = true } );

            IReadOnlyList<ActivityMonitorSimpleCollector.Entry> logs = null;
            using( TestHelper.Monitor.CollectEntries( entries => logs = entries, LogLevelFilter.Trace, 1000 ) )
            {
                CheckFailureConfigurationServices( collector, startupServices );
            }
            logs.Should().Contain( e => e.MaskedLevel >= LogLevel.Error );
            logs.Should().Contain( e => e.Text == "SuperStartupService is talking to you." );
            logs.Should().Contain( e => e.Text == "But SuperStartupService has been told to fail miserably." );
        }


    }
}
