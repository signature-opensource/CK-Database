using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Core
{
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
        /// Gets the context name. <see cref="String.Empty"/> for default context.
        /// </summary>
        public string Context
        {
            get { return _mappings.Context; }
        }

        /// <summary>
        /// Gets the type mapper for this context.
        /// </summary>
        public CT Mappings
        {
            get { return _mappings; }
        }

        /// <summary>
        /// Gets all the paths from <see cref="IAmbientContract"/> base classes to their most specialized concrete classes 
        /// that this context contains.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<TC>> ConcreteClasses
        {
            get { return _concreteClassesPath; }
        }

        /// <summary>
        /// Gets all the class ambiguities: the first type corresponds to more than one following concrete specializations.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Type>> ClassAmbiguities
        {
            get { return _classAmbiguities; }
        }

        /// <summary>
        /// Gets all the interfaces ambiguities: the first type is an interface that is implemented by more than one following concrete classes.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Type>> InterfaceAmbiguities
        {
            get { return _interfaceAmbiguities; }
        }

        public IReadOnlyList<Type> AbstractTails
        {
            get { return _abstractTails; }
        }

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
            using( monitor.OpenTrace().Send( "Ambient Contract for context '{1}': {0} mappings for {2} concrete paths.", _mappings.MappedTypeCount, Context, _concreteClassesPath.Count ) )
            {
                foreach( var a in _classAmbiguities )
                {
                    monitor.Error().Send( "Base class '{0}' has more than one concrete specialization: '{1}'.", a[0].FullName, String.Join( "', '", a.Skip(1).Select( t => t.FullName ) ) );
                }
                foreach( var a in _interfaceAmbiguities )
                {
                    monitor.Error().Send( "Interface '{0}' is implemented by more than one concrete classes: {1}.", a[0].FullName, String.Join( "', '", a.Skip( 1 ).Select( t => t.FullName ) ) );
                }
                if( _abstractTails.Count > 0 )
                {
                    monitor.Warn().Send( "Abstract classes without specialization are ignored: {0}.", String.Join( ", ", _abstractTails.Select( t => t.FullName ) ) );
                }

            }
        }

    }

}
