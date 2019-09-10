using CK.Core;

namespace SqlActorPackage
{
    /// <summary>
    /// This real object is not implemented.
    /// It can be used as a parameter of StObjConstruct with a null default
    /// (see <see cref="SqlActorPackage.Basic.Package.StObjConstruct(IUnknownAbstraction)"/>).
    /// </summary>
    public interface IUnknownAbstraction : IRealObject
    {
        bool IAmHere();
    }
}
