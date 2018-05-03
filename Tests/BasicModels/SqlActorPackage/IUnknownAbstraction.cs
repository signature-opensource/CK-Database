using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlActorPackage
{
    public interface IUnknownAbstraction : IAmbientContract
    {
        bool IAmHere();
    }
}
