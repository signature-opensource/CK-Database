using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CK.Setup
{
    public partial class StObjCollector
    {
        class SCRClass
        {
            public readonly AmbientServiceClassInfo Class;
            public readonly InterfaceFamily Family;

            List<CtorParameter> _params;
            bool _isHeadCandidate;
            bool _isHead;

            public class CtorParameter
            {
                public readonly AmbientServiceClassInfo.CtorParameter Parameter;
                bool _canUseDefault;

                public bool HasDefault => Parameter.ParameterInfo.HasDefaultValue;

                public bool CanUseDefault
                {
                    get => _canUseDefault;
                    set
                    {
                        Debug.Assert( value == false || HasDefault );
                        _canUseDefault = value;
                    }
                }

                public CtorParameter( AmbientServiceClassInfo.CtorParameter p )
                {
                    Parameter = p;
                }

                public override string ToString() => Parameter.ToString();
            }

            public SCRClass( InterfaceFamily f, AmbientServiceClassInfo c )
            {
                Family = f;
                Class = c;
            }

            public IReadOnlyList<CtorParameter> Parameters => _params;

            internal bool Initialize( IActivityMonitor m )
            {
                bool success = true;
                _params = new List<CtorParameter>();
                foreach( var p in Class.ConstructorParameters )
                {
                    if( p.ServiceInterface != null && Family.Interfaces.Contains( p.ServiceInterface ))
                    {
                        if( !p.IsEnumerated )
                        {
                            m.Error( $"Invalid parameter {p}: it can not be an Ambient Service of its own family." );
                            success = false;
                        }
                    }
                }
                if( success )
                {
                    _isHeadCandidate = !Family.Interfaces.Except( Class.Interfaces ).Any();
                    _isHead = _isHeadCandidate
                              && Family.Classes.Where( c => c != this )
                                               .All( c => Class.ComputedCtorParametersClassClosure.Contains( c.Class ) );
                }
                return success;
            }

            /// <summary>
            /// A head candidate is a class that implements all its <see cref="Family"/>'s
            /// <see cref="InterfaceFamily.Interfaces"/>.
            /// </summary>
            public bool IsHeadCandidate => _isHeadCandidate;

            /// <summary>
            /// To be a head, this class must be a head candidate and its constructor parameter closure must
            /// cover all other <see cref="Family"/>'s <see cref="InterfaceFamily.Classes"/>.
            /// </summary>
            public bool IsHead => _isHead;

            public override string ToString() => Class.ToString();
        }

        class InterfaceFamily
        {
            readonly HashSet<AmbientServiceInterfaceInfo> _interfaces;
            readonly Dictionary<AmbientServiceClassInfo,SCRClass> _classes;

            public IReadOnlyCollection<AmbientServiceInterfaceInfo> Interfaces => _interfaces;

            public IReadOnlyCollection<SCRClass> Classes => _classes.Values;

            public AmbientServiceClassInfo Resolved { get; private set; }

            InterfaceFamily()
            {
                _interfaces = new HashSet<AmbientServiceInterfaceInfo>();
                _classes = new Dictionary<AmbientServiceClassInfo, SCRClass>();
            }

            bool InitializeClasses( IActivityMonitor m )
            {
                Debug.Assert( Classes.Count > 0 );
                bool success = true;
                foreach( var c in Classes )
                {
                    success &= c.Initialize( m );
                }
                return success;
            }

            public bool Resolve( IActivityMonitor m, FinalRegisterer finalRegisterer )
            {
                bool success = true;
                Debug.Assert( Classes.Count > 0 && Interfaces.Count > 0 );
                if( Classes.Count == 1 )
                {
                    Resolved = Classes.Single().Class;
                }
                else
                {
                    using( m.OpenInfo( $"Service resolution required for {ToString()}." ) )
                    {
                        if( success = InitializeClasses( m ) )
                        {
                            var headCandidates = Classes.Where( c => c.IsHeadCandidate ).ToList();
                            var heads = headCandidates.Where( c => c.IsHead ).ToList();
                            if( headCandidates.Count == 0 )
                            {
                                m.Error( $"No possible implementation found. A class that implements '{BaseInterfacesToString()}' interfaces is required." );
                                success = false;
                            }
                            else if( heads.Count == 0 )
                            {
                                m.Error( $"Among '{headCandidates.Select( c => c.ToString() ).Concatenate( "', '" )}' possible implementations, none covers the whole set of other implementations." );
                                success = false;
                            }
                            else if( heads.Count > 1 )
                            {
                                m.Error( $"Multiple possible implementations found: '{heads.Select( c => c.ToString() ).Concatenate( "', '" )}'. They must be unified." );
                                success = false;
                            }
                            else
                            {
                                // Here comes the "dispatcher" handling and finalRegisterer must
                                // register all BuildClassInfo required by special handling of
                                // handled parameters (IReadOnlyCollection<IService>...).
                                var r = heads[0].Class;
                                Resolved = r;
                                foreach( var i in _interfaces )
                                {
                                    i.FinalResolved = r;
                                }
                                m.CloseGroup( $"Resolved to '{r}'." );
                            }
                        }
                    }
                }
                return success;
            }

            public static IReadOnlyCollection<InterfaceFamily> Build(
                IActivityMonitor m,
                StObjObjectEngineMap engineMap,
                IEnumerable<AmbientServiceClassInfo> classes )
            {
                var families = new Dictionary<AmbientServiceInterfaceInfo, InterfaceFamily>();
                bool familiesHasBeenMerged = false;
                foreach( var c in classes )
                {
                    Debug.Assert( c.IsIncluded
                                  && (c.Interfaces.Count == 0 || c.Interfaces.Any( i => i.SpecializationDepth == 0 )) );
                    foreach( var baseInterface in c.Interfaces.Where( i => !i.IsSpecialized ) )
                    {
                        InterfaceFamily currentF = null;
                        var rootInterfaces = baseInterface.SpecializationDepth == 0
                                                ? new[] { baseInterface }
                                                : baseInterface.Interfaces.Where( i => i.SpecializationDepth == 0 );
                        foreach( var root in rootInterfaces )
                        {
                            if( families.TryGetValue( root, out var f ) )
                            {
                                if( currentF == null ) currentF = f;
                                else if( currentF != f )
                                {
                                    currentF.MergeWith( f );
                                    families[root] = currentF;
                                    m.Info( $"Family interfaces merged because of '{baseInterface.Type.Name}'." );
                                    familiesHasBeenMerged = true;
                                }
                            }
                            else
                            {
                                if( currentF == null ) currentF = new InterfaceFamily();
                                families.Add( root, currentF );
                            }
                            currentF._interfaces.AddRange( baseInterface.Interfaces );
                            currentF._interfaces.Add( baseInterface );
                        }
                        if( currentF != null )
                        {
                            if( !currentF._classes.ContainsKey( c ) )
                            {
                                currentF._classes.Add( c, new SCRClass( currentF, c ) );
                            }
                        }
                    }
                }
                IReadOnlyCollection<InterfaceFamily> result = families.Values;
                if( familiesHasBeenMerged ) result = result.Distinct().ToList();
                return result;
            }

            void MergeWith( InterfaceFamily f )
            {
                Debug.Assert( _interfaces.Intersect( f._interfaces ).Any() == false );
                _interfaces.UnionWith( f._interfaces );
                _classes.AddRange( f._classes );
            }

            public string BaseInterfacesToString()
            {
                return Interfaces.Where( i => !i.IsSpecialized ).Select( i => i.Type.Name ).Concatenate( "', '" );
            }

            public string RootInterfacesToString()
            {
                return Interfaces.Where( i => i.SpecializationDepth == 0 ).Select( i => i.Type.Name ).Concatenate( "', '" );
            }

            public override string ToString()
            {
                return $"'{RootInterfacesToString()}' family with {Interfaces.Count} interfaces on {Classes.Count} classes.";
            }
        }

        class ParameterAssignment : IStObjServiceParameterInfo
        {
            public SCRClass.CtorParameter Parameter { get; }

            public ParameterAssignment( SCRClass.CtorParameter p, IReadOnlyList<Type> v )
            {
                Parameter = p;
                Value = v;
            }

            public Type ParameterType => Parameter.Parameter.ParameterInfo.ParameterType;

            int IStObjServiceParameterInfo.Position => Parameter.Parameter.ParameterInfo.Position;

            string IStObjServiceParameterInfo.Name => Parameter.Parameter.ParameterInfo.Name;

            public bool IsEnumerated => Parameter.Parameter.IsEnumerated;

            public IReadOnlyList<Type> Value { get; }
        }

        class BuildClassInfo : IStObjServiceClassFactoryInfo
        {
            IStObjServiceFinalManualMapping _finalMapping;
            bool _finalMappingDone;

            public AmbientServiceClassInfo Class { get; }

            public IReadOnlyList<ParameterAssignment> Assignments { get; }

            public BuildClassInfo( AmbientServiceClassInfo c, IReadOnlyList<ParameterAssignment> a )
            {
                Class = c;
                Assignments = a;
            }

            bool IStObjServiceClassDescriptor.IsScoped
            {
                get
                {
                    Debug.Assert( _finalMappingDone, "Must be called only once GetFinalMapping has been called at least once." );
                    return Class.MustBeScopedLifetime.Value;
                }
            }


            Type IStObjServiceClassDescriptor.ClassType => Class.Type;

            IReadOnlyList<IStObjServiceParameterInfo> IStObjServiceClassFactoryInfo.Assignments => Assignments;

            public IStObjServiceFinalManualMapping GetFinalMapping(
                IActivityMonitor m,
                StObjObjectEngineMap engineMap,
                AmbientTypeKindDetector typeKindDetector,
                ref bool success )
            {
                if( !_finalMappingDone )
                {
                    _finalMappingDone = true;
                    Class.GetFinalMustBeScopedLifetime( m, typeKindDetector, ref success );
                    if( Assignments.Any() )
                    {
                        _finalMapping = engineMap.CreateStObjServiceFinalManualMapping( this );
                    }
                }
                return _finalMapping;
            }

            public BuildClassInfo Merge( BuildClassInfo c )
            {
                Debug.Assert( Class == c.Class );
                Debug.Assert( Assignments.Select( a => a.Parameter ).Intersect( c.Assignments.Select( a => a.Parameter ) ).Any() == false );
                var assignments = Assignments.Concat( c.Assignments ).ToList();
                return new BuildClassInfo( Class, assignments );
            }

            public StringBuilder ToString( StringBuilder b )
            {
                if( Class == null ) b.Append( "null" );
                else
                {
                    b.Append( Class.Type.Name ).Append( '(' );
                    bool atLeastOne = false;
                    foreach( var a in Assignments )
                    {
                        if( atLeastOne ) b.Append( ',' );
                        atLeastOne = true;
                        if( a.IsEnumerated )
                        {
                            b.Append( '[' );
                            b.AppendStrings( a.Value.Select( t => t.Name ) );
                            b.Append( ']' );
                        }
                        else b.Append( a.Value[0].Name );
                    }
                    b.Append( ')' );
                }
                return b;
            }

            public override string ToString() => ToString( new StringBuilder() ).ToString();
        }

        class FinalRegisterer
        {
            readonly IActivityMonitor _monitor;
            readonly StObjObjectEngineMap _engineMap;
            readonly AmbientTypeKindDetector _ambientTypeKindDetector;
            readonly Dictionary<AmbientServiceClassInfo, BuildClassInfo> _infos;

            public FinalRegisterer(
                IActivityMonitor monitor,
                StObjObjectEngineMap engineMap,
                AmbientTypeKindDetector typeKindDetector )
            {
                _monitor = monitor;
                _engineMap = engineMap;
                _infos = new Dictionary<AmbientServiceClassInfo, BuildClassInfo>();
                _ambientTypeKindDetector = typeKindDetector;
            }

            /// <summary>
            /// Not used yet. Planned to support services enumerable and required manual mapping.
            /// </summary>
            /// <param name="c"></param>
            public void Register( BuildClassInfo c )
            {
                if( _infos.TryGetValue( c.Class, out var exists ) )
                {
                    _infos[c.Class] = exists.Merge( c );
                }
                else _infos.Add( c.Class, c );
            }

            public bool FinalRegistration( AmbientServiceCollectorResult typeResult, IEnumerable<InterfaceFamily> families )
            {
                bool success = true;
                foreach( var c in typeResult.RootClasses )
                {
                    RegisterClassMapping( c, ref success );
                }
                foreach( var f in families )
                {
                    foreach( var i in f.Interfaces )
                    {
                        RegisterMapping( i.Type, f.Resolved, ref success );
                    }
                }
                return success;
            }

            void RegisterClassMapping( AmbientServiceClassInfo c, ref bool success )
            {
                RegisterMapping( c.Type, c.MostSpecialized, ref success );
                foreach( var s in c.Specializations )
                {
                    RegisterClassMapping( s, ref success );
                }
            }

            void RegisterMapping(
                Type t,
                AmbientServiceClassInfo final,
                ref bool success )
            {
                Debug.Assert( _infos.Count == 0, "Currently, no manual instanciation is available since IEnumerable is not yet handled." );
                IStObjServiceFinalManualMapping manual = null;
                if( _infos.TryGetValue( final, out var build )
                    && (manual = build.GetFinalMapping( _monitor, _engineMap, _ambientTypeKindDetector, ref success )) != null )
                {
                    _monitor.Debug( $"Map '{t.Name}' -> manual '{final}': '{manual}'." );
                    _engineMap.ServiceManualMappings.Add( t, manual );
                }
                else
                {
                    _monitor.Debug( $"Map '{t.Name}' -> '{final}'." );
                    final.GetFinalMustBeScopedLifetime( _monitor, _ambientTypeKindDetector, ref success );
                    _engineMap.ServiceSimpleMappings.Add( t, final );
                }
            }
        }

        /// <summary>
        /// Called once Mutable items have been created.
        /// </summary>
        /// <param name="typeResult">The Ambient types discovery result.</param>
        /// <returns>True on success, false on error.</returns>
        bool RegisterServices( AmbientTypeCollectorResult typeResult )
        {
            var engineMap = typeResult.AmbientObjects.EngineMap;
            using( _monitor.OpenInfo( $"Service handling." ) )
            {
                try
                {
                    // Registering Interfaces: Families creation from all most specialized classes' supported interfaces.
                    var allClasses = typeResult.AmbientServices.RootClasses
                                        .Concat( typeResult.AmbientServices.SubGraphRootClasses )
                                        .Select( c => c.MostSpecialized );
                    Debug.Assert( allClasses.GroupBy( c => c ).All( g => g.Count() == 1 ) );
                    IReadOnlyCollection<InterfaceFamily> families = InterfaceFamily.Build( _monitor, engineMap, allClasses );
                    if( families.Count == 0 )
                    {
                        _monitor.Warn( "No IAmbient Service interface found. Nothing can be mapped at the Service Interface level." );
                    }
                    else _monitor.Trace( $"{families.Count} Service families found." );
                    bool success = true;
                    var manuals = new FinalRegisterer( _monitor, engineMap, typeResult.TypeKindDetector );
                    foreach( var f in families )
                    {
                        success &= f.Resolve( _monitor, manuals );
                    }
                    if( success )
                    {
                        success &= manuals.FinalRegistration( typeResult.AmbientServices, families );
                    }
                    return success;
                }
                catch( Exception ex )
                {
                    _monitor.Fatal( ex );
                    return false;
                }
            }
        }

    }
}
