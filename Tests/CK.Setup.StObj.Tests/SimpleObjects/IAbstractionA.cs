using CK.Core;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    public interface IAbstractionA : IAmbiantContract
    {
        int ConstructCount { get; }
        
        void MethofOfA();
    }
}
