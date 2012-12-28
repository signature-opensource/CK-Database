using System.Reflection;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    [StObj( ItemKind = DependentItemKindSpec.Container )]
    public class PackageForAB : IAmbientContract
    {
        public int ConstructCount { get; protected set; }

        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 0 ), "First construct." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }
        
    }
}
