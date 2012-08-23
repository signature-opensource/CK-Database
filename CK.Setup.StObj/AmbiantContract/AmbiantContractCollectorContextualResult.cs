using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Core
{
    public class AmbiantContractCollectorContextualResult : IContextualResult
    {
        AmbiantTypeContextualMapper _mappings;
        IReadOnlyList<IReadOnlyList<Type>> _concreteClassesPath;
        IReadOnlyList<IReadOnlyList<Type>> _classAmbiguities;
        IReadOnlyList<IReadOnlyList<Type>> _interfaceAmbiguities;
        IReadOnlyList<Type> _abstractTails;

        internal AmbiantContractCollectorContextualResult( AmbiantTypeContextualMapper mappings,
                                IReadOnlyList<IReadOnlyList<Type>> concreteClasses,
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
        /// Gets the <see cref="Type"/> that identifies this context. Null for default context.
        /// </summary>
        public Type Context
        {
            get { return _mappings.Context; }
        }

        /// <summary>
        /// Gets the type mapper for this context.
        /// </summary>
        public IAmbiantTypeContextualMapper Mappings
        {
            get { return _mappings; }
        }

        /// <summary>
        /// Gets all the paths from <see cref="IAmbiantContract"/> base classes to their most specialized concrete classes 
        /// that this context contains.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Type>> ConcreteClasses
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
        /// Logs detailed information about discovered ambiant contracts.
        /// </summary>
        /// <param name="logger">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            using( logger.OpenGroup( LogLevel.Trace, "Ambiant Contract for '{1}' context: {0} mappings for {2} concrete paths.", _mappings.Count, Context != AmbiantContractCollector.DefaultContext ? Context.Name : "(Default)", _concreteClassesPath.Count ) )
            {
                foreach( var a in _classAmbiguities )
                {
                    logger.Error( "Base class '{0}' has more than one concrete specialization: '{1}'.", a[0].FullName, String.Join( "', '", a.Skip(1).Select( t => t.FullName ) ) );
                }
                foreach( var a in _interfaceAmbiguities )
                {
                    logger.Error( "Interface '{0}' is implemented by more than one concrete classes: {1}.", a[0].FullName, String.Join( "', '", a.Skip( 1 ).Select( t => t.FullName ) ) );
                }
                if( _abstractTails.Count > 0 )
                {
                    logger.Warn( "Abstract classes without specialization are ignored: {0}.", String.Join( ", ", _abstractTails.Select( t => t.FullName ) ) );
                }

            }
        }

    }

}
