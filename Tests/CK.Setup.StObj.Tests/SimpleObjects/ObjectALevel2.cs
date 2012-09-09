using System.Reflection;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    // ObjectALevel2 is by default in the container of its parent: ObjectALevel1 is in PackageForABLevel1.
    public class ObjectALevel2 : ObjectALevel1
    {
        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 2 ), "ObjectA and ObjectALevel1 construct has been called." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }

    }
}
