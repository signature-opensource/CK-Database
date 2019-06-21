using CK.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using static CK.Testing.StObjSetupTestHelper;

namespace CK.StObj.DependencyInjection.Tests
{
    public class StObjServiceCollectionExtensionsTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Should_Not_Map_To_Same_Reference()
        {
            var services = new ServiceCollection();
            var mappings = new Dictionary<Type, IStObjServiceClassDescriptor>
            {
                { typeof( IA ), new StubScopedServiceCallDescriptor<A>() },
                { typeof( IB ), new StubScopedServiceCallDescriptor<A>() }
            };
            foreach( var kv in mappings )
            {
                if( kv.Value.IsScoped )
                {
                    services.AddScoped( kv.Key, kv.Value.ClassType );
                }
                else
                {
                    services.AddSingleton( kv.Key, kv.Value.ClassType );
                }
            }
            var sp = services.BuildServiceProvider();

            var a = sp.GetRequiredService<IA>();
            var b = sp.GetRequiredService<IB>();
            Assert.That( ReferenceEquals( a, b ), Is.False );
        }

        [Test, TestCase( false ), TestCase( true )]
        public void Should_Map_To_Same_Reference_With_Scope( bool withImplementationRegistered )
        {
            var services = new ServiceCollection();
            var mappings = new Dictionary<Type, IStObjServiceClassDescriptor>
            {
                { typeof( IA ), new StubScopedServiceCallDescriptor<A>() },
                { typeof( IB ), new StubScopedServiceCallDescriptor<A>() }
            };
            if( withImplementationRegistered ) mappings.Add( typeof( A ), new StubScopedServiceCallDescriptor<A>() );

            StObjServiceCollectionExtensions.AddServiceSimpleMappings( services, mappings );
            var sp = services.BuildServiceProvider();

            var a = sp.GetRequiredService<IA>();
            var b = sp.GetRequiredService<IB>();
            Assert.That( ReferenceEquals( a, b ), Is.True );

            using( var scope = sp.CreateScope() )
            {
                var a_scoped = scope.ServiceProvider.GetRequiredService<IA>();
                var b_scoped = scope.ServiceProvider.GetRequiredService<IB>();
                var a_impl = scope.ServiceProvider.GetRequiredService<A>();
                Assert.That( ReferenceEquals( a_scoped, b_scoped ), Is.True );
                Assert.That( ReferenceEquals( a_scoped, a_impl ), Is.True );
                Assert.That( ReferenceEquals( b_scoped, a_impl ), Is.True );
            }
        }

        [Test, TestCase( false ), TestCase( true )]
        public void Should_Map_To_Same_Reference_With_Singleton( bool withImplementationRegistered )
        {
            var services = new ServiceCollection();
            var mappings = new Dictionary<Type, IStObjServiceClassDescriptor>
            {

                { typeof( IA ), new StubSingletonServiceCallDescriptor<A>() },
                { typeof( IB ), new StubSingletonServiceCallDescriptor<A>() }
            };
            if( withImplementationRegistered ) mappings.Add( typeof( A ), new StubSingletonServiceCallDescriptor<A>() );

            StObjServiceCollectionExtensions.AddServiceSimpleMappings( services, mappings );
            var sp = services.BuildServiceProvider();

            var a = sp.GetRequiredService<IA>();
            var b = sp.GetRequiredService<IB>();
            var a_impl = sp.GetRequiredService<A>();
            Assert.That( ReferenceEquals( a, b ), Is.True );
            Assert.That( ReferenceEquals( a, a_impl ), Is.True );
            Assert.That( ReferenceEquals( b, a_impl ), Is.True );
        }

        class StubScopedServiceCallDescriptor<T> : IStObjServiceClassDescriptor
        {
            public Type ClassType => typeof( T );

            public bool IsScoped => true;
        }

        class StubSingletonServiceCallDescriptor<T> : IStObjServiceClassDescriptor
        {
            public Type ClassType => typeof( T );

            public bool IsScoped => false;
        }


        interface IA { }
        interface IB : IA { }
        class A : IB, IA { }
    }
}
