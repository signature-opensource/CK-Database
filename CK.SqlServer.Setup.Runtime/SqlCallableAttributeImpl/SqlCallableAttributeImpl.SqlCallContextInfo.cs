#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlProcedureAttributeImpl.SqlCallContextInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;
using CK.CodeGen;
using CK.CodeGen.Abstractions;

namespace CK.SqlServer.Setup
{
    public partial class SqlCallableAttributeImpl
    {

        /// <summary>
        /// Unifies multiple ISqlCallContext parameters.
        /// </summary>
        class SqlCallContextInfo
        {
            readonly GenerationType _gType;
            readonly List<Property> _props;
            readonly Type _returnedType;
            readonly ParameterInfo _cancellationTokenParam;
            readonly MethodInfo _executorCallNonQuery;
            readonly string _sourceExecutorCallNonQuery;

            // Only the first one that supports ISqlCommandExecutor interests us. 
            ParameterInfo _sqlCommandExecutorParameter;
            MethodInfo _sqlCommandExecutorMethodGetter;
            string _sourceExecutor;

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
                        _executorCallNonQuery = SqlObjectItem.MExecutorCallNonQueryAsync;
                        _returnedType = typeof(void);
                        _sourceExecutorCallNonQuery = "ExecuteNonQueryAsync";
                    }
                    else if( returnedType.GetTypeInfo().IsGenericType && returnedType.GetGenericTypeDefinition() == typeof(Task<>) )
                    {
                        _cancellationTokenParam = methodParameters.FirstOrDefault( p => p.ParameterType == typeof( CancellationToken ) );
                        _executorCallNonQuery = SqlObjectItem.MExecutorCallNonQueryAsyncTyped;
                        _returnedType = returnedType.GetGenericArguments()[0];
                        _sourceExecutorCallNonQuery = $"ExecuteNonQueryAsyncTyped<{_returnedType.ToCSharpName(true)}>";
                    }
                    else
                    {
                        _executorCallNonQuery = SqlObjectItem.MExecutorCallNonQuery;
                        _returnedType = returnedType;
                        _sourceExecutorCallNonQuery = "ExecuteNonQuery";
                    }
                }
            }

            public bool AddParameterSourceAndSqlCommandExecutor( ParameterInfo param, IActivityMonitor monitor, IPocoSupportResult poco )
            {
                TypeInfo paramTypeInfo = param.ParameterType.GetTypeInfo();
                if( paramTypeInfo.IsValueType || typeof( string ).IsAssignableFrom( param.ParameterType ) ) return false;

                bool isParameterSource = param.GetCustomAttribute<ParameterSourceAttribute>() != null;
                bool isParameterSourcePoco = isParameterSource && typeof( IPoco ).IsAssignableFrom( param.ParameterType );
                bool needExecutor = !isParameterSourcePoco && ( _gType & GenerationType.IsCall) != 0 && _sqlCommandExecutorParameter == null;
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
                        rawProperties = paramTypeInfo.IsInterface
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
                        Debug.Assert( _gType == GenerationType.ExecuteNonQuery );
                        if( typeof( ISqlCommandExecutor ).IsAssignableFrom( param.ParameterType ) )
                        {
                            _sqlCommandExecutorParameter = param;
                            monitor.Trace( $"Planning to use parameter '{param.Name}' {_executorCallNonQuery.Name} method." );
                            _sourceExecutor = $"((CK.SqlServer.ISqlCommandExecutor){param.Name})";
                            return true;
                        }
                        PropertyInfo pE = allProperties.Select( p => p.Prop ).FirstOrDefault( p => p.Name == "Executor" && typeof( ISqlCommandExecutor ).IsAssignableFrom( p.PropertyType ) );
                        if( pE != null )
                        {
                            _sqlCommandExecutorParameter = param;
                            _sqlCommandExecutorMethodGetter = pE.GetGetMethod();
                            monitor.Trace( $"Planning to use parameter '{param.Name}.Executor' property {_executorCallNonQuery.Name} method." );
                            _sourceExecutor = $"{param.Name}.Executor";
                            return true;
                        }
                        var methods = paramTypeInfo.IsInterface 
                                        ? ReflectionHelper.GetFlattenMethods( param.ParameterType ) 
                                        : param.ParameterType.GetMethods();
                        MethodInfo mE = methods.FirstOrDefault( m => m.Name == "GetExecutor" && m.GetParameters().Length == 0 && typeof( ISqlCommandExecutor ).IsAssignableFrom( m.ReturnType ) );
                        if( mE != null )
                        {
                            _sqlCommandExecutorParameter = param;
                            _sqlCommandExecutorMethodGetter = mE;
                            monitor.Trace( $"Planning to use parameter '{param.Name}.GetExecutor()' method {_executorCallNonQuery.Name} method." );
                            _sourceExecutor = $"{param.Name}.GetExecutor()";
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
            public ParameterInfo SqlCommandExecutorParameter => _sqlCommandExecutorParameter; 

            /// <summary>
            /// Gets whether a Func{SqlCommand,T} is required to call the procedure.
            /// It is necessarily an async call (for synchronous calls, the return code is inlined).
            /// </summary>
            public bool RequiresReturnTypeBuilder => _executorCallNonQuery == SqlObjectItem.MExecutorCallNonQueryAsyncTyped; 

            /// <summary>
            /// Gets whether this is an asynchronous call.
            /// </summary>
            public bool IsAsyncCall => _executorCallNonQuery != null && _executorCallNonQuery != SqlObjectItem.MExecutorCallNonQuery; 

            /// <summary>
            /// Emits call to SqlCommandExecutorParameter.ExecuteNonQuery( string, SqlCommand ) method.
            /// </summary>
            /// <param name="g">The IL generator.</param>
            /// <param name="localSqlCommand">The SqlCommand local variable.</param>
            /// <param name="resultBuilder">Field that holds the generated function (when <see cref="RequiresReturnTypeBuilder"/> is true).</param>
            public void GenerateExecuteNonQueryCall( ILGenerator g, LocalBuilder localSqlCommand, FieldInfo resultBuilder )
            {
                Debug.Assert( _executorCallNonQuery != null && (RequiresReturnTypeBuilder == (resultBuilder != null)) );
                g.LdArg( _sqlCommandExecutorParameter.Position + 1 );
                if( _sqlCommandExecutorMethodGetter != null )
                {
                    g.Emit( OpCodes.Callvirt, _sqlCommandExecutorMethodGetter );
                }
                g.Emit( OpCodes.Ldarg_0 );
                g.Emit( OpCodes.Call, SqlObjectItem.MGetDatabase );
                g.Emit( OpCodes.Call, SqlObjectItem.MDatabaseGetConnectionString );
                g.LdLoc( localSqlCommand );
                MethodInfo toCall;
                if( resultBuilder == null )
                {
                    toCall = _executorCallNonQuery;
                }
                else
                {
                    Debug.Assert( resultBuilder.FieldType.GetGenericTypeDefinition() == typeof( Func<,> ) );
                    Debug.Assert( resultBuilder.FieldType.GetGenericArguments()[0] == SqlObjectItem.TypeCommand );
                    Debug.Assert( resultBuilder.FieldType.GetGenericArguments()[1] == _returnedType );
                    Debug.Assert( _executorCallNonQuery.IsGenericMethodDefinition );
                    g.Emit( OpCodes.Ldsfld, resultBuilder );
                    toCall = _executorCallNonQuery.MakeGenericMethod( _returnedType );
                }
                if( _cancellationTokenParam != null )
                {
                    Debug.Assert( IsAsyncCall );
                    g.LdArg( _cancellationTokenParam.Position + 1 );
                }
                else if( IsAsyncCall )
                {
                    LocalBuilder tDef = g.DeclareLocal( typeof( CancellationToken ) );
                    g.Emit( OpCodes.Ldloca_S, tDef );
                    g.Emit( OpCodes.Initobj, typeof( CancellationToken ) );
                    g.LdLoc( tDef );
                }
                g.Emit( OpCodes.Callvirt, toCall );
            }

            public void GenerateExecuteNonQueryCall( ICodeWriter b, string varCommandName, string resultBuilderName, ParameterInfo[] callingParameters )
            {
                b.Append( _sourceExecutor )
                    .Append( "." )
                    .Append( _sourceExecutorCallNonQuery )
                    .Append( "(Database.ConnectionString," )
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
                return !(mP.ParameterType.GetTypeInfo().IsValueType || typeof(string).IsAssignableFrom( mP.ParameterType ))
                        && mP.GetCustomAttribute<ParameterSourceAttribute>() != null;
            }

        }

    }
}
