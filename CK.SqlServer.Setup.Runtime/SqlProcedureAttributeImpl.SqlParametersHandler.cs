#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlProcedureAttributeImpl.SqlParametersHandler.cs) is part of CK-Database. 
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
using System.Threading.Tasks;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl
    {
        /// <summary>
        /// Manages sql parameters thanks to <see cref="SqlParamHandler"/> objects that are built on <see cref="SqlExprParameter"/> objects
        /// and can be associated to <see cref="ParameterInfo"/>.
        /// </summary>
        class SqlParametersHandler
        {
            readonly List<SqlParamHandler> _params;

            public class SqlParamHandler
            {
                readonly SqlParametersHandler _holder;

                public readonly SqlExprParameter SqlExprParam;
                // ParameterName without the '@' prefix char.
                public readonly string SqlParameterName;
                int _index;

                ParameterInfo _methodParam;
                SqlCallContextInfo.Property _ctxProp;
                bool _isIgnoredOutputParameter;
                bool _mapped;

                public SqlParamHandler( SqlParametersHandler holder, SqlExprParameter sP, int index )
                {
                    _holder = holder;
                    SqlExprParam = sP;
                    SqlParameterName = sP.Variable.Identifier.Name;
                    if( SqlParameterName[0] == '@' ) SqlParameterName = SqlParameterName.Substring( 1 );
                    _index = index;
                }

                /// <summary>
                /// Gets whether this Sql parameter has a corresponding method parameter or a property in one of the ISqlCallContext objects 
                /// or is an ignored output.
                /// </summary>
                public bool IsMapped
                {
                    get { return _mapped; }
                }

                public void SetParameterMapping( SqlCallContextInfo.Property prop )
                {
                    Debug.Assert( !IsMapped );
                    _mapped = true;
                    _ctxProp = prop;
                }

                public bool SetParameterMapping( ParameterInfo mP, IActivityMonitor monitor )
                {
                    Debug.Assert( !IsMapped );
                    _mapped = true;
                    _methodParam = mP;
                    if( !CheckParameter( mP, SqlExprParam, monitor ) ) return false;
                    return true;
                }

                static bool CheckParameter( ParameterInfo mP, SqlExprParameter p, IActivityMonitor monitor )
                {
                    int nbError = CheckParameterDirection( mP, p, monitor );
                    return nbError == 0 && CheckParameterType( mP.ParameterType, p, monitor );
                }

                static int CheckParameterDirection( ParameterInfo mP, SqlExprParameter p, IActivityMonitor monitor )
                {
                    int nbError = 0;
                    bool sqlIsInputOutput = p.IsInputOutput;
                    bool sqlIsOutput = sqlIsInputOutput || p.IsOutput;
                    bool sqlIsInput = sqlIsInputOutput || p.IsInput;
                    Debug.Assert( sqlIsInput || sqlIsOutput );
                    if( mP.ParameterType.IsByRef )
                    {
                        if( mP.IsOut )
                        {
                            if( sqlIsInputOutput )
                            {
                                monitor.Error().Send( "Sql parameter '{0}' is an /*input*/output parameter. The method '{1}' must use 'ref' for it (not 'out').", p.Variable.Identifier.Name, mP.Member.Name );
                                ++nbError;
                            }
                            else if( sqlIsInput )
                            {
                                Debug.Assert( !sqlIsOutput );
                                monitor.Error().Send( "Sql parameter '{0}' is an input parameter. The method '{1}' can not use 'out' for it (and 'ref' modifier will be useless).", p.Variable.Identifier.Name, mP.Member.Name );
                                ++nbError;
                            }
                        }
                        else
                        {
                            if( !sqlIsOutput )
                            {
                                monitor.Warn().Send( "Sql parameter '{0}' is not an output parameter. The method '{1}' uses 'ref' for it that is useless.", p.Variable.Identifier.Name, mP.Member.Name );
                            }
                        }
                    }
                    else
                    {
                        if( sqlIsInputOutput )
                        {
                            monitor.Warn().Send( "Sql parameter '{0}' is an /*input*/output parameter. The method '{1}' should use 'ref' to retrieve the new value after the call.", p.Variable.Identifier.Name, mP.Member.Name );
                        }
                        else if( sqlIsOutput )
                        {
                            monitor.Error().Send( "Sql parameter '{0}' is an output parameter. The method '{1}' must use 'out' for the parameter (you can also simply remove the method's parameter the output value can be ignored).", p.Variable.Identifier.Name, mP.Member.Name );
                            ++nbError;
                        }
                    }
                    return nbError;
                }

                public void SetMappingToIgnoredOutput()
                {
                    Debug.Assert( !IsMapped );
                    _mapped = true;
                    _isIgnoredOutputParameter = true;
                }

                public void RemoveParameterForOptionalDefaultValue( ILGenerator g, LocalBuilder locParams )
                {
                    Debug.Assert( !IsMapped );
                    Debug.Assert( SqlExprParam.DefaultValue != null );
                    Debug.Assert( SqlExprParam.IsInput, "Input our input/output." );

                    _mapped = true;
                    // Removing the optional parameter.
                    g.LdLoc( locParams );
                    g.LdInt32( _index );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionRemoveAtParameter );
                    // Adjusts indexes.
                    for( int i = _index + 1; i < _holder._params.Count; ++i )
                    {
                        var param = _holder._params[i];
                        // Only adjust index to parameters that have not been yet adjusted.
                        if( param._index > -1 ) --param._index;
                    }
                    _index = -1;
                }

                /// <summary>
                /// We must not emit anything if:
                /// - the Sql parameter has been removed,
                /// - OR the method does not declare the Sql Parameter that is a pure output parameter,
                /// - OR the method parameter is out.
                /// </summary>
                bool SkipEmitSetSqlParameterValue
                {
                    get { return _index == -1 || _isIgnoredOutputParameter || (_methodParam != null && _methodParam.IsOut); }
                }

                private Type LdParameterType( ILGenerator g )
                {
                    if( _methodParam != null )
                    {
                        g.LdArgBox( _methodParam );
                        return _methodParam.ParameterType;
                    }
                    else
                    {
                        Debug.Assert( _ctxProp != null );
                        g.LdArgBox( _ctxProp.Parameter );
                        g.Emit( OpCodes.Callvirt, _ctxProp.Prop.GetGetMethod() );
                        if( _ctxProp.Prop.PropertyType.IsGenericParameter || _ctxProp.Prop.PropertyType.IsValueType )
                        {
                            g.Emit( OpCodes.Box, _ctxProp.Prop.PropertyType );
                        }
                        return _ctxProp.Parameter.ParameterType;
                    }
                }

                internal void EmitSetSqlParameterValue( ILGenerator g, LocalBuilder locParameterCollection )
                {
                    if( SkipEmitSetSqlParameterValue ) return;
                    g.LdLoc( locParameterCollection );
                    g.LdInt32( _index );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                    
                    Label notNull = g.DefineLabel();

                    //Load ParameterType on the stack
                    Type t = LdParameterType( g );

                    if( t.IsByRef ) t = t.GetElementType();
                    if( t.IsGenericType && t.GetGenericTypeDefinition() == typeof( Nullable<> ) )
                    {
                        var trueLabel = g.DefineLabel();

                        g.Emit( OpCodes.Brtrue_S, trueLabel );

                        //false
                        g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                        g.Emit( OpCodes.Br, notNull );

                        g.MarkLabel( trueLabel );
                        LdParameterType( g );
                        g.Emit( OpCodes.Call, t.GetProperty( "Value", t.GetGenericArguments()[0] ).GetGetMethod() );
                        if( t.GetGenericArguments()[0].IsGenericParameter || t.GetGenericArguments()[0].IsValueType )
                        {
                            g.Emit( OpCodes.Box, t.GetGenericArguments()[0] );
                        }
                    }
                    else
                    {
                        g.Emit( OpCodes.Dup );
                        g.Emit( OpCodes.Brtrue_S, notNull );
                        g.Emit( OpCodes.Pop );
                        g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                    }
                    g.MarkLabel( notNull );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterSetValue );
                }

                bool SkipEmitSetRefOrRefParameter
                {
                    get { return _index == -1 || _isIgnoredOutputParameter || _methodParam == null || !_methodParam.ParameterType.IsByRef; }
                }

                internal void EmitSetRefOrOutParameter( ILGenerator g, LocalBuilder locParameterCollection )
                {
                    if( SkipEmitSetRefOrRefParameter ) return;

                    g.LdArg( _methodParam.Position + 1 );
                    g.LdLoc( locParameterCollection );
                    g.LdInt32( _index );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterGetValue );
                    if( _methodParam.ParameterType.IsValueType )
                    {
                        g.Emit( OpCodes.Unbox_Any, _methodParam.ParameterType );
                        g.Emit( OpCodes.Stobj, _methodParam.ParameterType );
                    }
                    else
                    {
                        g.Emit( OpCodes.Stind_Ref );
                    }
                }
            }

            public SqlParametersHandler( SqlExprParameterList parameters )
            {
                _params = parameters.Select( ( p, idx ) => new SqlParamHandler( this, p, idx ) ).ToList();
            }

            public IReadOnlyList<SqlParamHandler> Handlers
            {
                get { return _params; }
            }

            public int IndexOf( int iStart, ParameterInfo mP )
            {
                while( iStart < _params.Count )
                {
                    if( StringComparer.OrdinalIgnoreCase.Equals( _params[iStart].SqlParameterName, mP.Name ) ) return iStart;
                    ++iStart;
                }
                return -1;
            } 

            internal void EmitReturn( ILGenerator g, LocalBuilder locParameterCollection, Type returnType )
            {
                for( int i = _params.Count - 1; i >= 0; --i )
                {
                    var p = _params[i];
                    if( p.SqlExprParam.IsOutput && p.SqlExprParam.Variable.TypeDecl.ActualType.IsTypeCompatible( returnType ) )
                    {
                        g.LdLoc( locParameterCollection );
                        g.LdInt32( i );
                        g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                        g.Emit( OpCodes.Call, SqlObjectItem.MParameterGetValue );
                        if( returnType.IsValueType )
                        {
                            g.Emit( OpCodes.Unbox_Any, returnType );
                        }
                    }
                }

            }
        }

    }
}
