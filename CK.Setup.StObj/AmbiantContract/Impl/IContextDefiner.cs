using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    internal interface IContextDefiner
    {
        Type Context { get; }
    }
}
