using CK.Core;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    public interface IAbstractionA : IAmbientContract
    {
        int ConstructCount { get; }
        
        void MethofOfA();
    }
}
