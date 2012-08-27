using System.Reflection;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    [StObj( Container = typeof( PackageForABLevel1 ) )]
    public class ObjectBLevel1 : ObjectB
    {
        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 1 ), "ObjectB.Construct has been called." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }
    }
}
