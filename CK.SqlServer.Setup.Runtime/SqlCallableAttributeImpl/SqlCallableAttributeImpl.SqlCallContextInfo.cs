using CK.CodeGen;
using CK.CodeGen.Abstractions;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    public partial class SqlCallableAttributeImpl
    {

        /// <summary>
        /// Unifies multiple ISqlCallContext parameters.
        /// </summary>
        class SqlCallContextInfo
        {
            enum GenerateCallType
            {
                None,
                ExecuteNonQuery,
                ExecuteNonQueryAsync,
                FuncBuilderHelper
            }

            readonly GenerationType _gType;
            readonly List<Property> _props;
            readonly ParameterInfo _cancellationTokenParam;
            readonly GenerateCallType _generateCallType;
            readonly string _sourceExecutorCallNonQuery;

            // Only the first one that supports ISqlCallContext interests us. 
            ParameterInfo _sqlCallContextParameter;

            public class Property
            {
                public readonly ParameterInfo Parameter;
                public readonly PropertyInfo Prop;
                public readonly Type PocoMappedType;

                public Property( ParameterInfo param, PropertyInfo prop, Type pocoMappedType )
                {
                    Parameter = param;
                    Prop = prop;
                    PocoMappedType = pocoMappedType;
                }

                internal bool Match( ISqlServerParameter sqlP, IActivityMonitor monitor )
                {
                    if( StringComparer.OrdinalIgnoreCase.Equals( '@' + Prop.Name, sqlP.Name ) )
                    {
                        if( sqlP.SqlType.IsTypeCompatible( Prop.PropertyType ) )
                        {
                            monitor.Info( $"Sql Parameter '{sqlP.ToStringClean()}' will take its value from the [ParameterSource] '{Parameter.Name}.{Prop.Name}' property." );
                            return true;
                        }
                    }
                    return false;
                }
            }

            public SqlCallContextInfo( GenerationType gType, Type returnedType, ParameterInfo[] methodParameters )
            {
                _gType = gType;
                _props = new List<Property>();
                if( (_gType & GenerationType.IsCall) != 0 )
                {
                    if( returnedType == typeof(Task) )
                    {
                        _cancellationTokenParam = methodParameters.FirstOrDefault( p => p.ParameterType == typeof( CancellationToken ) );
                        _generateCallType = GenerateCallType.ExecuteNonQueryAsync;
                        _sourceExecutorCallNonQuery = "ExecuteNonQueryAsync";
                    }
                    else if( returnedType.IsGenericType
                             && returnedType.GetGenericTypeDefinition() == typeof(Task<>) )
                    {
                        _cancellationTokenParam = methodParameters.FirstOrDefault( p => p.ParameterType == typeof( CancellationToken ) );
                        var ret = returnedType.GetGenericArguments()[0];
                        _generateCallType = GenerateCallType.FuncBuilderHelper;
                        _sourceExecutorCallNonQuery = $"FuncBuilderHelper<{ret.ToCSharpName(true)}>";
                    }
                    else
                    {
                        _generateCallType = GenerateCallType.ExecuteNonQuery;
                        _sourceExecutorCallNonQuery = "ExecuteNonQuery";
                    }
                }
            }

            public bool AddParameterSourceOrSqlCallContext( ParameterInfo param, IActivityMonitor monitor, IPocoSupportResult poco )
            {
                Type paramType = param.ParameterType;
                if( paramType.IsValueType || typeof( string ).IsAssignableFrom( paramType ) ) return false;

                bool isParameterSource = param.GetCustomAttribute<ParameterSourceAttribute>() != null;
                bool isParameterSourcePoco = isParameterSource && typeof( IPoco ).IsAssignableFrom( param.ParameterType );
                bool needExecutor = !isParameterSourcePoco && ( _gType & GenerationType.IsCall) != 0 && _sqlCallContextParameter == null;
                if( isParameterSource || needExecutor )
                {
                    Type pocoMappedType = null;
                    IEnumerable<PropertyInfo> rawProperties = null;
                    if( isParameterSourcePoco )
                    {
                        pocoMappedType = poco.Find( param.ParameterType ).Root.PocoClass;
                        if( pocoMappedType == null ) throw new Exception( $"Unmapped Poco for {param.ParameterType.FullName}." );
                        rawProperties = pocoMappedType.GetProperties();
                    }
                    else
                    {
                        rawProperties = paramType.IsInterface
                                                ? ReflectionHelper.GetFlattenProperties( param.ParameterType )
                                                : param.ParameterType.GetProperties();
                    }
                    var allProperties = rawProperties.Select( p => new Property( param, p, pocoMappedType ) ).ToList();
                    if( isParameterSource )
                    {
                        _props.AddRange( allProperties );
                    }
                    if( needExecutor )
                    {
                        if( typeof( ISqlCallContext ).IsAssignableFrom( param.ParameterType ) )
                        {
                            monitor.Debug( $"ISqlCallContext: using parameter '{param.Name}'." );
                            _sqlCallContextParameter = param;
                            return true;
                        }
                    }
                }
                return isParameterSource;
            }

            public bool MatchPropertyToSqlParameter( SqlParameterHandlerList.SqlParamHandler setter, IActivityMonitor monitor )
            {
                foreach( var p in _props )
                {
                    if( p.Match( setter.SqlExprParam, monitor ) )
                    {
                        setter.SetParameterMapping( p );
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Gets the parameter that must support the call (when GenerationType.IsCall is set).
            /// Null if not found or if we are not generating call.
            /// </summary>
            public ParameterInfo SqlCommandExecutorParameter => _sqlCallContextParameter; 

            /// <summary>
            /// Gets whether a Func{SqlCommand,T} is required to call the procedure.
            /// It is necessarily an async call (for synchronous calls, the return code is inlined).
            /// </summary>
            public bool RequiresReturnTypeBuilder => _generateCallType == GenerateCallType.FuncBuilderHelper; 

            /// <summary>
            /// Gets whether this is an asynchronous call.
            /// </summary>
            public bool IsAsyncCall => _generateCallType == GenerateCallType.ExecuteNonQueryAsync || _generateCallType == GenerateCallType.FuncBuilderHelper; 

            public void GenerateExecuteNonQueryCall( ICodeWriter b, string varCommandName, string resultBuilderName, ParameterInfo[] callingParameters )
            {
                b.Append( _sqlCallContextParameter.Name )
                    .Append( "[Database]." )
                    .Append( _sourceExecutorCallNonQuery )
                    .Append( "(" )
                    .Append( varCommandName );
                if( resultBuilderName != null )
                {
                    b.Append( "," ).Append( resultBuilderName );
                }
                if( IsAsyncCall )
                {
                    b.Append( "," );
                    if( _cancellationTokenParam != null )
                    {
                        b.Append( callingParameters[_cancellationTokenParam.Position].Name );
                    }
                    else b.Append( "default(System.Threading.CancellationToken)" );
                }
                b.Append( ");" ).NewLine();
            }

            /// <summary>
            /// Centralized helper that states whether a parameter carries parameter values.
            /// </summary>
            /// <param name="mP">The parameter info.</param>
            /// <returns>True for parameter that are parameter sources.</returns>
            static internal bool IsSqlParameterSource( ParameterInfo mP )
            {
                return !(mP.ParameterType.IsValueType || typeof(string).IsAssignableFrom( mP.ParameterType ))
                        && mP.GetCustomAttribute<ParameterSourceAttribute>() != null;
            }

        }

    }
}
