using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CK.Testing
{
    public static class StObjMapTestHelperExtensions
    {
        /// <summary>
        /// Loads a <see cref="IStObjMap"/> from existing generated assembly.
        /// Actual loading of the assembly is done only if the StObjMap is not already available.
        /// </summary>
        /// <returns>The map or null if an error occurred (the error is logged).</returns>
        public static IStObjMap LoadStObjMap( this IStObjMapTestHelper @this, string assemblyName, bool withWeakAssemblyResolver = true )
        {
            return withWeakAssemblyResolver
                        ? @this.WithWeakAssemblyResolver( () => DoLoadStObjMap( @this, assemblyName ) )
                        : DoLoadStObjMap( @this, assemblyName );
        }

        internal static IStObjMap DoLoadStObjMap( IMonitorTestHelperCore m, string assemblyName )
        {
            using( m.Monitor.OpenInfo( $"Loading StObj map from {assemblyName}." ) )
            {
                try
                {
                    var a = Assembly.Load( assemblyName );
                    return StObjContextRoot.Load( a, StObjContextRoot.DefaultStObjRuntimeBuilder, m.Monitor );
                }
                catch( Exception ex )
                {
                    m.Monitor.Error( ex );
                    return null;
                }
            }
        }
    }
}
