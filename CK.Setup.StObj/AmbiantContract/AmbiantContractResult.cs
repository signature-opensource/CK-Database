using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public class AmbiantContractResult : IContextResult
    {
        AmbiantTypeMapper _mappings;
        IReadOnlyList<IReadOnlyList<Type>> _concreteClassesPath;
        IReadOnlyList<Tuple<Type,Type>> _classAmbiguities;
        IReadOnlyList<Tuple<Type, Type>> _interfaceAmbiguities;
        IReadOnlyList<IReadOnlyList<Type>> _abstractClasses;
        IReadOnlyList<IReadOnlyList<Type>> _abstractTails;
        IReadOnlyList<Type> _pureDefiners;

        internal AmbiantContractResult( Type context,
                                Dictionary<object, Type> mappings,
                                IReadOnlyList<IReadOnlyList<Type>> concreteClasses,
                                IReadOnlyList<Tuple<Type, Type>> classAmbiguities,
                                List<Tuple<Type, Type>> interfaceAmbiguities,
                                List<IReadOnlyList<Type>> abstractClasses,
                                List<IReadOnlyList<Type>> abstractTails,
                                List<Type> pureDefiners )
        {
            _mappings = new AmbiantTypeMapper( context, mappings );
            _concreteClassesPath = concreteClasses;
            _classAmbiguities = classAmbiguities;
            _interfaceAmbiguities = interfaceAmbiguities != null
                                        ? new ReadOnlyListOnIList<Tuple<Type, Type>>( interfaceAmbiguities )
                                        : ReadOnlyListEmpty<Tuple<Type, Type>>.Empty;
            _abstractClasses = abstractClasses != null
                                        ? new ReadOnlyListOnIList<IReadOnlyList<Type>>( abstractClasses )
                                        : ReadOnlyListEmpty<IReadOnlyList<Type>>.Empty;
            _abstractTails = abstractTails != null
                                        ? new ReadOnlyListOnIList<IReadOnlyList<Type>>( abstractTails )
                                        : ReadOnlyListEmpty<IReadOnlyList<Type>>.Empty;
            _pureDefiners = pureDefiners != null 
                                        ? new ReadOnlyListOnIList<Type>( pureDefiners )
                                        : ReadOnlyListEmpty<Type>.Empty;
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
        public IAmbiantTypeMapper Mappings
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
        /// Gets all the class ambiguities: the pair of non-abstract classes listed here 
        /// share the same ancestor.
        /// </summary>
        public IReadOnlyList<Tuple<Type, Type>> ClassAmbiguities
        {
            get { return _classAmbiguities; }
        }

        /// <summary>
        /// Gets all the interfaces ambiguities: the pair of interfaces listed here share the same ancestor.
        /// </summary>
        public IReadOnlyList<Tuple<Type, Type>> InterfaceAmbiguities
        {
            get { return _interfaceAmbiguities; }
        }

        /// <summary>
        /// Gets the list of purely abstract chains: first type (main abstraction) is ignored.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Type>> AbstractClasses
        {
            get { return _abstractClasses; }
        }

        public IReadOnlyList<IReadOnlyList<Type>> AbstractTails
        {
            get { return _abstractTails; }
        }

        /// <summary>
        /// Gets the <see cref="Type"/> that implement <see cref="IAmbiantContractDefiner"/> and have no specialization: these 
        /// abstractions have no available concretization.
        /// </summary>
        public IReadOnlyList<Type> PureDefiners
        {
            get { return _pureDefiners; }
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
        /// Logs detailed information about discovered ambiant contracts. Returns false if an error
        /// should prevent the process to continue (currently if a class or an interface ambiguity is found, false is returned).
        /// </summary>
        /// <param name="logger">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityLogger logger )
        {
            using( logger.OpenGroup( LogLevel.Trace, "Ambiant Contract for '{1}' context: {0} mappings for {2} concrete paths.", _mappings.Count, Context != null ? Context.Name : "(Default)", _concreteClassesPath.Count ) )
            {
                foreach( Tuple<Type,Type> a in _classAmbiguities )
                {
                    Type winner = _mappings[a.Item1];
                    logger.Error( "Multiple inheritance chain for '{0}': '{1}' and '{2}' both specialize it.", a.Item1.FullName, winner.AssemblyQualifiedName, a.Item2.AssemblyQualifiedName );
                }
                foreach( Tuple<Type,Type> a in _interfaceAmbiguities )
                {
                    Type winner = _mappings[a.Item1];
                    logger.Error( "Interface '{0}' mapping is ambiguous between '{1}' and '{2}'.", a.Item1.FullName, winner.AssemblyQualifiedName, a.Item2.AssemblyQualifiedName );
                }
                foreach( var a in _abstractTails )
                {
                    logger.Warn( "Inheritance chain ignored for '{0}'. All classes are abstract: {1}.", a[0].FullName, String.Join( " => ", a.Skip( 1 ).Select( t => t.FullName ) ) );
                }
                foreach( var a in _abstractClasses )
                {
                    logger.Warn( "Ignored mapping for '{0}'. All classes are abstract: {1}.", a[0].FullName, String.Join( " => ", a.Skip( 1 ).Select( t => t.FullName ) ) );
                }
                foreach( var p in _pureDefiners )
                {
                    logger.Warn( "Ignored Definer Type '{0}'. No specialization found.", p );
                }
            }
        }

    }

}
