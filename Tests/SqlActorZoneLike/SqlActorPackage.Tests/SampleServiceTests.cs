using CK.SqlServer;
using CK.Testing;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;

namespace SqlActorPackage.Tests;

[TestFixture]
public class SampleServiceTests
{
    [Test]
    public void scoped_service_resolution()
    {
        var sample = SharedEngine.AutomaticServices.GetRequiredService<ISampleService>();
        var id = sample.CreateGroup( Guid.NewGuid().ToString() );
        id.ShouldBePositive();

        // This "pollutes" the root TestHelper.AutomaticServices with the resolved scoped services.
        var sharedContext = SharedEngine.AutomaticServices.GetRequiredService<ISqlCallContext>();
        sharedContext.ShouldBeSameAs( SharedEngine.AutomaticServices.GetRequiredService<SampleService>().SqlCallContext );
    }
}
