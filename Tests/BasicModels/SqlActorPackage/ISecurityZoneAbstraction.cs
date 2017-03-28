using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlActorPackage
{
    public interface ISecurityZoneAbstraction : IAmbientContract
    {
        bool IAmHere();
    }
}
