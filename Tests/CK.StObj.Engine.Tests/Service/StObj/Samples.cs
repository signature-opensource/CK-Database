using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests.Service.StObj
{
    public static class Samples
    {
        public static int ObjectNumber;


        [StObj(ItemKind = DependentItemKindSpec.Container)]
        public class RootPackage : IAmbientContract
        {
        }

        [StObj( ItemKind = DependentItemKindSpec.Container, Container = typeof(RootPackage))]
        public class P0 : IAmbientContract
        {
        }

        [StObj( ItemKind = DependentItemKindSpec.Container, Requires = new[] { typeof( P0 ) } )]
        public class P1 : IAmbientContract
        {
        }

        public interface ISBase : IAmbientService
        {
            string Collect();
        }

        [AmbientService( typeof(P0))]
        public class SBaseLeaf : ISBase
        {
            readonly int _gNum;

            public SBaseLeaf()
            {
                _gNum = ++ObjectNumber;
            }

            public string Collect() => $"SBaseLeaf_{_gNum}";
        }

        [AmbientService( typeof( P0 ) )]
        public class SFront1 : ISBase
        {
            readonly int _gNum;
            readonly ISBase _next;

            public SFront1( ISBase next )
            {
                _next = next;
                _gNum = ++ObjectNumber;
            }

            public string Collect() => $"SFront1_{_gNum}[" + _next.Collect() + "]";
        }

        [AmbientService( typeof( P0 ) )]
        public class SOnFront1 : ISBase
        {
            readonly int _gNum;
            readonly SFront1 _next;

            public SOnFront1( SFront1 next )
            {
                _next = next;
                _gNum = ++ObjectNumber;
            }

            public string Collect() => $"SOnFront1_{_gNum}[" + _next.Collect() + "]";
        }

        [AmbientService( typeof( P1 ) )]
        public class SFront1InP1 : ISBase
        {
            readonly int _gNum;
            readonly ISBase _next;

            public SFront1InP1( ISBase next )
            {
                _next = next;
                _gNum = ++ObjectNumber;
            }

            public string Collect() => $"SFront1InP1_{_gNum}[" + _next.Collect() + "]";
        }

    }
}
