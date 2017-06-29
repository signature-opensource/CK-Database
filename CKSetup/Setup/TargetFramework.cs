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
        NetFramework = 10000,
        Net451 = 10451,
        Net46 = 10460,
        Net461 = 10461,
        Net462 = 10462,
        NetStandard = 1,
        NetStandard10 = 100,
        NetStandard12 = 120,
        NetStandard13 = 130,
        NetStandard14 = 140,
        NetStandard15 = 150,
        NetStandard16 = 160
    }

    public static class TargetFrameworkExtension
    {
        static public TargetFramework BestAmong( this TargetFramework @this, IEnumerable<TargetFramework> frameworks )
        {
            var min = @this >= TargetFramework.NetFramework ? TargetFramework.NetFramework : TargetFramework.NetStandard;
            return frameworks.Where( f => f >= min ).OrderByDescending( f => f ).FirstOrDefault();
        }

        static public bool CanWorkWith( this TargetFramework @this, TargetFramework f )
        {
            return @this != TargetFramework.None && f != TargetFramework.None
                    && (@this < TargetFramework.NetFramework) == (f <= TargetFramework.NetFramework);
        }

        static public TargetFramework TryParse( string rawTargetFramework )
        {
            switch( rawTargetFramework )
            {
                case ".NETFramework,Version=v4.6.1": return TargetFramework.Net461;
                case ".NETStandard,Version=v1.3": return TargetFramework.NetStandard13;
                case ".NETStandard,Version=v1.6": return TargetFramework.NetStandard16;
            }
            return TargetFramework.None;
        }


    }
}
