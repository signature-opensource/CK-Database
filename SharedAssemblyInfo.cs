#region Proprietary License
/*----------------------------------------------------------------------------
* This file (SharedAssemblyInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany( "Invenietis" )]
[assembly: AssemblyProduct( "CK-Database" )]
[assembly: AssemblyCopyright( "Copyright © Invenietis 2012-2015" )]
[assembly: AssemblyTrademark( "" )]
[assembly: CLSCompliant( true )]

[assembly: AssemblyVersion( "3.0.18" )]


#if DEBUG
    [assembly: AssemblyConfiguration( "Debug" )]
#else
    [assembly: AssemblyConfiguration( "Release" )]
#endif

// Added by CKReleaser.
[assembly: AssemblyInformationalVersion( "%ck-standard%" )]
