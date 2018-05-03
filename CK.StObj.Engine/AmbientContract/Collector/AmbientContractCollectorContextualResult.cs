using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Text;

namespace CK.Core
{
    /// <summary>
    /// Result of the <see cref="AmbientContractCollector{CT,T,TC}"/> work.
    /// </summary>
    /// <typeparam name="CT">A <see cref="AmbientContextualTypeMap{T,TC}"/> type.</typeparam>
    /// <typeparam name="T">A <see cref="AmbientTypeInfo"/> type.</typeparam>
    /// <typeparam name="TC">A <see cref="AmbientContextualTypeInfo{T,TC}"/> type.</typeparam>
    public class AmbientContractCollectorContextualResult<CT,T,TC> : IContextualResult
        where CT : AmbientContextualTypeMap<T,TC>
        where T : AmbientTypeInfo
        where TC : AmbientContextualTypeInfo<T,TC>
    {
        CT _mappings;
        IReadOnlyList<IReadOnlyList<TC>> _concreteClassesPath;
        IReadOnlyList<IReadOnlyList<Type>> _classAmbiguities;
        IReadOnlyList<IReadOnlyList<Type>> _interfaceAmbiguities;
        IReadOnlyList<Type> _abstractTails;

        internal AmbientContractCollectorContextualResult( CT mappings,
                                IReadOnlyList<IReadOnlyList<TC>> concreteClasses,
                                IReadOnlyList<IReadOnlyList<Type>> classAmbiguities,
                                IReadOnlyList<IReadOnlyList<Type>> interfaceAmbiguities,
                                IReadOnlyList<Type> abstractTails )
        {
            _mappings = mappings;
            _concreteClassesPath = concreteClasses;
            _classAmbiguities = classAmbiguities;
            _interfaceAmbiguities = interfaceAmbiguities;
            _abstractTails = abstractTails;
        }


        /// <summary>
        /// Gets the context name. <see cref="string.Empty"/> for default context.
        /// </summary>
        public string Context => _mappings.Context; 

        /// <summary>
        /// Gets the type mapper for this context.
        /// </summary>
        public CT Mappings => _mappings; 

        /// <summary>
        /// Gets all the paths from <see cref="IAmbientContract"/> base classes to their most specialized concrete classes 
        /// that this context contains.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<TC>> ConcreteClasses => _concreteClassesPath; 

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
        /// Gets whether an error exists that prevents the process to continue: currently if a class or an interface 
        /// ambiguity is found, it is true.
        /// </summary>
        /// <returns>True to stop remaining processes.</returns>
        public bool HasFatalError 
        {
            get { return _classAmbiguities.Count != 0 || _interfaceAmbiguities.Count != 0; } 
        }

        /// <summary>
        /// Logs detailed information about discovered ambient contracts.
        /// </summary>
        /// <param name="monitor">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            using( monitor.OpenTrace( $"Ambient Contract for context '{Context}': {_mappings.MappedTypeCount} mappings for {_concreteClassesPath.Count} concrete paths." ) )
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
