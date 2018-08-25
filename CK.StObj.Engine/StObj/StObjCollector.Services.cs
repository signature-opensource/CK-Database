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
                    var afterMe = f.Classes.Where( c => CanBeFollowedBy( c, true ) );
                    Debug.Assert( !afterMe.Contains( this ) );
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

            public bool CanBeFollowedBy( SCRClass other, bool allowFinalRank )
            {
                return other != this
                        && (RankOrdered >= other.RankOrdered || (allowFinalRank && other.RankOrdered == Int32.MaxValue));
            }

            public int RankOrdered => Class.ContainerItem?.RankOrdered ?? Int32.MaxValue;

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
                var content = new HashSet<SCRClass>();
                foreach( var ac in a )
                {
                    if( ac.Value != null )
                    {
                        content.Add( ac.Value.Class );
                        content.UnionWith( ac.Value.Content );
                    }
                }
                Content = content;
            }

            public IReadOnlyCollection<SCRClass> Content { get; }

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

        interface IBuildClassInfoContext
        {
            BuildClassInfo this[SCRClass c] { get; }

            void Register( SCRClass c, BuildClassInfo info );

            //IBuildClassInfoContext CreateSubCache();
        }

        class BuildClassInfoCache : IBuildClassInfoContext
        {
            readonly InterfaceFamily _f;
            readonly Dictionary<SCRClass, BuildClassInfo> _c;
            readonly List<BuildClassInfo> _solved;

            class SubCache : IBuildClassInfoContext
            {
                readonly BuildClassInfoCache _primary;
                readonly IBuildClassInfoContext _parent;
                readonly Dictionary<SCRClass, BuildClassInfo> _c;

                public SubCache( BuildClassInfoCache primary )
                {
                    _parent = _primary = primary;
                    _c = new Dictionary<SCRClass, BuildClassInfo>();
                }

                public SubCache( SubCache parent )
                {
                    _parent = parent;
                    _primary = parent._primary;
                    _c = new Dictionary<SCRClass, BuildClassInfo>();
                }

                public BuildClassInfo this[SCRClass c] => _c.GetValueWithDefault( c, null ) ?? _parent[c];

                public void Register( SCRClass c, BuildClassInfo info )
                {
                    Debug.Assert( this[c] == null );
                    _c[c] = info;
                    if( info.Class.IsHeadCandidate && info.Content.Count + 1 == _primary._f.Classes.Count )
                    {
                        _primary._solved.Add( info );
                    }
                }

                IBuildClassInfoContext CreateSubCache() => new SubCache( this );
            }

            public BuildClassInfoCache( InterfaceFamily f )
            {
                _f = f;
                _c = new Dictionary<SCRClass, BuildClassInfo>();
                foreach( var c in f.Classes ) _c.Add( c, null );
                _solved = new List<BuildClassInfo>();
            }

            public void Register( SCRClass c, BuildClassInfo info )
            {
                Debug.Assert( this[c] == null );
                _c[c] = info;
                if( info.Class.IsHeadCandidate && info.Content.Count + 1 == _f.Classes.Count )
                {
                    _solved.Add( info );
                }
            }

            public IBuildClassInfoContext CreateSubCache() => new SubCache( this );

            public BuildClassInfo this[ SCRClass c ] => _c[c];

            public IEnumerable<SCRClass> Unregistered => _c.Where( kv => kv.Value == null ).Select( kv => kv.Key );

            public IReadOnlyList<BuildClassInfo> CurrentlySolved => _solved;

            public bool Finalize( IActivityMonitor m, StObjObjectEngineMap engineMap )
            {
                if( _solved.Count == 1 )
                {
                    _f.FinalRegister( engineMap, _solved[0] );
                    return true;
                }
                if( _solved.Count > 1 )
                {
                    using( m.OpenError( $"Multiple possible chains found:" ) )
                    {
                        foreach( var c in _solved ) m.Trace( c.ToString() );
                    }
                }
                return false;
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
                var cache = new BuildClassInfoCache( f );
                // Primary round: no null gates opened.
                OneRound( f, cache );
                success = cache.Finalize( _monitor, engineMap );
                if( !success && cache.CurrentlySolved.Count == 0 )
                {
                    var nullGates = cache.Unregistered.SelectMany( c => c.Parameters.Where( p => p.HasDefault ) ).ToList();
                    _monitor.Debug( $"{nullGates.Count} null gates." );
                    if( nullGates.Count > 0 )
                    {
                        // Secondary round: only one null gate opened at a time,
                        // but all these hypothesis must lead to one and only one
                        // chain. The subcache isolates the intermediate infos that may
                        // be created during the loop and solved chains are added to the
                        // root solved chains.
                        // Finalize then does its job and check that only one solved chain
                        // has been found.
                        var oneNullAllowed = cache.CreateSubCache();
                        SCRClass.CtorParameter prev = null;
                        for( int nullIdx = 0; nullIdx < nullGates.Count; ++nullIdx )
                        {
                            if( prev != null ) prev.CanUseDefault = false;
                            (prev = nullGates[nullIdx]).CanUseDefault = true;
                            OneRound( f, oneNullAllowed );
                        }
                        prev.CanUseDefault = false;
                    }
                    success = cache.Finalize( _monitor, engineMap );
                }
            }
            return success;
        }

        void OneRound( InterfaceFamily f, IBuildClassInfoContext cache )
        {
            bool foundNewOne;
            do
            {
                foundNewOne = false;
                foreach( var c in f.Classes )
                {
                    if( cache[c] == null )
                    {
                        foundNewOne |= BuildChain( c, f.Classes, cache ) != null;
                    }
                }
            }
            while( foundNewOne );
        }

        BuildClassInfo BuildChain(
            SCRClass head,
            IEnumerable<SCRClass> remainders,
            IBuildClassInfoContext cache )
        {
            var result = cache[head];
            if( result != null ) return result;
            using( _monitor.OpenDebug( () => $"Resolving {head}." ) )
            {
                // Currently parameter order matters.
                // To be parameter order indepedent we whould use a local cache here
                // and run the loop like the primary one (until none succeed).
                // This is already done by the primary lopp so this is useless.
                remainders = remainders.Where( r => r != head );
                var bindings = new ParameterAssignment[head.Parameters.Count];
                for( int i = 0; i < bindings.Length; ++i )
                {
                    var p = head.Parameters[i];
                    var cInfo = BindParameter( head.Parameters[i], remainders, cache );
                    if( cInfo == null )
                    {
                        _monitor.Debug( $"Failed to resolve '{head.Class.Type.Name}' considering classes: '{remainders.Select( r => r.Class.Type.Name ).Concatenate("', '")}'." );
                        return null;
                    }
                    bindings[i] = new ParameterAssignment( p, cInfo );
                }
                result = new BuildClassInfo( head, bindings );
                cache.Register( head, result );
                _monitor.Trace( $"Chain found: {result}." );
                return result;
            }
        }

        BuildClassInfo BindParameter( SCRClass.CtorParameter p, IEnumerable<SCRClass> remainders, IBuildClassInfoContext cache )
        {
            var possible = p.StatisficationSet
                                .Intersect( remainders )
                                .ToList();
            SCRClass unambiguousPossible = null;
            if( possible.Count == 0 )
            {
                if( p.CanUseDefault )
                {
                    _monitor.Trace( $"Using null default for parameter {p}." );
                    return BuildClassInfo.NullValue;
                }
                _monitor.Trace( $"Unable to bind parameter {p}." );
                return null;
            }
            if( possible.Count == 1 ) unambiguousPossible = possible[0];
            else
            {
                _monitor.Debug( () => $"Ambiguous parameter candidates for {p} : {possible.Select( t => t.Class.Type.Name ).Concatenate()}." );
                // Removes the pure tail (tail candidate and no other parameter) if any.
                possible.RemoveAll( c => c.IsTailCandidate && c.Parameters.Count == 0 );
                if( possible.Count == 1 )
                {
                    unambiguousPossible = possible[0];
                    _monitor.Debug( "Solved by removing necessary tail." );
                }
                else
                {
                    // Considering thet a unique best Rank is the one.
                    int maxRank = possible.Select( c => c.RankOrdered ).Max();
                    possible.RemoveAll( c => c.RankOrdered < maxRank );
                    if( possible.Count == 1 )
                    {
                        unambiguousPossible = possible[0];
                        _monitor.Debug( "Solved by max RankOrdered." );
                    }
                    else
                    {
                        // Rank conflict. Use the cache to keep only the top of any
                        // already resolved chains.
                        var covered = new HashSet<SCRClass>( possible
                                                                .Select( pc => cache[ pc ]?.Content )
                                                                .Where( content => content != null )
                                                                .SelectMany( content => content ) );
                        unambiguousPossible = possible.SingleOrDefault( c => !covered.Contains( c ) );
                        if( unambiguousPossible != null )
                        {
                            _monitor.Debug( "Solved by already built chains." );
                        }
                    }
                }
                if( unambiguousPossible == null )
                {
                    _monitor.Trace( $"Ambiguous binding for parameter {p} between {possible.Select( t => t.Class.Type.Name ).Concatenate()}." );
                    return null;
                }
            }
            var result = BuildChain( unambiguousPossible, remainders, cache );
            return result;
        }

    }
}
