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
    public class AmbientContractCollectorResult
    {
        IReadOnlyList<IReadOnlyList<MutableItem>> _concreteClassesPath;
        IReadOnlyList<IReadOnlyList<Type>> _classAmbiguities;
        IReadOnlyList<IReadOnlyList<Type>> _interfaceAmbiguities;
        IReadOnlyList<Type> _abstractTails;

        internal AmbientContractCollectorResult(
            StObjObjectEngineMap mappings,
            IReadOnlyList<IReadOnlyList<MutableItem>> concreteClasses,
            IReadOnlyList<IReadOnlyList<Type>> classAmbiguities,
            IReadOnlyList<IReadOnlyList<Type>> interfaceAmbiguities,
            IReadOnlyList<Type> abstractTails )
        {
            EngineMap = mappings;
            _concreteClassesPath = concreteClasses;
            _classAmbiguities = classAmbiguities;
            _interfaceAmbiguities = interfaceAmbiguities;
            _abstractTails = abstractTails;
        }

        /// <summary>
        /// Gets the internal mappings.
        /// </summary>
        internal StObjObjectEngineMap EngineMap { get; }

        /// <summary>
        /// Gets all the paths from <see cref="IAmbientContract"/> base classes to their most specialized concrete classes 
        /// that this context contains.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<IStObjMutableItem>> ConcreteClasses => _concreteClassesPath; 

        /// <summary>
        /// Gets all the class ambiguities: the first type corresponds to more than one following concrete specializations.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Type>> ClassAmbiguities => _classAmbiguities; 

        /// <summary>
        /// Gets all the interfaces ambiguities: the first type is an interface that is implemented by more than one following concrete classes.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Type>> InterfaceAmbiguities => _interfaceAmbiguities; 

        /// <summary>
        /// Gets the list of tails that are abstract types.
        /// </summary>
        public IReadOnlyList<Type> AbstractTails => _abstractTails;

        /// <summary>
        /// Gets whether an error exists that prevents the process to continue.
        /// </summary>
        /// <returns>
        /// False to continue the process (only warnings - or error considered as 
        /// warning - occured), true to stop remaining processes.
        /// </returns>
        public bool HasFatalError => _classAmbiguities.Count != 0 || _interfaceAmbiguities.Count != 0;

        /// <summary>
        /// Logs detailed information about discovered ambient contracts.
        /// </summary>
        /// <param name="monitor">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            using( monitor.OpenTrace( $"Ambient Contracts: {EngineMap.MappedTypeCount} mappings for {_concreteClassesPath.Count} concrete paths." ) )
            {
                foreach( var a in _classAmbiguities )
                {
                    monitor.Error( $"Base class '{a[0].FullName}' has more than one concrete specialization: '{a.Skip( 1 ).Select( t => t.FullName ).Concatenate( "', '" )}'." );
                }
                foreach( var a in _interfaceAmbiguities )
                {
                    monitor.Error( $"Interface '{a[0].FullName}' is implemented by more than one concrete classes: {a.Skip( 1 ).Select( t => t.FullName ).Concatenate( "', '" )}." );
                }
                if( _abstractTails.Count > 0 )
                {
                    monitor.Warn( $"Abstract classes without specialization are ignored: {_abstractTails.Select( t => t.FullName ).Concatenate()}." );
                }

            }
        }

    }

}
