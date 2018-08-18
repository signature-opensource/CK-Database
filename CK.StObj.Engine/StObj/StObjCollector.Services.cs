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

            InterfaceFamily _family;
            List<CtorParameter> _params;
            bool _isHeadCandidate;
            bool _isTailCandidate;

            public class CtorParameter
            {
                public readonly AmbientServiceClassInfo.CtorParameter Parameter;
                public readonly HashSet<SCRClass> StatisficationSet;
                public bool HasDefault => Parameter.ParameterInfo.HasDefaultValue;

                public CtorParameter( AmbientServiceClassInfo.CtorParameter p, HashSet<SCRClass> sSet )
                {
                    Parameter = p;
                    StatisficationSet = sSet;
                }

                public override string ToString() => Parameter.ToString();
            }

            public SCRClass( AmbientServiceClassInfo c )
            {
                Class = c;
            }

            public IReadOnlyList<CtorParameter> Parameters => _params;

            internal void Initialize( InterfaceFamily f, ref List<SCRClass> candidateTail )
            {
                _family = f;
                bool tailCandidate = true;
                _params = new List<CtorParameter>();
                foreach( var p in Class.ConstructorParameters )
                {
                    var afterMe = f.Classes.Where( c => c != this && CanBeFollowedBy( c ) );
                    var typeCompatible = afterMe.Where( t => p.ParameterInfo.ParameterType.IsAssignableFrom( t.Class.Type ) );
                    var sSet = new HashSet<SCRClass>( typeCompatible );
                    if( sSet.Count > 0 )
                    {
                        var param = new CtorParameter( p, sSet );
                        _params.Add( param );
                        tailCandidate &= !param.HasDefault;
                    }
                }
                if( (_isTailCandidate = tailCandidate) )
                {
                    if( candidateTail == null ) candidateTail = new List<SCRClass>();
                    candidateTail.Add( this );
                }
                _isHeadCandidate = !f.Interfaces.Except( Class.Interfaces ).Any();
            }

            public bool CanBeFollowedBy( SCRClass other )
            {
                // Other may not have been initialized yet.
                // Debug.Assert( other._family == _family );
                int thisRank = Class.ContainerItem?.RankOrdered ?? Int32.MaxValue;
                int otherRank = other.Class.ContainerItem?.RankOrdered ?? Int32.MaxValue;
                return thisRank >= otherRank;
            }

            public InterfaceFamily Family => _family;

            public bool IsHeadCandidate => _isHeadCandidate;

            public bool IsTailCandidate => _isTailCandidate;

            public override int GetHashCode() => Class.GetHashCode();

            public override bool Equals( object obj ) => obj is SCRClass c && c.Class == Class;

            public override string ToString() => Class.ToString();
        }

        class InterfaceFamily
        {
            readonly HashSet<AmbientServiceInterfaceInfo> _interfaces;
            readonly HashSet<SCRClass> _classes;
            List<SCRClass> _necessaryTail;

            public IReadOnlyCollection<AmbientServiceInterfaceInfo> Interfaces => _interfaces;

            public IReadOnlyCollection<SCRClass> Classes => _classes;

            public IReadOnlyList<SCRClass> NecessaryTail => _necessaryTail;

            InterfaceFamily()
            {
                _interfaces = new HashSet<AmbientServiceInterfaceInfo>();
                _classes = new HashSet<SCRClass>();
            }

            public bool InitializeClasses( IActivityMonitor m )
            {
                Debug.Assert( Classes.Count > 0 );
                List<SCRClass> candidateTails = null;
                foreach( var c in Classes )
                {
                    c.Initialize( this, ref candidateTails );
                }
                if( candidateTails == null )
                {
                    m.Error( $"No valid tail found. At least one class must implement at least one of the root interface '{RootInterfacesToString()} and must not require any dependency." );
                    return false;
                }
                _necessaryTail = candidateTails.Where( tail => tail.Parameters.Count == 0 ).ToList();
                if( _necessaryTail.Count > 1 )
                {
                    m.Error( $"Duplicate tails found: '{_necessaryTail.Select( t => t.ToString() ).Concatenate("', '")}'." );
                    return false;
                }
                return true;
            }

            public void FinalRegister( StObjObjectEngineMap engineMap, SCRClass winner )
            {
                Debug.Assert( _classes.Contains( winner ) );
                foreach( var i in _interfaces )
                {
                    engineMap.ServiceSimpleMappings.Add( i.Type, winner.Class );
                }
            }

            public void FinalRegister( StObjObjectEngineMap engineMap, BuildClassInfo head )
            {
                Debug.Assert( _classes.Contains( head.Class ) );
                if( head.Assignments.Any( a => a.IsRequired ) )
                {
                    var factory = engineMap.CreateStObjServiceFinalManualMapping( head );
                    foreach( var i in _interfaces )
                    {
                        engineMap.ServiceManualMappings.Add( i.Type, factory );
                    }
                }
                else FinalRegister( engineMap, head.Class );
            }

            public static IReadOnlyCollection<InterfaceFamily> Build( IActivityMonitor m, IEnumerable<AmbientServiceClassInfo> classes )
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
                            currentF._classes.Add( new SCRClass( c ) );
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
                _classes.Union( f._classes );
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
            public BuildClassInfo Value { get; }

            public ParameterAssignment( SCRClass.CtorParameter p, BuildClassInfo v )
            {
                Parameter = p;
                Value = v;
                IsRequired = Value == BuildClassInfo.NullValue
                             || ParameterType.IsInterface
                             || Value.Assignments.Any( a => a.IsRequired );
            }

            public bool IsRequired { get; }

            public Type ParameterType => Parameter.Parameter.ParameterInfo.ParameterType;

            int IStObjServiceParameterInfo.Position => Parameter.Parameter.ParameterInfo.Position;

            string IStObjServiceParameterInfo.Name => Parameter.Parameter.ParameterInfo.Name;

            IStObjServiceClassFactoryInfo IStObjServiceParameterInfo.Value => Value != BuildClassInfo.NullValue ? Value : null;

        }

        class BuildClassInfo : IStObjServiceClassFactoryInfo
        {
            readonly IReadOnlyList<IStObjServiceParameterInfo> _finalAssignments;

            public SCRClass Class { get; }

            public IReadOnlyList<ParameterAssignment> Assignments { get; }

            static internal readonly BuildClassInfo NullValue = new BuildClassInfo();

            private BuildClassInfo()
            {
            }

            public BuildClassInfo( SCRClass c, IReadOnlyList<ParameterAssignment> a )
            {
                Class = c;
                Assignments = a;
                _finalAssignments = Assignments.Where( s => s.IsRequired ).ToArray();
            }

            Type IStObjServiceClassFactoryInfo.ClassType => Class.Class.Type;

            IReadOnlyList<IStObjServiceParameterInfo> IStObjServiceClassFactoryInfo.Assignments => _finalAssignments;

            public StringBuilder ToString( StringBuilder b )
            {
                if( Class == null ) b.Append( "null" );
                else
                {
                    b.Append( Class.Class.Type.Name ).Append( '(' );
                    bool atLeastOne = false;
                    foreach( var a in Assignments )
                    {
                        if( atLeastOne ) b.Append( ',' );
                        atLeastOne = true;
                        a.Value.ToString( b );
                    }
                    b.Append( ')' );
                }
                return b;
            }

            public override string ToString() => ToString( new StringBuilder() ).ToString();
        }

        /// <summary>
        /// Called once Mutable items have been created.
        /// </summary>
        /// <param name="typeResult">The Ambient types discovery result.</param>
        /// <returns>True on success, false on error.</returns>
        bool RegisterServices( AmbientTypeCollectorResult typeResult )
        {
            using( _monitor.OpenInfo( $"Service handling." ) )
            {
                // Registering Interface => Class or List<Class> from classes' supported interfaces.
                var allClasses = typeResult.AmbientServices.RootClasses
                                    .Concat( typeResult.AmbientServices.SubGraphRootClasses )
                                    .Select( c => c.MostSpecialized );
                IReadOnlyCollection<InterfaceFamily> families = InterfaceFamily.Build( _monitor, allClasses );
                if( families.Count == 0 )
                {
                    _monitor.Warn( "No Service interface found. Nothing can be mapped at the Service Interface level." );
                    return true;
                }
                _monitor.Trace( $"{families.Count} Service families found." );
                var engineMap = typeResult.AmbientContracts.EngineMap;
                bool success = true;
                foreach( var f in families )
                {
                    Debug.Assert( f.Classes.Count > 0 && f.Interfaces.Count > 0 );
                    if( f.Classes.Count == 1 )
                    {
                        f.FinalRegister( engineMap, f.Classes.First() );
                    }
                    else
                    {
                        using( _monitor.OpenInfo( $"Service Chaining resolution required for {f}." ) )
                        {
                            if( !f.InitializeClasses( _monitor )
                                || !ResolveChain( engineMap, f ) )
                            {
                                _monitor.CloseGroup( "Failed." );
                                success = false;
                            }
                        }
                    }
                }
                return success;
            }
        }

        bool ResolveChain( StObjObjectEngineMap engineMap, InterfaceFamily f )
        {
            bool success = true;
            var heads = f.Classes.Where( c => c.IsHeadCandidate ).ToList();
            if( heads.Count == 0 )
            {
                _monitor.Error( $"No valid head found. A class that implements '{f.BaseInterfacesToString()}' interfaces is required." );
                success = false;
            }
            else
            {
                var solved = new List<BuildClassInfo>();
                for( int i = 0; i < heads.Count; ++i )
                {
                    using( _monitor.OpenTrace( $"Trying to resolve from '{heads[i].Class.Type.Name}' head." ) )
                    {
                        var cache = new Dictionary<SCRClass, BuildClassInfo>();
                        var chain = BuildChain( heads[i], f.Classes, cache );
                        if( chain != null )
                        {
                            Debug.Assert( cache.Count <= f.Classes.Count );
                            if( cache.Count < f.Classes.Count )
                            {
                                _monitor.CloseGroup( $"Chain {chain} is too short." );
                            }
                            else
                            {
                                solved.Add( chain );
                                _monitor.CloseGroup( $"-> {chain}." );
                            }
                        }
                        else _monitor.CloseGroup( $"Failed." );
                    }
                }
                if( solved.Count == 1 )
                {
                    f.FinalRegister( engineMap, solved[0] );
                }
                else
                {
                    success = false;
                    if( solved.Count > 1 )
                    {
                        using( _monitor.OpenError( $"Multiple possible chains found:" ) )
                        {
                            foreach( var c in solved ) _monitor.Trace( c.ToString() );
                        }
                    }
                }
            }
            return success;
        }

        BuildClassInfo BuildChain(
            SCRClass head,
            IEnumerable<SCRClass> remainders,
            Dictionary<SCRClass, BuildClassInfo> cache )
        {
            remainders = remainders.Where( r => r != head );
            if( cache.TryGetValue( head, out var result ))
            {
                if( !remainders.Contains( result.Class ) ) return null;
                return result;
            }
            var bindings = new ParameterAssignment[head.Parameters.Count];
            for( int i = 0; i < bindings.Length; ++i )
            {
                var p = head.Parameters[i];
                var cInfo = BindParameter( head.Parameters[i], remainders, cache );
                if( cInfo == null )
                {
                    _monitor.Warn( $"Failed to resolve '{head.Class.Type.Name}' considering classes: '{remainders.Select( r => r.Class.Type.Name ).Concatenate("', '")}'." );
                    return null;
                }
                bindings[i] = new ParameterAssignment( p, cInfo );
            }
            result = new BuildClassInfo( head, bindings );
            cache.Add( head, result );
            return result;
        }

        BuildClassInfo BindParameter( SCRClass.CtorParameter p, IEnumerable<SCRClass> remainders, Dictionary<SCRClass, BuildClassInfo> cache )
        {
            var possible = p.StatisficationSet
                                .Intersect( remainders )
                                .ToList();
            SCRClass unambiguousPossible = null;
            if( possible.Count == 0 )
            {
                if( p.HasDefault )
                {
                    _monitor.Trace( $"Using null default for parameter {p}." );
                    return BuildClassInfo.NullValue;
                }
                _monitor.Warn( $"Unable to bind parameter {p}." );
                return null;
            }
            if( possible.Count == 1 ) unambiguousPossible = possible[0];
            else
            {
                // Heuristic: consider that the class that more parameters are more general.
                possible.Sort( ( c1, c2 ) => c2.Parameters.Count - c1.Parameters.Count );
                // Do not rely on RankOrder, only on the IsNecessarilyBefore predicate: this way, the "ordering"
                // can rely on any other rules and has not to be a projection on scalar.
                // This is awfully n² but we don't care for the moment.
                for( int i = 0; i < possible.Count; ++i )
                {
                    bool found = true;
                    for( int j = 0; j < possible.Count; j++ )
                    {
                        if( i == j ) continue;
                        if( !possible[i].CanBeFollowedBy( possible[j] ) )
                        {
                            found = false;
                            break;
                        }
                    }
                    if( found )
                    {
                        unambiguousPossible = possible[i];
                        break;
                    }
                }
                if( unambiguousPossible == null )
                {
                    _monitor.Warn( $"Ambiguous binding for parameter {p}: {possible.Select( t => t.Class.Type.Name ).Concatenate()}." );
                    return null;
                }
            }
            var result = BuildChain( unambiguousPossible, remainders, cache );
            return result;
        }


    }
}
