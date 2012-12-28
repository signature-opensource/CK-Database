using System.Reflection;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    // ObjectALevel2 is by default in the container of its parent's parent: ObjectALevel1 is in PackageForABLevel1
    public class ObjectALevel3 : ObjectALevel2, IAbstractionALevel3
    {
        void Construct()
        {
            Assert.That( ConstructCount, Is.EqualTo( 3 ), "ObjectA, ObjectALevel1 and ObjectALevel2 construct have been called." );
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
            ConstructCount = ConstructCount + 1;
        }

        public virtual void MethofOfALevel3()
        {
            SimpleObjectsTrace.LogMethod( MethodInfo.GetCurrentMethod() );
        }

    }
}
