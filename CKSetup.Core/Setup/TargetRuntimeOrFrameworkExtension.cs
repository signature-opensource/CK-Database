using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    public static class TargetRuntimeOrFrameworkExtension
    {

        /// <summary>
        /// Gets the runtimes that are common to multiple frameworks.
        /// </summary>
        /// <param name="this">This framework.</param>
        /// <param name="others">Other frameworks.</param>
        /// <returns>The common runtimes.</returns>
        static public IEnumerable<TargetRuntime> GetCommonRuntimes( this TargetFramework @this, IEnumerable<TargetFramework> others )
        {
            var intersect = new HashSet<TargetRuntime>( @this.GetAllowedRuntimes() );
            foreach( var f in others )
            {
                intersect.IntersectWith( f.GetAllowedRuntimes() );
            }
            return intersect;
        }

        static TargetRuntime[] NetFramework461Runtimes = new TargetRuntime[] { TargetRuntime.NetFramework461, TargetRuntime.NetFramework462, TargetRuntime.NetFramework47 };
        static TargetRuntime[] NetFramework462Runtimes = new TargetRuntime[] { TargetRuntime.NetFramework461, TargetRuntime.NetFramework462 };
        static TargetRuntime[] NetFramework47Runtimes = new TargetRuntime[] { TargetRuntime.NetFramework47 };
        static TargetRuntime[] NetStandard16Runtimes = new TargetRuntime[] { TargetRuntime.NetFramework461, TargetRuntime.NetFramework462, TargetRuntime.NetFramework47, TargetRuntime.NetCoreApp10, TargetRuntime.NetCoreApp11, TargetRuntime.NetCoreApp20 };
        static TargetRuntime[] NetStandard20Runtimes = new TargetRuntime[] { TargetRuntime.NetFramework461, TargetRuntime.NetFramework462, TargetRuntime.NetFramework47, TargetRuntime.NetCoreApp20 };
        static TargetRuntime[] NetCoreApp10Runtimes = new TargetRuntime[] { TargetRuntime.NetCoreApp10 };
        static TargetRuntime[] NetCoreApp11Runtimes = new TargetRuntime[] { TargetRuntime.NetCoreApp11 };
        static TargetRuntime[] NetCoreApp20Runtimes = new TargetRuntime[] { TargetRuntime.NetCoreApp20 };

        /// <summary>
        /// Gets the different <see cref="TargetRuntime"/> that can handle a <see cref="TargetFramework"/>.
        /// </summary>
        /// <param name="this">This framexork.</param>
        /// <returns>The compatible runtimes.</returns>
        static public IReadOnlyList<TargetRuntime> GetAllowedRuntimes( this TargetFramework @this )
        {
            switch( @this )
            {
                case TargetFramework.Net451:
                case TargetFramework.Net46:
                case TargetFramework.Net461: return NetFramework461Runtimes;
                case TargetFramework.Net462: return NetFramework462Runtimes;
                case TargetFramework.Net47: return NetFramework47Runtimes;
                case TargetFramework.NetStandard10:
                case TargetFramework.NetStandard11:
                case TargetFramework.NetStandard12:
                case TargetFramework.NetStandard13:
                case TargetFramework.NetStandard14:
                case TargetFramework.NetStandard15:
                case TargetFramework.NetStandard16: return NetStandard16Runtimes;
                case TargetFramework.NetStandard20: return NetStandard20Runtimes;
                case TargetFramework.NetCoreApp10: return NetCoreApp10Runtimes;
                case TargetFramework.NetCoreApp11: return NetCoreApp11Runtimes;
                case TargetFramework.NetCoreApp20: return NetCoreApp20Runtimes;
                default: return Array.Empty<TargetRuntime>();
            }
        }

        /// <summary>
        /// Gets whether a <see cref="TargetFramework"/> is compatible with a <see cref="TargetRuntime"/>.
        /// </summary>
        /// <param name="this">This framework.</param>
        /// <param name="r">The runtime.</param>
        /// <returns>True if the framework can run on the given runtime.</returns>
        static public bool CanWorkOn( this TargetFramework @this, TargetRuntime r )
        {
            return GetAllowedRuntimes( @this ).Contains( r );
        }

        /// <summary>
        /// Parses the TargetFrameworkAttribute (from System.Runtime.Versioning).
        /// </summary>
        /// <param name="rawTargetFramework">
        /// String to parse (like ".NETFramework,Version=v4.7", ".NETStandard,Version=v1.6", or ".NETCoreApp,Version=v2.0"").</param>
        /// <returns>The target framework or <see cref="TargetFramework.None"/> if it is not known.</returns>
        static public TargetFramework TryParse( string rawTargetFramework )
        {
            switch( rawTargetFramework )
            {
                case ".NETFramework,Version=v4.5.1": return TargetFramework.Net451;
                case ".NETFramework,Version=v4.6.1": return TargetFramework.Net461;
                case ".NETFramework,Version=v4.6.2": return TargetFramework.Net462;
                case ".NETFramework,Version=v4.7": return TargetFramework.Net47;
                case ".NETStandard,Version=v1.0": return TargetFramework.NetStandard10;
                case ".NETStandard,Version=v1.1": return TargetFramework.NetStandard11;
                case ".NETStandard,Version=v1.2": return TargetFramework.NetStandard12;
                case ".NETStandard,Version=v1.3": return TargetFramework.NetStandard13;
                case ".NETStandard,Version=v1.4": return TargetFramework.NetStandard14;
                case ".NETStandard,Version=v1.5": return TargetFramework.NetStandard15;
                case ".NETStandard,Version=v1.6": return TargetFramework.NetStandard16;
                case ".NETStandard,Version=v2.0": return TargetFramework.NetStandard20;
                case ".NETCoreApp,Version=v1.0": return TargetFramework.NetCoreApp10;
                case ".NETCoreApp,Version=v1.1": return TargetFramework.NetCoreApp11;
                case ".NETCoreApp,Version=v2.0": return TargetFramework.NetCoreApp20;
            }
            return TargetFramework.None;
        }


    }
}
