using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlActorPackage
{
    public interface ISecurityZoneAbstraction : IAmbientObject
    {
        bool IAmHere();
    }
}
