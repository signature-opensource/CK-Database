using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    public enum TargetRuntime
    {
        None = 0,
        Net461 = 1 << 16,
        Net462 = 2 << 16,
        Net47 = 3 << 16,
        NetCoreApp10 = 128 << 16,
        NetCoreApp11 = 129 << 16,
        NetCoreApp20 = 130 << 16,
    }

}
