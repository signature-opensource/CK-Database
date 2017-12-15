using CK.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CK.Testing
{
    public static class MonitorTestHelperExtensions
    {
        /// <summary>
        /// Runs code inside a standard "weak assembly resolver".
        /// </summary>
        /// <param name="this">This test helper.</param>
        /// <param name="action">The action. Must not be null.</param>
        /// <returns>The action result.</returns>
        public static T WithWeakAssemblyResolver<T>( this IMonitorTestHelperCore @this, Func<T> action )
        {
            if( action == null ) throw new ArgumentNullException( nameof( action ) );
            ResolveEventHandler loadHook = ( sender, arg ) =>
            {
                var failed = new AssemblyName( arg.Name );
                var resolved = failed.Version != null && string.IsNullOrWhiteSpace( failed.CultureName )
                        ? Assembly.Load( new AssemblyName( failed.Name ) )
                        : null;
                //@this.Monitor.Info( $"Assembly load conflict: {arg.Name} => {(resolved != null ? resolved.FullName : "(null)")}" );
                return resolved;
            };
            AppDomain.CurrentDomain.AssemblyResolve += loadHook;
            try
            {
                return action();
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= loadHook;
            }
        }
    }
}
