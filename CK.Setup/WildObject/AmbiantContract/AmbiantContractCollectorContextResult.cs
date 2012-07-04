using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public class AmbiantContractCollectorContextResult
    {
        AmbiantTypeMapper _mappings;
        IReadOnlyList<Type> _concreteClasses;
        IReadOnlyList<Tuple<Type,Type>> _classAmbiguities;
        IReadOnlyList<Tuple<Type, Type>> _interfaceAmbiguities;
        IReadOnlyList<IReadOnlyList<Type>> _abstractClasses;
        IReadOnlyList<IReadOnlyList<Type>> _abstractTails;
        IReadOnlyList<Type> _pureDefiners;

        internal AmbiantContractCollectorContextResult( Type context,
                                Dictionary<Type, Type> mappings,
                                IReadOnlyList<Type> concreteClasses,
                                IReadOnlyList<Tuple<Type, Type>> classAmbiguities,
                                List<Tuple<Type, Type>> interfaceAmbiguities,
                                List<IReadOnlyList<Type>> abstractClasses,
                                List<IReadOnlyList<Type>> abstractTails,
                                List<Type> pureDefiners )
        {
            _mappings = new AmbiantTypeMapper( context, mappings );
            _concreteClasses = concreteClasses;
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

        public Type Context
        {
            get { return _mappings.Context; }
        }

        public IAmbiantTypeMapper Mappings
        {
            get { return _mappings; }
        }

        public IReadOnlyList<Type> ConcreteClasses
        {
            get { return _concreteClasses; }
        }

        public IReadOnlyList<Tuple<Type, Type>> ClassAmbiguities
        {
            get { return _classAmbiguities; }
        }

        public IReadOnlyList<Tuple<Type, Type>> InterfaceAmbiguities
        {
            get { return _interfaceAmbiguities; }
        }

        public IReadOnlyList<IReadOnlyList<Type>> AbstractClasses
        {
            get { return _abstractClasses; }
        }

        public IReadOnlyList<IReadOnlyList<Type>> AbstractTails
        {
            get { return _abstractTails; }
        }

        public IReadOnlyList<Type> PureDefiners
        {
            get { return _pureDefiners; }
        }

        internal bool CheckErrorAndWarnings( IActivityLogger logger )
        {
            using( logger.OpenGroup( LogLevel.Trace, "Ambiant Contract for '{1}' context: {0} types.", _mappings.Count, Context != null ? Context.Name : "(Default)" ) )
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
            return _classAmbiguities.Count == 0 && _interfaceAmbiguities.Count == 0;
        }

    }

}
