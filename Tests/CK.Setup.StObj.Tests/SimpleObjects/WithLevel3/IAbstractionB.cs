using CK.Core;

namespace CK.Setup.StObj.Tests.SimpleObjects.WithLevel3
{
    public interface IAbstractionBOnLevel2 : IAmbientContract
    {
        int ConstructCount { get; }
        
        void MethofOfBOnLevel2();
    }
}
