using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlActorPackage
{
    /// <summary>
    /// This ambient contract is not implemented.
    /// It can be used as a parameter of StObjConstruct with a null default
    /// (see <see cref="SqlActorPackage.Basic.Package.StObjConstruct(IUnknownAbstraction)"/>).
    /// </summary>
    public interface IUnknownAbstraction : IAmbientContract
    {
        bool IAmHere();
    }
}
