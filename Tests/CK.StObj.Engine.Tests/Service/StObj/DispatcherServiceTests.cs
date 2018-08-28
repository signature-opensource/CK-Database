using CK.Core;
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
    public class DispatcherServiceTests : TestsBase
    {
        public interface IServiceBase : IAmbientService
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
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( S1 ) );
            collector.RegisterType( typeof( S2 ) );
            collector.RegisterType( typeof( SDispatcher ) );
            CheckFailure( collector );
            Assume.That( false, "IEnumerable<T> or IReadOnlyList<T> where T is IAmbientService is not supported yet." );
        }


    }
}
