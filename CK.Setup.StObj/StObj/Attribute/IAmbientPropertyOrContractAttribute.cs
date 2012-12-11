using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    internal interface IAmbientPropertyOrContractAttribute
    {
        bool IsOptional { get; }

        bool IsOptionalDefined { get; }

        bool IsAmbientProperty { get; }
    }
}
