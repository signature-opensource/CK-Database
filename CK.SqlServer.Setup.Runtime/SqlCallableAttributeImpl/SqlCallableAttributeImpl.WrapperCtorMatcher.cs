using CK.CodeGen;
using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CK.SqlServer.Setup;

public partial class SqlCallableAttributeImpl
{
    class WrapperCtorMatcher
    {
        public readonly ConstructorInfo Ctor;
        public readonly IReadOnlyList<ParameterInfo> Parameters;
        // Contains either:
        // - ParameterInfo from _methodParameters or
        // - ParameterInfo from our Parameters if the default value exists and must be used or
        // - The _declaringTypeMarker.
        readonly ParameterInfo[] _mappedParameters;
        readonly MethodParameter[] _methodParameters;
        readonly int _idxSqlCommand;
        readonly Type _declaringType;
        readonly static ParameterInfo _declaringTypeMarker = typeof( WrapperCtorMatcher ).GetConstructors()[0].GetParameters()[2];
        readonly StringBuilder _warnings;

        public WrapperCtorMatcher( ConstructorInfo m, IReadOnlyList<ParameterInfo> methodParameters, Type declaringType )
        {
            Ctor = m;
            Parameters = m.GetParameters();
            _declaringType = declaringType;
            _mappedParameters = new ParameterInfo[Parameters.Count];
            _methodParameters = methodParameters.Select( p => new MethodParameter( p ) ).ToArray();
            _idxSqlCommand = Parameters.IndexOf( p => p.ParameterType == SqlObjectItem.TypeCommand && !p.ParameterType.IsByRef && !p.HasDefaultValue );
            _warnings = new StringBuilder();
        }

        class MethodParameter
        {
            public readonly ParameterInfo Parameter;
            public int IdxTarget;

            public MethodParameter( ParameterInfo p )
            {
                Parameter = p;
                IdxTarget = -1;
            }
        }

        public bool HasSqlCommand
        {
            get { return _idxSqlCommand >= 0; }
        }

        internal bool IsCallable()
        {
            Debug.Assert( _idxSqlCommand >= 0 );
            Debug.Assert( _methodParameters.All( p => p.IdxTarget == -1 ) );
            Debug.Assert( _mappedParameters.All( p => p == null ) );

            for( int i = 0; i < Parameters.Count; ++i )
            {
                if( i == _idxSqlCommand ) continue;

                ParameterInfo toMatch = Parameters[i];
                Debug.Assert( toMatch.Position == i );

                var exactCandidates = _methodParameters.Where( p => p.IdxTarget == -1
                                                                && toMatch.ParameterType.Equals( p.Parameter.ParameterType )
                                                                && toMatch.IsOut == p.Parameter.IsOut ).ToList();
                if( TrySetCandidate( toMatch, exactCandidates ) ) continue;
                var candidates = _methodParameters.Where( p => p.IdxTarget == -1
                                                            && !toMatch.ParameterType.IsByRef
                                                            && !p.Parameter.ParameterType.IsByRef
                                                            && !toMatch.ParameterType.Equals( p.Parameter.ParameterType )
                                                            && toMatch.ParameterType.IsAssignableFrom( p.Parameter.ParameterType ) ).ToList();
                if( TrySetCandidate( toMatch, candidates ) ) continue;
                // No method parameter found. Try the declaring type itself, or the default value.
                if( !toMatch.ParameterType.IsByRef )
                {
                    if( toMatch.ParameterType.IsAssignableFrom( _declaringType ) )
                    {
                        _mappedParameters[i] = _declaringTypeMarker;
                    }
                    else if( IsValidDefaultValue( toMatch ) ) _mappedParameters[i] = toMatch;
                }
            }
            return _methodParameters.All( p => p.IdxTarget >= 0 || SqlCallContextInfo.IsSqlParameterSource( p.Parameter ) )
                    && !_mappedParameters.Where( ( p, idx ) => idx != _idxSqlCommand && p == null ).Any();
        }

