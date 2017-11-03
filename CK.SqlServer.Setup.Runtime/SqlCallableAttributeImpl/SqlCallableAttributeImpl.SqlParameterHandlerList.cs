#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlProcedureAttributeImpl.SqlParameterHandlerList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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
        /// Manages sql parameters thanks to <see cref="SqlParamHandler"/> objects that are built on SqlExprParameter objects
        /// and can be associated to <see cref="ParameterInfo"/>.
        /// </summary>
        class SqlParameterHandlerList
        {
            readonly List<SqlParamHandler> _params;
            SqlParamHandler _simpleReturnType;
            ISqlServerParameter _funcReturnParameter;
            bool _isAsyncCall;
            Type _unwrappedReturnedType;
            readonly StringBuilder _funcResultBuilderSignature;
            ComplexTypeMapperModel _complexReturnType;

            public class SqlParamHandler
            {
                readonly SqlParameterHandlerList _holder;

                public readonly ISqlServerParameter SqlExprParam;

                /// <summary>
                /// ParameterName without the '@' prefix char.
                /// </summary>
                public readonly string SqlParameterName;
                readonly int _index;

                ParameterInfo _methodParam;
                SqlCallContextInfo.Property _ctxProp;
                TypeInfo _actualParameterType;
                bool _isIgnoredOutputParameter;
                bool _isUseDefaultSqlValue;
                bool _isUsedByReturnedType;

                public SqlParamHandler( SqlParameterHandlerList holder, ISqlServerParameter sP, int index )
                {
                    _holder = holder;
                    SqlExprParam = sP;
                    SqlParameterName = sP.Name;
                    if( SqlParameterName[0] == '@' ) SqlParameterName = SqlParameterName.Substring( 1 );
                    _index = index;
                }

                /// <summary>
                /// Gets whether this Sql parameter has a corresponding method parameter or a property in one of the ParameterSource objects.
                /// </summary>
                public bool IsMappedToMethodParameterOrParameterSourceProperty
                {
                    get { return _methodParam != null || _ctxProp != null; }
                }

                public bool IsUsedByReturnType => _isUsedByReturnedType;

                public int Index => _index; 

                /// <summary>
                /// 1 - This is done first, right after the SqlParameterHandlerList has been created when in Calling mode.
                /// </summary>
                public void SetUsedByReturnedType()
                {
                    _isUsedByReturnedType = true;
                }

                /// <summary>
                /// 2 - Second phase is to map the SqlParamHandler to a method parameter.
                /// </summary>
                public bool SetParameterMapping( ParameterInfo mP, IActivityMonitor monitor )
                {
                    Debug.Assert( !IsMappedToMethodParameterOrParameterSourceProperty );
                    _methodParam = mP;
                    _actualParameterType = mP.ParameterType.GetTypeInfo();
                    if( _actualParameterType.IsByRef ) _actualParameterType = _actualParameterType.GetElementType().GetTypeInfo();
                    if( !CheckParameter( mP, SqlExprParam, monitor ) ) return false;
                    return true;
                }

                bool CheckParameter( ParameterInfo mP, ISqlServerParameter p, IActivityMonitor monitor )
                {
                    int nbError = CheckParameterDirection( mP, p, monitor );
                    return nbError == 0 && CheckParameterType( mP.ParameterType, p, monitor );
                }

                int CheckParameterDirection( ParameterInfo mP, ISqlServerParameter p, IActivityMonitor monitor )
                {
                    int nbError = 0;
                    bool sqlIsInputOutput = p.IsInputOutput;
                    bool sqlIsOutput = p.IsOutput;
                    bool sqlIsInput = p.IsInput;
                    bool sqlIsPureOutput = p.IsPureOutput;
                    bool isComplexReturnedType = _holder.ComplexReturnType != null;
                    bool sqlParameterHasDefaultValue = p.DefaultValue != null;
                    Debug.Assert( sqlIsInput || sqlIsOutput );
                    // Analysing Method vs. Procedure parameters.
                    if( mP.ParameterType.IsByRef )
                    {
                        #region ref or out Method parameter
                        if( _holder.IsAsyncCall )
                        {
                            monitor.Error( $"The method '{mP.Member.Name}' implements an async call, There can not be 'ref' or 'out' parameters (like parameter '{mP.Name}')." );
                            ++nbError;
                        }
                        else
                        {
                            if( isComplexReturnedType )
                            {
                                monitor.Warn( $"Sql parameter '{p.Name}' is not an output parameter. The method '{mP.Member.Name}' uses 'ref' for it. That is useless." );
                            }
                            if( mP.IsOut )
                            {
                                #region out Method parameter
                                if( sqlIsInputOutput )
                                {
                                    if( _isUsedByReturnedType )
                                    {
                                        monitor.Warn( $"Sql parameter '{p.Name}' is an /*input*/output parameter. The method '{mP.Member.Name}' should use 'ref' for it (not 'out') or no ref nor out since this parameter is used by returned call." );
                                    }
                                    else
                                    {
                                        monitor.Error( $"Sql parameter '{p.Name}' is an /*input*/output parameter. The method '{mP.Member.Name}' must use 'ref' for it (not 'out')." );
                                        ++nbError;
                                    }
                                }
                                else if( sqlIsInput )
                                {
                                    Debug.Assert( !sqlIsOutput );
                                    monitor.Error( $"Sql parameter '{p.Name}' is an input parameter. The method '{mP.Member.Name}' can not use 'out' for it (and 'ref' modifier will be useless)." );
                                    ++nbError;
                                }
                                #endregion
                            }
                            else
                            {
                                // ref Method parameter.
                                if( !sqlIsOutput )
                                {
                                    monitor.Warn( $"Sql parameter '{p.Name}' is not an output parameter. The method '{mP.Member.Name}' uses 'ref' for it. That is useless." );
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region By value Method parameter.
                        if( sqlIsPureOutput )
                        {
                            // By value method parameter with a pure output Sql parameter: it should be /*input*/output in sql.
                            monitor.Warn( $"Sql parameter '{p.Name}' is an output parameter. Setting its value is useless. Should it be marked /*input*/output?." );
                        }
                        // Whenever the sql parameter is output but it is not ref nor out, we warn the user: it is an ignored return from the database.
                        // When using specialization, this should NOT occur if we succeed to reroute the call to the most specialized, covariant, method.
                        //
                        // For complex returned type (1), this is easier than for output parameters (2):
                        //   1 - Complex type: one need to find a specialized definition that return a specialized Complex Type. Then this most specialized one SHOULD
                        //       handle all output parameters: the warnings below make sense.
                        //   2 - Output parameters: one need to generate local fake variables to be able to call
                        //       the specialized method and, if the method parameter has no default value, use the sql default value transformed to its .Net type.
                        //
                        if( sqlIsInputOutput && !_isUsedByReturnedType )
                        {
                            if( isComplexReturnedType )
                            {
                                monitor.Warn( $"Sql parameter '{p.Name}' is an /*input*/output parameter and this parameter is not returned in '{_holder.ComplexReturnType.CreatedType.Name} {mP.Member.Name}' method." );
                            }
                            else
                            {
                                monitor.Warn( $"Sql parameter '{p.Name}' is an /*input*/output parameter and this parameter is not returned. The method '{mP.Member.Name}' may use 'ref' to retrieve the new value after the call." );
                            }
                        }
                        #endregion
                    }
                    return nbError;
                }
                
                /// <summary>
                /// 3 - If this Sql parameter is not mapped to a method parameter, then it may be mapped by a property of one 
                /// of the SqlCallContext objects.
                /// </summary>
                public void SetParameterMapping( SqlCallContextInfo.Property prop )
                {
                    Debug.Assert( !IsMappedToMethodParameterOrParameterSourceProperty );
                    _ctxProp = prop;
                    _actualParameterType = _ctxProp.Prop.PropertyType.GetTypeInfo();
                }

                /// <summary>
                /// 4.1 - If no previous mapping has been found and the parameter is purely output and has no Sql default value.
                /// </summary>
                public void SetMappingToIgnoredOutput()
                {
                    Debug.Assert( !IsMappedToMethodParameterOrParameterSourceProperty );
                    _isIgnoredOutputParameter = true;
                }

                /// <summary>
                /// 4.2 - If no previous mapping has been found and the parameter has a Sql default value.
                /// </summary>
                public void SetMappingToSqlDefaultValue()
                {
                    Debug.Assert( !IsMappedToMethodParameterOrParameterSourceProperty );
                    _isUseDefaultSqlValue = true;
                }

                public bool MappingDone
                {
                    get { return _methodParam != null || _ctxProp != null || _isIgnoredOutputParameter || _isUseDefaultSqlValue; }
                }

                #region Setting SqlParameter.Value

                bool LdObjectToSetFromParameterOrCallContextProperty( IActivityMonitor monitor, ILGenerator g )
                {
                    Type typeToSet;
                    TypeInfo typeToSetInfo;
                    if( _methodParam != null )
                    {
                        typeToSet = _methodParam.ParameterType;
                        g.LdArg( _methodParam.Position + 1 );
                        if( typeToSet.IsByRef )
                        {
                            typeToSet = typeToSet.GetElementType();
                            typeToSetInfo = typeToSet.GetTypeInfo();
                            if( typeToSetInfo.IsValueType )
                            {
                                g.Emit( OpCodes.Ldobj, typeToSet );
                            }
                            else
                            {
                                g.Emit( OpCodes.Ldind_Ref );
                            }
                        }
                        else typeToSetInfo = typeToSet.GetTypeInfo();
                    }
                    else
                    {
                        Debug.Assert( _ctxProp != null );
                        g.LdArg( _ctxProp.Parameter.Position + 1 );
                        if( _ctxProp.PocoMappedType != null )
                        {
                            g.Emit( OpCodes.Castclass, _ctxProp.PocoMappedType );
                        }
                        g.Emit( OpCodes.Callvirt, _ctxProp.Prop.GetGetMethod() );
                        typeToSet = _ctxProp.Prop.PropertyType;
                        typeToSetInfo = typeToSet.GetTypeInfo();
                    }
                    // The value is on the stack: it may be a value or reference type of type typeToSet.
                    // If it is null or a Nullable<T> that has no value, we must transform it into DBNull.Value.
                    Label objectIsAvailable = g.DefineLabel();
                    if( typeToSetInfo.IsValueType )
                    {
                        // Boxing a Nullable<T> is handled at the CLR level: if Nullable<T>.HasValue is false,
                        // a null reference is left on the stack.
                        g.Emit( OpCodes.Box, typeToSet );
                    }
                    g.Emit( OpCodes.Dup );
                    g.Emit( OpCodes.Brtrue_S, objectIsAvailable );
                    g.Emit( OpCodes.Pop );
                    g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                    g.MarkLabel( objectIsAvailable );
                    return true;
                }

                bool LdObjectToSetFromDefaultSqlValue( IActivityMonitor monitor, ILGenerator g )
                {
                    if( SqlExprParam.DefaultValue.IsVariable )
                    {
                        monitor.Error( $"Parameter '{SqlExprParam.ToStringClean()}' has default value '{SqlExprParam.DefaultValue.ToString()}' that is a variable. This not supported." );
                        return false;
                    }
                    object o = SqlExprParam.DefaultValue.NullOrLitteralDotNetValue;
                    if( o == DBNull.Value )
                    {
                        g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                    }
                    else if( o is Int32 )
                    {
                        g.LdInt32( (int)o );
                        g.Emit( OpCodes.Box, typeof(Int32) );
                    }
                    else if( o is Decimal )
                    {
                        Decimal d = (Decimal)o;
                        int[] bits = Decimal.GetBits( d );
                        g.LdInt32( 4 );
                        g.Emit( OpCodes.Newarr, typeof( Int32 ) );
                        for( int i = 0; i < 4; ++i  )
                        {
                            g.Emit( OpCodes.Dup );
                            g.LdInt32( i );
                            g.LdInt32( bits[i] );
                            g.Emit( OpCodes.Stelem_I4 );
                        }
                        g.Emit( OpCodes.Newobj, SqlObjectItem.CtorDecimalBits );
                        g.Emit( OpCodes.Box, typeof(Decimal) );
                    }
                    else if( o is Double )
                    {
                        g.Emit( OpCodes.Ldc_R8, (double)o );
                        g.Emit( OpCodes.Box, typeof( Double ) );
                    }
                    else if (o is string)
                    {
                        g.Emit(OpCodes.Ldstr, (string)o);
                    }
                    else if (o is byte[])
                    {
                        var t = (byte[])o;
                        g.LdInt32(t.Length);
                        g.Emit(OpCodes.Newarr, typeof(byte));
                        for (int i = 0; i < t.Length; ++i)
                        {
                            g.Emit(OpCodes.Dup);
                            g.LdInt32(i);
                            g.LdInt32(t[i]);
                            g.Emit(OpCodes.Stelem_I1);
                        }
                    }
                    else
                    {
                        monitor.Error( $"Emit for type  '{o.GetType().Name}' (Parameter '{SqlExprParam.ToStringClean()}') is not supported." );
                        return false;
                    }
                    return true;
                }

                internal bool EmitSetSqlParameterValue( IActivityMonitor monitor, ILGenerator g, LocalBuilder locParameterCollection )
                {
                    if( _isIgnoredOutputParameter ) return true;
                    if( _isUseDefaultSqlValue )
                    {
                        monitor.Info( $"Parameter '{SqlExprParam.ToStringClean()}' will use its default value." );
                        return EmitSetParameterCode( monitor, g, locParameterCollection, LdObjectToSetFromDefaultSqlValue );
                    }
                    Debug.Assert( IsMappedToMethodParameterOrParameterSourceProperty );
                    // Do not set any Value if the C# parameter is out.
                    if( _methodParam != null && _methodParam.IsOut ) return true;
                    return EmitSetParameterCode( monitor, g, locParameterCollection, LdObjectToSetFromParameterOrCallContextProperty );
                }

                bool EmitSetParameterCode( IActivityMonitor monitor, ILGenerator g, LocalBuilder locParameterCollection, Func<IActivityMonitor,ILGenerator,bool> valueLoader )
                {
#if NET461
                    g.LdLoc( locParameterCollection );
                    g.LdInt32( _index );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                    // The SqlCommandParameter is on the stack.
                    // We must correct its default configuration if the Sql parameter is output in order for Sql Server to take into account the input value.
                    if( SqlExprParam.IsPureOutput )
                    {
                        g.Emit( OpCodes.Dup );
                        g.LdInt32( (int)ParameterDirection.InputOutput );
                        g.Emit( OpCodes.Callvirt, SqlObjectItem.MParameterSetDirection );
                    }
                    string sqlType = SqlExprParam.SqlType.ToStringClean();
                    if (StringComparer.OrdinalIgnoreCase.Equals(sqlType, "Geometry")
                        || StringComparer.OrdinalIgnoreCase.Equals(sqlType, "Geography")
                        || StringComparer.OrdinalIgnoreCase.Equals(sqlType, "HierarchyId"))
                    {
                        g.Emit(OpCodes.Dup);
                        g.LdInt32((int)SqlDbType.Udt);
                        g.Emit(OpCodes.Callvirt, SqlObjectItem.MParameterSetSqlDbType);

                        g.Emit(OpCodes.Dup);
                        g.Emit(OpCodes.Ldstr, sqlType.ToLowerInvariant());
                        g.Emit(OpCodes.Callvirt, SqlObjectItem.MParameterSetUdtTypeName);
                    }

                    // Load object on the stack.
                    if ( !valueLoader( monitor, g ) ) return false;
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterSetValue );
#endif
                    return true;
                }

                internal bool EmitSetSqlParameterValue( IActivityMonitor monitor, ICodeWriter b, string varParameterName )
                {
                    if( _isIgnoredOutputParameter ) return true;
                    string sqlType = SqlExprParam.SqlType.ToStringClean();
                    if( StringComparer.OrdinalIgnoreCase.Equals( sqlType, "Geometry" )
                        || StringComparer.OrdinalIgnoreCase.Equals( sqlType, "Geography" )
                        || StringComparer.OrdinalIgnoreCase.Equals( sqlType, "HierarchyId" ) )
                    {
                        b.Append( varParameterName ).Append( ".SqlDbType = SqlDbType.Udt;" ).NewLine();
                        b.Append( varParameterName )
                          .Append( ".UdtTypeName = " ).AppendSourceString( sqlType.ToLowerInvariant() ).Append( ";" )
                          .NewLine();
                    }
                    // Do not set any Value if the C# parameter is out.
                    if( !_isUseDefaultSqlValue && _methodParam != null && _methodParam.IsOut ) return true;

                    // We must correct its default configuration if the Sql parameter is output in order 
                    // for Sql Server to take into account the input value.
                    if( SqlExprParam.IsPureOutput )
                    {
                        b.Append( varParameterName ).Append( ".Direction = ParameterDirection.InputOutput;" ).NewLine();
                    }
                    b.Append( varParameterName ).Append( ".Value = " );
                    if( _isUseDefaultSqlValue )
                    {
                        if( SqlExprParam.DefaultValue.IsVariable )
                        {
                            monitor.Error( $"Parameter '{SqlExprParam.ToStringClean()}' has default value '{SqlExprParam.DefaultValue}' that is a variable. This not supported." );
                            return false;
                        }
                        monitor.Info( $"Parameter '{SqlExprParam.ToStringClean()}' will use its default sql value '{SqlExprParam.DefaultValue}'." );
                        object o = SqlExprParam.DefaultValue.NullOrLitteralDotNetValue;
                        if( o == DBNull.Value ) b.Append( "DBNull.Value" );
                        else
                        {
                            try
                            {
                                b.Append( o );
                            }
                            catch( Exception ex )
                            {
                                monitor.Error( $"Emit for type  '{SqlExprParam.ToStringClean()}' (Parameter '{o.GetType().Name}') is not supported.", ex );
                                return false;
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert( IsMappedToMethodParameterOrParameterSourceProperty );
                        Debug.Assert( (_methodParam == null) != (_ctxProp == null) );
                        Debug.Assert( _actualParameterType != null );
                        bool isNullable = true;
                        if( _actualParameterType.IsValueType )
                        {
                            isNullable = _actualParameterType.IsGenericType && _actualParameterType.GetGenericTypeDefinition() == typeof( Nullable<> );
                        }
                        if( isNullable ) b.Append( "(object)" );
                        if( _methodParam != null )
                        {
                            b.Append( _methodParam.Name );
                        }
                        else
                        {
                            if( _ctxProp.PocoMappedType != null )
                            {
                                b.Append( "((" )
                                    .AppendCSharpName( _ctxProp.PocoMappedType )
                                    .Append( ")" )
                                    .Append( _ctxProp.Parameter.Name )
                                    .Append( ")." )
                                    .Append( _ctxProp.Prop.Name );
                            }
                            else b.Append( _ctxProp.Parameter.Name ).Append( "." ).Append( _ctxProp.Prop.Name );
                        }
                        if( isNullable ) b.Append( " ?? DBNull.Value" );
                    }
                    b.Append( ";" ).NewLine();
                    return true;
                }

#endregion

                internal void EmitSetRefOrOutParameter( ILGenerator g, LocalBuilder locParameterCollection )
                {
                    if( _methodParam == null || !_methodParam.ParameterType.IsByRef ) return;

                    g.LdArg( _methodParam.Position + 1 );
                    g.LdLoc( locParameterCollection );
                    g.LdInt32( _index );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterGetValue );
                    Type t = _methodParam.ParameterType.GetElementType();
                    if( t.GetTypeInfo().IsValueType )
                    {
                        g.Emit( OpCodes.Unbox_Any, t );
                        g.Emit( OpCodes.Stobj, t );
                    }
                    else
                    {
                        g.Emit( OpCodes.Castclass, t );
                        g.Emit( OpCodes.Stind_Ref );
                    }
                }
                internal void EmitSetRefOrOutParameter( ICodeWriter b, string varCmdParameters, Func<string> tempObjName )
                {
                    if( _methodParam == null || !_methodParam.ParameterType.IsByRef ) return;

                    string resultName = EmitGetSqlCommandParameterValue( b, varCmdParameters, tempObjName, _index, _actualParameterType.AsType() );
                    b.Append( _methodParam.Name ).Append( "=" ).Append( resultName ).Append( ";" ).NewLine();
                }
            }

            public SqlParameterHandlerList( ISqlServerCallableObject sqlObject )
            {
                _params = new List<SqlParamHandler>();
                int idx = 0;
                ISqlServerFunctionScalar func = sqlObject as ISqlServerFunctionScalar;
                if( func != null )
                {
                    _funcReturnParameter = new SqlParameterReturnedValue( func.ReturnType );
                    _params.Add( new SqlParamHandler( this, _funcReturnParameter, idx++ ) );
                }
                foreach( var p in sqlObject.Parameters )
                {
                    _params.Add( new SqlParamHandler( this, p, idx++ ) );
                }
                _funcResultBuilderSignature = new StringBuilder();
            }

            public IReadOnlyList<SqlParamHandler> Handlers => _params; 

            public int IndexOf( int iStart, ParameterInfo mP )
            {
                while( iStart < _params.Count )
                {
                    if( StringComparer.OrdinalIgnoreCase.Equals( _params[iStart].SqlParameterName, mP.Name ) ) return iStart;
                    ++iStart;
                }
                return -1;
            }

            public bool IsAsyncCall => _isAsyncCall; 

            public ComplexTypeMapperModel ComplexReturnType => _complexReturnType; 

            internal bool HandleNonVoidCallingReturnedType( IActivityMonitor monitor, Type returnType )
            {
                if( returnType == typeof( Task ) ) return _isAsyncCall = true;
                bool isSimpleType = IsSimpleReturnType( returnType );
                if( !isSimpleType && returnType.GetTypeInfo().IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>) )
                {
                    _isAsyncCall = true;
                    returnType = returnType.GetGenericArguments()[0];
                    isSimpleType = IsSimpleReturnType( returnType );
                }
                // If it is a scalar function
                if( _funcReturnParameter != null )
                {
                    if( isSimpleType )
                    {
                        if( _funcReturnParameter.SqlType.IsTypeCompatible( returnType ) )
                        {
                            SetSimpleReturnedTypeParameter( returnType, _params[0] );
                            return true;
                        }
                    }
                    else
                    {
                        monitor.Error( $"Type mismatch for function returned type '{returnType.Name}' / '{_funcReturnParameter.SqlType.ToStringClean()}'." );
                        return false;
                    }
                }
                if( isSimpleType )
                {
                    // Look for the first last (reverse order) parameter for which the returned type is compatible.
                    for( int i = _params.Count - 1; i >= 0; --i )
                    {
                        var p = _params[i];
                        if( p.SqlExprParam.IsOutput && p.SqlExprParam.SqlType.IsTypeCompatible( returnType ) )
                        {
                            SetSimpleReturnedTypeParameter( returnType, p );
                            return true;
                        }
                    }
                }
                else
                {
                    if( returnType.GetTypeInfo().IsInterface )
                    {
                        monitor.Error( $"Return type '{returnType.Name}' is an interface. This is not yet supported." );
                        return false;
                    }
                    _unwrappedReturnedType = returnType;
                    _complexReturnType = new ComplexTypeMapperModel( returnType );
                    _funcResultBuilderSignature.Append( _unwrappedReturnedType.FullName );
                    foreach( var p in _params )
                    {
                        if( _complexReturnType.AddInput( p.Index, p.SqlParameterName, p.SqlExprParam.SqlType.IsTypeCompatible, p.SqlExprParam.SqlType.ToStringClean(), p.SqlExprParam.IsOutput ) )
                        {
                            _funcResultBuilderSignature.Append( '-' ).Append( p.Index );
                            p.SetUsedByReturnedType();
                        }
                    }
                    if( _complexReturnType.CheckValidity( monitor ) )
                    {
                        return true;
                    }
                }
                monitor.Error( $"Unable to find a way to return the required return type '{returnType.ToCSharpName()}'." );
                return false;
            }

            private void SetSimpleReturnedTypeParameter( Type returnType, SqlParamHandler p )
            {
                _simpleReturnType = p;
                _unwrappedReturnedType = returnType;
                _funcResultBuilderSignature.Append( _unwrappedReturnedType.FullName ).Append( '-' ).Append( p.Index );
                p.SetUsedByReturnedType();
            }

            internal void EmitInlineReturn(ILGenerator g, LocalBuilder locParameterCollection)
            {
                if (_simpleReturnType != null)
                {
                    EmitGetSqlCommandParameterValue(g, locParameterCollection, _simpleReturnType.Index, _unwrappedReturnedType);
                }
                else
                {
                    Debug.Assert(_complexReturnType != null);
                    _complexReturnType.EmitFullInitialization(g, (idxValue, targetType) =>
                    {
                        EmitGetSqlCommandParameterValue(g, locParameterCollection, idxValue, targetType);
                    });
                }
            }

            internal string EmitInlineReturn(ICodeWriter b, string nameParameters, Func<string> tempObjectName )
            {
                if (_simpleReturnType != null)
                {
                    return EmitGetSqlCommandParameterValue(b, nameParameters, tempObjectName, _simpleReturnType.Index, _unwrappedReturnedType);
                }
                Debug.Assert(_complexReturnType != null);
                return _complexReturnType.EmitFullInitialization(b, (idxValue, targetType) =>
                {
                    return EmitGetSqlCommandParameterValue(b, nameParameters, tempObjectName, idxValue, targetType);
                });
            }

            static string EmitGetSqlCommandParameterValue(ICodeWriter b, string varCmdParameters, Func<string> tempObjName, int sqlParameterIndex, Type targetType)
            {
                Debug.Assert( !targetType.IsByRef );
                string resultName = "getR" + sqlParameterIndex;
                b.Append( tempObjName() )
                    .Append( " = " )
                    .Append( varCmdParameters ).Append( "[" ).Append( sqlParameterIndex ).Append( "].Value;")
                    .NewLine();
                bool isNullable = true;
                Type enumUnderlyingType = null;
                if( targetType.GetTypeInfo().IsValueType )
                {
                    isNullable = false;
                    Type actualType = Nullable.GetUnderlyingType( targetType );
                    if( actualType != null )
                    {
                        isNullable = true;
                        if( actualType.GetTypeInfo().IsEnum ) enumUnderlyingType = Enum.GetUnderlyingType( actualType );
                    }
                    else
                    {
                        //Debugger.Break();
                        //if( targetType.GetTypeInfo().IsEnum ) enumUnderlyingType = Enum.GetUnderlyingType( targetType );
                    }
                }
                b.Append( "var " ).Append( resultName ).Append( '=' );
                if( isNullable )
                {
                    b.Append( "DBNull.Value == " )
                        .Append( tempObjName() )
                        .Append( "? default(" ).AppendCSharpName( targetType ).Append( ')' )
                        .Append( ": (" ).AppendCSharpName( targetType ).Append( ')' );
                    if( enumUnderlyingType != null )
                    {
                        b.Append( '(' ).AppendCSharpName( enumUnderlyingType ).Append( ')' );
                    }
                }
                else
                {
                    b.Append( '(' )
                        .AppendCSharpName( targetType )
                        .Append( ')' );
                }
                b.Append( tempObjName() ).Append( ";" ).NewLine();
                return resultName;
            }

            static void EmitGetSqlCommandParameterValue( ILGenerator g, LocalBuilder locParameterCollection, int sqlParameterIndex, Type targetType )
            {
                g.LdLoc( locParameterCollection );
                g.LdInt32( sqlParameterIndex );
                g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                g.Emit( OpCodes.Call, SqlObjectItem.MParameterGetValue );
                if( targetType.GetTypeInfo().IsValueType )
                {
                    Type actualType = Nullable.GetUnderlyingType( targetType );
                    if( actualType != null )
                    {
                        Label isNull = g.DefineLabel();
                        Label afterIsNotNull = g.DefineLabel();
                        LocalBuilder defNullable = g.DeclareLocal( targetType );
                        g.Emit( OpCodes.Dup );
                        g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                        g.Emit( OpCodes.Ceq );
                        g.Emit( OpCodes.Brtrue_S, isNull );
                        if( actualType.GetTypeInfo().IsEnum )
                        {
                            g.Emit( OpCodes.Unbox_Any, actualType );
                            g.Emit( OpCodes.Newobj, targetType.GetConstructor( new[] { actualType } ) );
                        }
                        else
                        {
                            // For non enum value type, the Unbox_Any handles the implicit 
                            // conversion to Nullable<actualType>: newobj is useless.
                            g.Emit( OpCodes.Unbox_Any, targetType );
                        }
                        g.Emit( OpCodes.Br_S, afterIsNotNull );
                        g.MarkLabel( isNull );
                        g.Emit( OpCodes.Pop );
                        g.Emit( OpCodes.Ldloc, defNullable );
                        g.MarkLabel( afterIsNotNull );
                    }
                    else
                    {
                        g.Emit( OpCodes.Unbox_Any, targetType );
                    }
                }
                else
                {
                    Label afterCheckDBNull = g.DefineLabel();
                    g.Emit( OpCodes.Dup );
                    g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                    g.Emit( OpCodes.Ceq );
                    g.Emit( OpCodes.Brfalse_S, afterCheckDBNull );
                    g.Emit( OpCodes.Pop );
                    g.Emit( OpCodes.Ldnull );
                    g.MarkLabel( afterCheckDBNull );
                    g.Emit( OpCodes.Castclass, targetType );
                }
            }

            static bool IsSimpleReturnType( Type returnType )
            {
                return IsNetTypeMapped( returnType );
            }

            #region Result builders functions (AssumeResultBuilder)

            /// <summary>
            /// Equals to: "CK.&lt;FuncResultBuilder&gt;".
            /// </summary>
            const string _funcHolderTypeName = "CK.<FuncResultBuilder>";

            class FuncTypeHolder
            {
                public readonly ITypeScope ClassBuilder;
                public readonly System.Reflection.Emit.TypeBuilder TypeBuilder;
                public FuncImpl FirstFuncImpl;
                public FuncTypeHolder(System.Reflection.Emit.TypeBuilder b, ITypeScope classBuilder )
                {
                    TypeBuilder = b;
                    ClassBuilder = classBuilder;
                }
            }

            class FuncImpl
            {
                public readonly FieldInfo Field;
                public readonly MethodInfo Func;
                public readonly FuncImpl Next;
                public FuncImpl( FieldInfo field, MethodInfo func, FuncImpl next ) { Field = field; Func = func; Next = next; }
            }

            FuncTypeHolder AssumeFuncTypeHolder(IDynamicAssembly dynamicAssembly)
            {
                FuncTypeHolder fB = (FuncTypeHolder)dynamicAssembly.Memory[_funcHolderTypeName];
                if( fB == null )
                {
                    System.Reflection.Emit.TypeBuilder tB = dynamicAssembly.ModuleBuilder.DefineType(_funcHolderTypeName, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.NotPublic);
                    ITypeScope cB = dynamicAssembly.DefaultGenerationNamespace.CreateType("static class _build_func_");
                    dynamicAssembly.Memory.Add(_funcHolderTypeName, (fB = new FuncTypeHolder(tB, cB)));
                    dynamicAssembly.PushFinalAction(FinalizeFuncHolderType);
                }
                return fB;
            }

            internal FieldInfo AssumeResultBuilder( IDynamicAssembly dynamicAssembly )
            {
                string funcKey = _funcHolderTypeName + ':' + _funcResultBuilderSignature.ToString();
                FuncImpl f = (FuncImpl)dynamicAssembly.Memory[funcKey];
                if( f == null )
                {
                    FuncTypeHolder fB = AssumeFuncTypeHolder(dynamicAssembly);
                    string funcName = 'f' + dynamicAssembly.NextUniqueNumber();
                    string fieldName = "_" + funcName;
                    Type tFunc = typeof(Func<,>).MakeGenericType(SqlObjectItem.TypeCommand, _unwrappedReturnedType);
                    var field = fB.TypeBuilder.DefineField(fieldName, tFunc, FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly);
                    var func = fB.TypeBuilder.DefineMethod(funcName, MethodAttributes.Private | MethodAttributes.Static, _unwrappedReturnedType, new Type[] { SqlObjectItem.TypeCommand });
                    ILGenerator g = func.GetILGenerator();
                    LocalBuilder locParams = g.DeclareLocal(SqlObjectItem.TypeParameterCollection);
                    g.LdArg(0);
                    g.Emit(OpCodes.Callvirt, SqlObjectItem.MCommandGetParameters);
                    g.StLoc(locParams);
                    EmitInlineReturn(g, locParams);
                    g.Emit(OpCodes.Ret);
                    dynamicAssembly.Memory[funcKey] = (fB.FirstFuncImpl = f = new FuncImpl(field, func, fB.FirstFuncImpl));
                }
                return f.Field;
            }

            internal string AssumeSourceFuncResultBuilder( IDynamicAssembly dynamicAssembly )
            {
                string funcKey = "S:" + _funcHolderTypeName + ':' + _funcResultBuilderSignature.ToString();
                string fieldFullName = (string)dynamicAssembly.Memory[funcKey];
                if( fieldFullName == null )
                {
                    FuncTypeHolder fB = AssumeFuncTypeHolder( dynamicAssembly );
                    string funcName = 'f' + dynamicAssembly.NextUniqueNumber();
                    string fieldName = "_" + funcName;
                    string tFuncReturnType = _unwrappedReturnedType.ToCSharpName();
                    string tFunc = $"Func<{SqlObjectItem.TypeCommand.FullName},{tFuncReturnType}>";
                    //var fieldDef = fB.ClassBuilder.DefineField(tFunc, fieldName);
                    //fieldDef.FrontModifiers.Add("internal static readonly");
                    //fieldDef.InitialValue = funcName;
                    fB.ClassBuilder.Append( "internal static readonly " )
                                    .Append( tFunc )
                                    .Append( " " )
                                    .Append( fieldName )
                                    .Append( " = " )
                                    .Append( funcName )
                                    .Append( ";" )
                                    .NewLine();

                    //var func = fB.ClassBuilder.DefineMethod(funcName);
                    //func.FrontModifiers.Add("private static");
                    //func.ReturnType = tFuncReturnType;
                    //func.Parameters.Add(new CodeGen.ParameterBuilder() { Name = "c", ParameterType = SqlObjectItem.TypeCommand.FullName } );

                    var fT = fB.ClassBuilder.CreateFunction( h =>
                                   h.Append( "private static " )
                                    .Append( tFuncReturnType )
                                    .Append( " " )
                                    .Append( funcName )
                                    .Append( "( " )
                                    .Append( SqlObjectItem.TypeCommand.FullName )
                                    .Append( " c )" ) );

                    // We may use a temporary object.
                    string tempObjectName = null;
                    Func<string> GetTempObjectName = () =>
                    {
                        if( tempObjectName == null )
                        {
                            fT.Append( "object tempObj;" ).NewLine();
                            tempObjectName = "tempObj";
                        }
                        return tempObjectName;
                    };
                    fT.Append("var parameters = c.Parameters;").NewLine();
                    string varName = EmitInlineReturn( fT, "parameters", GetTempObjectName );
                    fT.Append( "return " ).Append( varName ).Append( ";" ).NewLine();

                    fieldFullName = fB.ClassBuilder.FullName + '.' + fieldName;
                    dynamicAssembly.Memory[funcKey] = fieldFullName;
                }
                return fieldFullName;
            }

            void FinalizeFuncHolderType( IDynamicAssembly dynamicAssembly )
            {
                FuncTypeHolder fB = (FuncTypeHolder)dynamicAssembly.Memory[_funcHolderTypeName];
                System.Reflection.Emit.ConstructorBuilder cB = fB.TypeBuilder.DefineTypeInitializer();
                ILGenerator g = cB.GetILGenerator();
                FuncImpl f = fB.FirstFuncImpl;
                while( f != null )
                {
                    g.Emit( OpCodes.Ldnull );
                    g.Emit( OpCodes.Ldftn, f.Func );
                    g.Emit( OpCodes.Newobj, f.Field.FieldType.GetConstructor( new Type[]{ typeof( object ), typeof( IntPtr ) } ) );
                    g.Emit( OpCodes.Stsfld, f.Field );
                    f = f.Next;
                }
                g.Emit( OpCodes.Ret );
                fB.TypeBuilder.CreateTypeInfo();
            }

            #endregion

        }

    }
}
