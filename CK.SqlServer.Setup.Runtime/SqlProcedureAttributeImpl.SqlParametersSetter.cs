#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlProcedureAttributeImpl.SqlParametersSetter.cs) is part of CK-Database. 
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
        class SqlParametersSetter
        {
            List<Setter> _params;

            public class Setter
            {
                readonly SqlParametersSetter _holder;
                public readonly SqlExprParameter SqlExprParam;
                // ParameterName without the '@' prefix char.
                public readonly string SqlParameterName;

                int _index;
                ParameterInfo _methodParam;
                SqlCallContextInfo.Property _ctxProp;
                bool _isIgnoredOutputParameter;
                bool _mapped;

                public Setter( SqlParametersSetter holder, SqlExprParameter sP, int index )
                {
                    _holder = holder;
                    SqlExprParam = sP;
                    SqlParameterName = sP.Variable.Identifier.Name;
                    if( SqlParameterName[0] == '@' ) SqlParameterName = SqlParameterName.Substring( 1 );
                    _index = index;
                }

                public bool IsMapped
                {
                    get { return _mapped; }
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

                public void SetParameterMapping( SqlCallContextInfo.Property prop )
                {
                    Debug.Assert( !IsMapped );
                    _mapped = true;
                    _ctxProp = prop;
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
                bool SkipEmitSetValue
                {
                    get { return _index == -1 || _isIgnoredOutputParameter || (_methodParam != null && _methodParam.IsOut); }
                }

                internal void EmitSetParameter( ILGenerator g, LocalBuilder locParams )
                {
                    if( SkipEmitSetValue ) return;
                    g.LdLoc( locParams );
                    g.LdInt32( _index );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                    Label notNull = g.DefineLabel();
                    if( _methodParam != null ) g.LdArgBox( _methodParam );
                    else
                    {
                        Debug.Assert( _ctxProp != null );
                        g.LdArgBox( _ctxProp.Parameter );
                        g.Emit( OpCodes.Callvirt, _ctxProp.Prop.GetGetMethod() );
                        if( _ctxProp.Prop.PropertyType.IsGenericParameter || _ctxProp.Prop.PropertyType.IsValueType )
                        {
                            g.Emit( OpCodes.Box, _ctxProp.Prop.PropertyType );
                        }
                    }
                    g.Emit( OpCodes.Dup );
                    g.Emit( OpCodes.Brtrue_S, notNull );
                    g.Emit( OpCodes.Pop );
                    g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                    g.MarkLabel( notNull );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterSetValue );
                }
            }

            public SqlParametersSetter( SqlExprParameterList parameters )
            {
                _params = parameters.Select( ( p, idx ) => new Setter( this, p, idx ) ).ToList();
            }

            public IReadOnlyList<Setter> Setters
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
        }

    }
}
