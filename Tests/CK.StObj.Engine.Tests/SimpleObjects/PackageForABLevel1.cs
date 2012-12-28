using System.Reflection;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    public class PackageForABLevel1 : PackageForAB
    {
        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 1 ), "PackageForAB.Construct has been called." );

            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );

            ConstructCount = ConstructCount + 1;
        }
        
    }
}
