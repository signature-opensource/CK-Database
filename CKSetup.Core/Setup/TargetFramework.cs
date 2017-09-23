using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    public enum TargetFramework
    {
        None = 0,
        Net451 = TargetRuntime.Net461 | 451,
        Net46 = TargetRuntime.Net461 | 460,
        Net461 = TargetRuntime.Net461 | 461,
        Net462 = TargetRuntime.Net462 | 462,
        Net47 = TargetRuntime.Net47 | 470,
        NetStandard10 = 10,
        NetStandard11 = 11,
        NetStandard12 = 12,
        NetStandard13 = 13,
        NetStandard14 = 14,
        NetStandard15 = 15,
        NetStandard16 = 16,
        NetStandard20 = 20,
        NetCoreApp10 = TargetRuntime.NetCoreApp10,
        NetCoreApp11 = TargetRuntime.NetCoreApp11,
        NetCoreApp20 = TargetRuntime.NetCoreApp20,
    }

}
