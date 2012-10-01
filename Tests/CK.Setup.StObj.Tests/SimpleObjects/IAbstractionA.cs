using CK.Core;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    public interface IAbstractionA : IAmbientContract
    {
        int ConstructCount { get; }
        
        void MethofOfA();
    }
}
