using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany( "Invenietis" )]
[assembly: AssemblyProduct( "CK-Database" )]
[assembly: AssemblyCopyright( "Copyright © Invenietis 2012-2013" )]
[assembly: AssemblyTrademark( "" )]
[assembly: CLSCompliant( true )]

[assembly: AssemblyVersion( "2.0.0" )]
[assembly: AssemblyFileVersion( "2.0.0" )]

#if DEBUG
    [assembly: AssemblyConfiguration( "Debug" )]
#else
    [assembly: AssemblyConfiguration( "Release" )]
#endif
