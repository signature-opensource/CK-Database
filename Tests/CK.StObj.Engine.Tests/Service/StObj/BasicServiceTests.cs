using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests.Service.StObj
{
    [TestFixture]
    public class BasicServiceTests
    {
        interface IServiceRegistered : IAmbientService
        {
        }

        [Test]
        public void only_IPoco_or_classes_can_be_registered()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterType( typeof( IServiceRegistered ) );
            collector.RegisteringFatalOrErrorCount.Should().Be( 1 );
        }

    }
}
