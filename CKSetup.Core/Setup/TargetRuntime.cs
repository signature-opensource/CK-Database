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
        NetFramework461 = 1 << 16,
        NetFramework462 = 2 << 16,
        NetFramework47 = 3 << 16,
        NetCoreApp10 = 100 << 16,
        NetCoreApp11 = 101 << 16,
        NetCoreApp20 = 102 << 16,
    }

}
