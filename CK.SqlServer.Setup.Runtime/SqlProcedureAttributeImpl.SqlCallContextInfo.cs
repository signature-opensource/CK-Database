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

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl
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

            // Only the first one that supports ISqlCommandExecutor interests us. 
            ParameterInfo _sqlCommandExecutorParameter;
            MethodInfo _sqlCommandExecutorMethodGetter;
            
            public class Property
            {
                public readonly ParameterInfo Parameter;
                public readonly PropertyInfo Prop;

                public Property( ParameterInfo param, PropertyInfo prop )
                {
                    Parameter = param;
                    Prop = prop;
                }

                internal bool Match( SqlExprParameter sqlP, IActivityMonitor monitor )
                {
                    if( StringComparer.OrdinalIgnoreCase.Equals( '@' + Prop.Name, sqlP.Variable.Identifier.Name ) )
                    {
                        if( sqlP.Variable.TypeDecl.ActualType.IsTypeCompatible( Prop.PropertyType ) )
                        {
                            monitor.Info().Send( "Sql Parameter '{0}' will take its value from the SqlCallContext parameter '{1}' property '{2}'.", sqlP.ToStringClean(), Parameter.Name, Prop.Name );
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
                        _executorCallNonQuery = _cancellationTokenParam != null ? SqlObjectItem.MExecutorCallNonQueryAsyncCancellable : SqlObjectItem.MExecutorCallNonQueryAsync;
                        _returnedType = typeof(void);
                    }
                    else if( returnedType.IsGenericType && returnedType.GetGenericTypeDefinition() == typeof(Task<>) )
                    {
                        _cancellationTokenParam = methodParameters.FirstOrDefault( p => p.ParameterType == typeof( CancellationToken ) );
                        _executorCallNonQuery = _cancellationTokenParam != null ? SqlObjectItem.MExecutorCallNonQueryAsyncTypedCancellable : SqlObjectItem.MExecutorCallNonQueryAsyncTyped;
                        _returnedType = returnedType.GetGenericArguments()[0];
                    }
                    else
                    {
                        _executorCallNonQuery = SqlObjectItem.MExecutorCallNonQuery;
                        _returnedType = returnedType;
                    }
                }
            }
 
            public bool Add( ParameterInfo param, IActivityMonitor monitor )
            {
                var properties = param.ParameterType.IsInterface ? ReflectionHelper.GetFlattenProperties( param.ParameterType ) : param.ParameterType.GetProperties();
                _props.AddRange( properties.Select( p => new Property( param, p ) ) );
                
                if( (_gType & GenerationType.IsCall) != 0 && _sqlCommandExecutorParameter == null )
                {
                    Debug.Assert( _gType == GenerationType.ExecuteNonQuery );
                    if( typeof(ISqlCommandExecutor).IsAssignableFrom( param.ParameterType ) )
                    {
                        _sqlCommandExecutorParameter = param;
                        monitor.Trace().Send( "Planning to use parameter '{0}' {1} method.", param.Name, _executorCallNonQuery.Name );
                    }
                    else
                    {
                        PropertyInfo pE = _props.Select( p => p.Prop ).FirstOrDefault( p => p.Name == "Executor" && typeof( ISqlCommandExecutor ).IsAssignableFrom( p.PropertyType ) );
                        if( pE != null )
                        {
                            _sqlCommandExecutorParameter = param;
                            _sqlCommandExecutorMethodGetter = pE.GetGetMethod();
                            monitor.Trace().Send( "Planning to use parameter '{0}.Executor' property {1} method.", param.Name, _executorCallNonQuery.Name );
                        }
                        else
                        {
                            var methods = param.ParameterType.IsInterface ? ReflectionHelper.GetFlattenMethods( param.ParameterType ) : param.ParameterType.GetMethods();
                            MethodInfo mE = methods.FirstOrDefault( m => m.Name == "GetExecutor" && m.GetParameters().Length == 0 && typeof( ISqlCommandExecutor ).IsAssignableFrom( m.ReturnType ) );
                            if( mE != null )
                            {
                                _sqlCommandExecutorParameter = param;
                                _sqlCommandExecutorMethodGetter = mE;
                                monitor.Trace().Send( "Planning to use parameter '{0}.GetExecutor()' method {1} method.", param.Name, _executorCallNonQuery.Name );
                            }
                        }

                    }
                }
                return true;
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
            public ParameterInfo SqlCommandExecutorParameter
            {
                get { return _sqlCommandExecutorParameter; }
            }

            /// <summary>
            /// Gets whether a Func{SqlCommand,T} is required to call the procedure.
            /// It is necessarily an async call (for synchronous calls, the return code is inlined).
            /// </summary>
            public bool RequiresReturnTypeBuilder
            {
                get { return _executorCallNonQuery == SqlObjectItem.MExecutorCallNonQueryAsyncTyped || _executorCallNonQuery == SqlObjectItem.MExecutorCallNonQueryAsyncTypedCancellable; }
            }

            /// <summary>
            /// Gets whether this is an asynchronous call.
            /// </summary>
            public bool IsAsyncCall
            {
                get { return _executorCallNonQuery != null && _executorCallNonQuery != SqlObjectItem.MExecutorCallNonQuery; }
            }

            /// <summary>
            /// Emits call to SqlCommandExecutorParameter.ExecuteNonQuery( string, SqlCommand ) method.
            /// </summary>
            /// <param name="g">The IL genrator.</param>
            /// <param name="localSqlCommand">The SqlCommand local variable.</param>
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
                    g.LdArg( _cancellationTokenParam.Position + 1 );
                }
                g.Emit( OpCodes.Callvirt, toCall );
            }

            /// <summary>
            /// Centralized helper that states whether a parameter is a <see cref="ISqlCallContext"/> object.
            /// </summary>
            /// <param name="mP">The parameter info.</param>
            /// <returns>True for ISqlCallContext parameter.</returns>
            static public bool IsSqlCallContext( ParameterInfo mP )
            {
                Type t = mP.ParameterType;
                return typeof( ISqlCallContext ).IsAssignableFrom( t );
            }

        }

    }
}
