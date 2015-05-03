#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlProcedureAttributeImpl.SqlCallContextInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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

            // Only the first one that offers ExecuteNonQuery( string, SqlCommand ) interests us. 
            ParameterInfo _callProviderParameter;
            MethodInfo _callProviderParameterMethod;
            
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

            public SqlCallContextInfo( GenerationType gType )
            {
                _gType = gType;
                _props = new List<Property>();
            }

            public bool Add( ParameterInfo param, IActivityMonitor monitor )
            {
                if( param.ParameterType.IsInterface )
                {
                    _props.AddRange( ReflectionHelper.GetFlattenProperties( param.ParameterType ).Select( p => new Property( param, p ) ) );
                }
                else _props.AddRange( param.ParameterType.GetProperties().Select( p => new Property( param, p ) ) );
                if( (_gType&GenerationType.IsCall) != 0 && _callProviderParameter == null )
                {
                    if( _gType == GenerationType.ExecuteNonQuery )
                    {
                        _callProviderParameterMethod = param.ParameterType.GetMethod( "ExecuteNonQuery", SqlObjectItem.ExecuteCallMethodParameters );
                        if( _callProviderParameterMethod != null )
                        {
                            _callProviderParameter = param;
                            monitor.Trace().Send( "Using ExecuteNonQuery() method from parameter '{0}'.", param.Name );
                        }
                    }
                }
                return true;
            }

            public bool FindMatchingProperty( SqlParametersHandler.SqlParamHandler setter, IActivityMonitor monitor )
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
            /// Null if not found.
            /// </summary>
            public ParameterInfo SelectedCallProviderParameter
            {
                get { return _callProviderParameter; }
            }

            /// <summary>
            /// Emits call to SelectedCallProviderParameter.ExecuteNonQuery( string, SqlCommand ) method.
            /// </summary>
            /// <param name="g">The IL genrator.</param>
            /// <param name="localSqlCommand">The SqlCommand local variable.</param>
            public void GenerateExecuteNonQueryCall( ILGenerator g, LocalBuilder localSqlCommand )
            {
                ParameterInfo callProvider = _callProviderParameter;
                g.LdArg( callProvider.Position + 1 );
                g.Emit( OpCodes.Ldarg_0 );
                g.Emit( OpCodes.Call, SqlObjectItem.MGetDatabase );
                g.Emit( OpCodes.Call, SqlObjectItem.MDatabaseGetConnectionString );
                g.LdLoc( localSqlCommand );
                g.Emit( OpCodes.Callvirt, _callProviderParameterMethod );
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
