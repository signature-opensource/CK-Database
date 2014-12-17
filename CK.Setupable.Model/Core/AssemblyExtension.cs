#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\Core\AssemblyExtension.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    public static class AssemblyExtension
    {
        static readonly ConcurrentDictionary<Assembly,IReadOnlyList<string>> _cache = new ConcurrentDictionary<Assembly, IReadOnlyList<string>>();

        /// <summary>
        /// Gets all resource names contained in the assembly (calls <see cref="Assembly.GetManifestResourceNames"/>)
        /// as a sorted ascending (thanks to <see cref="StringComparer.Ordinal"/>) cached list of strings.
        /// </summary>
        /// <param name="assembly">Assembly </param>
        /// <returns></returns>
        static public IReadOnlyList<string> GetSortedResourceNames( this Assembly assembly )
        {
            if( assembly == null ) throw new ArgumentNullException( "assembly" );
            // We don't care about duplicate computation and set. "Out of lock" Add in GetOrAdd is okay.
            return _cache.GetOrAdd( assembly, a =>
            {
                var l = a.GetManifestResourceNames();
                Array.Sort( l, StringComparer.Ordinal );
                return new CKReadOnlyListOnIList<string>( l ); 
            } );

        }
    }
}
