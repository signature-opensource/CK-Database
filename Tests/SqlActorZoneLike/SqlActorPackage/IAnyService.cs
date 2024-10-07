namespace SqlActorPackage;

/// <summary>
/// This interface is registered via StObjInitialize method.
/// Any number of StObj can implement it and all instances can be harvested thanks to StObjInitialize method.
/// (see <see cref="SqlActorPackage.Basic.Package.StObjInitialize(CK.Core.IActivityMonitor, CK.Core.IStObjMap)" />).
/// </summary>
public interface IAnyService
{
    string CallService();
}
