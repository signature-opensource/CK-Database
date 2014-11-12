using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany( "Invenietis" )]
[assembly: AssemblyProduct( "CK-Database" )]
[assembly: AssemblyCopyright( "Copyright © Invenietis 2012-2014" )]
[assembly: AssemblyTrademark( "" )]
[assembly: CLSCompliant( true )]

[assembly: AssemblyVersion( "3.0.0" )]


#if DEBUG
    [assembly: AssemblyConfiguration( "Debug" )]
#else
    [assembly: AssemblyConfiguration( "Release" )]
#endif

// Added by CKReleaser.
[assembly: AssemblyInformationalVersion( "%ck-standard%" )]
