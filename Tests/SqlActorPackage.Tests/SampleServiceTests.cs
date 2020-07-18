using CK.SqlServer;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using static CK.Testing.CKDatabaseLocalTestHelper;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class SampleServiceTests
    {
        [Test]
        public void scoped_service_resolution()
        {
            var sample = TestHelper.AutomaticServices.GetRequiredService<ISampleService>();
            var id = sample.CreateGroup( Guid.NewGuid().ToString() );
            id.Should().BePositive();

            // This "pollutes" the root TestHelper.AutomaticServices with the resolved scoped services.
            var sharedContext = TestHelper.AutomaticServices.GetRequiredService<ISqlCallContext>();
            sharedContext.Should().BeSameAs( TestHelper.AutomaticServices.GetRequiredService<SampleService>().SqlCallContext );
        }
    }
}
