using CK.Core;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static CK.Testing.StObjEngineTestHelper;

namespace CK.StObj.Engine.Tests.Service
{
    [TestFixture]
    public class DispatcherServiceTests
    {
        public interface IServiceBase : IScopedAutoService
        {
            int CountOfThings { get; }
        }

        public class S1 : IServiceBase
        {
            public int CountOfThings => 1;
        }

        public class S2 : IServiceBase
        {
            public int CountOfThings => 2;
        }

        public class SDispatcher : IServiceBase
        {
            readonly IReadOnlyCollection<IServiceBase> _others;

            public SDispatcher( IReadOnlyCollection<IServiceBase> others )
            {
                _others = others;
            }

            public int CountOfThings => _others.Select( o => o.CountOfThings ).Sum();

        }

        [Test]
        public void simple_dispatcher()
        {
            var collector = TestHelper.CreateStObjCollector();
            collector.RegisterType( typeof( S1 ) );
            collector.RegisterType( typeof( S2 ) );
            collector.RegisterType( typeof( SDispatcher ) );
            TestHelper.GetFailedResult( collector );
            Assume.That( false, "IEnumerable<T> or IReadOnlyList<T> where T is IScoped/SingletonAutoService is not supported yet." );
        }


    }
}
