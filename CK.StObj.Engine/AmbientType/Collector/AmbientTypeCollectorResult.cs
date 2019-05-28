using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Text;
using System.Reflection;
using CK.Setup;

namespace CK.Core
{
    /// <summary>
    /// Result of the <see cref="AmbientTypeCollector"/> work.
    /// </summary>
    public class AmbientTypeCollectorResult
    {
        internal AmbientTypeCollectorResult(
            ISet<Assembly> assemblies,
            IPocoSupportResult pocoSupport,
            AmbientObjectCollectorResult c,
            AmbientServiceCollectorResult s,
            AmbientTypeKindDetector typeKindDetector )
        {
            PocoSupport = pocoSupport;
            Assemblies = assemblies;
            AmbientContracts = c;
            AmbientServices = s;
            TypeKindDetector = typeKindDetector;
        }

        /// <summary>
        /// Gets all the registered Poco information.
        /// </summary>
        public IPocoSupportResult PocoSupport { get; }

        /// <summary>
        /// Gets the set of asssemblies for which at least one type has been registered.
        /// </summary>
        public ISet<Assembly> Assemblies { get; }

        /// <summary>
        /// Gets the reults for <see cref="IAmbientObject"/> objects.
        /// </summary>
        public AmbientObjectCollectorResult AmbientContracts { get; }

        /// <summary>
        /// Gets the reults for <see cref="IScopedAmbientService"/> objects.
        /// </summary>
        public AmbientServiceCollectorResult AmbientServices { get; }

        /// <summary>
        /// Gets the ambient type detector.
        /// </summary>
        public AmbientTypeKindDetector TypeKindDetector { get; }

        /// <summary>
        /// Gets whether an error exists that prevents the process to continue.
        /// </summary>
        /// <returns>
        /// False to continue the process (only warnings - or error considered as 
        /// warning - occured), true to stop remaining processes.
        /// </returns>
        public bool HasFatalError => PocoSupport == null || AmbientContracts.HasFatalError || AmbientServices.HasFatalError;

        /// <summary>
        /// Gets all the <see cref="ImplementableTypeInfo"/>: Abstract types that require a code generation
        /// that are either <see cref="IAmbientService"/> or <see cref="IAmbientObject"/>.
        /// </summary>
        public IEnumerable<ImplementableTypeInfo> TypesToImplement
        {
            get
            {
                var all = AmbientContracts.EngineMap.AllSpecializations.Select( m => m.ImplementableTypeInfo )
                            .Concat( AmbientServices.RootClasses.Select( c => c.MostSpecialized.ImplementableTypeInfo ) )
                            .Concat( AmbientServices.SubGraphRootClasses.Select( c => c.MostSpecialized.ImplementableTypeInfo ) )
                            .Where( i => i != null );
                Debug.Assert( all.GroupBy( i => i ).Where( g => g.Count() > 1 ).Any() == false, "No duplicates." );
                return all;
            }
        }

        /// <summary>
        /// Logs detailed information about discovered items.
        /// </summary>
        /// <param name="monitor">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof(monitor) );
            using( monitor.OpenTrace( $"Collector summary:" ) )
            {
                if( PocoSupport == null )
                {
                    monitor.Fatal( $"Poco support failed!" );
                }
                AmbientContracts.LogErrorAndWarnings( monitor );
                AmbientServices.LogErrorAndWarnings( monitor );
            }
        }

    }

}