        internal void ExplainFailure( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( $"Considering constructor: {DumpParameters( Parameters, true )}." ) )
            {
                for( int idx = 0; idx < _mappedParameters.Length; ++idx )
                {
                    if( idx == _idxSqlCommand ) continue;
                    var cP = _mappedParameters[idx];
                    if( cP == null )
                    {
                        cP = Parameters[idx];
                        if( cP.HasDefaultValue ) monitor.Error( $"Parameter '{DumpParameter( cP )}' is not bound (and using its default value is not possible)." );
                        else monitor.Error( $"Parameter '{DumpParameter( cP )}' is not bound." );
                    }
                    else if( cP == _declaringTypeMarker )
                    {
                        monitor.Trace( $"Parameter '{Parameters[idx]}' is bound to the Type that defines the method ({_declaringType.FullName})." );
                    }
                    else if( cP.Member == Ctor )
                    {
                        monitor.Trace( $"Parameter '{DumpParameter( cP )}' uses its default value." );
                    }
                    else
                    {
                        monitor.Trace( $"Parameter '{Parameters[idx]}' is bound to method parameter '{DumpParameter( cP )}'." );
                    }
                }
                foreach( var mP in _methodParameters.Where( p => p.IdxTarget == -1 ) )
                {
                    if( SqlCallContextInfo.IsSqlParameterSource( mP.Parameter ) )
                        monitor.Trace( $"Method parameter '{DumpParameter( mP.Parameter )}' is a parameter source." );
                    else monitor.Error( $"Unable to map extra method parameter '{DumpParameter( mP.Parameter )}'." );
                }
            }
        }

        internal void LogWarnings( IActivityMonitor monitor )
        {
            if( _warnings.Length > 0 ) monitor.Warn( _warnings.ToString() );
        }

        internal void LdParameters( ICodeWriter b, string varCmdName, ParameterInfo[] callingParameters )
        {
            int i = 0;
            foreach( var mP in _mappedParameters )
            {
                if( i > 0 ) b.Append( ", " );
                if( i == _idxSqlCommand )
                {
                    b.Append( varCmdName );
                }
                else if( mP == _declaringTypeMarker )
                {
                    b.Append( "(" ).AppendCSharpName( Parameters[i].ParameterType, true, true, true ).Append( ")this" );
                }
                else if( mP.Member == Ctor )
                {
                    Debug.Assert( IsValidDefaultValue( mP ) );
                    Debug.Assert( mP.Position == i, "This is the ParameterInfo of the constructor." );
                    b.Append( mP.DefaultValue );
                }
                else
                {
                    b.AppendVariable( callingParameters[mP.Position].Name );
                }
                ++i;
            }
        }

        static bool IsValidDefaultValue( ParameterInfo p )
        {
            if( !p.HasDefaultValue ) return false;
            object d = p.DefaultValue;
            if( d == null ) return true;
            Type dT = d.GetType();
            if( dT.Equals( typeof( int ) ) || dT.Equals( typeof( Int16 ) ) || dT.Equals( typeof( sbyte ) ) ) return true;
            if( dT.Equals( typeof( string ) ) ) return true;
            if( dT.Equals( typeof( double ) ) ) return true;
            if( dT.Equals( typeof( float ) ) ) return true;
            return false;
        }

        bool TrySetCandidate( ParameterInfo toMatch, IEnumerable<MethodParameter> candidates )
        {
            var c = candidates.ToList();
            if( c.Count == 1 )
            {
                var only = c[0];
                SetMatch( toMatch.Position, only );
                if( only.Parameter.Name != toMatch.Name )
                {
                    if( _warnings.Length > 0 ) _warnings.AppendLine();
                    _warnings.AppendFormat( "Parameter {0} has been mapped to method parameter {1} because it was the only candidate in terms of Type. Both parameters SHOULD use the same name.", toMatch.Name, only.Parameter.Name );
                }
            }
            else
            {
                var byName = c.FirstOrDefault( mp => mp.Parameter.Name == toMatch.Name );
                if( byName != null ) SetMatch( toMatch.Position, byName );
                else return false;
            }
            return true;
        }

        void SetMatch( int iParameter, MethodParameter methodParam )
        {
            _mappedParameters[iParameter] = methodParam.Parameter;
            methodParam.IdxTarget = iParameter;
        }
    }

}
