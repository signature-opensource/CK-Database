using System.Reflection;
using CK.Core;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
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
