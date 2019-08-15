using CK.Core;

namespace SqlActorPackage
{
    public interface ISecurityZoneAbstraction : IAmbientObject
    {
        bool IAmHere();
    }
}
