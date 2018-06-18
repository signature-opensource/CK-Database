#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlProcedureAttributeImpl.SqlParameterHandlerList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.CodeGen;
using CK.CodeGen.Abstractions;
using CK.Core;
using CK.SqlServer.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
                bool isChar = false;
                if( targetType.IsValueType )
                {
                    isNullable = false;
                    if( !(isChar = (targetType == typeof(char))) )
                    {
                        Type actualType = Nullable.GetUnderlyingType( targetType );
                        if( actualType != null )
                        {
                            isNullable = true;
                            if( actualType == typeof( char ) ) isChar = true;
                            else if( actualType.IsEnum ) enumUnderlyingType = Enum.GetUnderlyingType( actualType );
                        }
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
                else if( !isChar )
                {
                    b.Append( '(' )
                        .AppendCSharpName( targetType )
                        .Append( ')' );
                }
                if( isChar )
                {
                    b.Append( "((string)" ).Append( tempObjName() ).Append( ")[0]" );
                }
                else b.Append( tempObjName() );
                b.Append( ";" ).NewLine();
                return resultName;
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

            internal string AssumeSourceFuncResultBuilder( IDynamicAssembly dynamicAssembly )
            {
                string funcKey = "S:_build_func_:" + _funcResultBuilderSignature.ToString();
                string fieldFullName = (string)dynamicAssembly.Memory[funcKey];
                if( fieldFullName == null )
                {
                    ITypeScope t = dynamicAssembly.DefaultGenerationNamespace.FindType( "_build_func_" );
                    if( t == null )
                    {
                        t = dynamicAssembly.DefaultGenerationNamespace.CreateType( "static class _build_func_" );
                        t.CreateFunction(
                            @"public static async System.Threading.Tasks.Task<T> FuncBuilderHelper<T>(
                                this ISqlConnectionController @this,
                                SqlCommand cmd,
                                Func<SqlCommand, T> resultBuilder,
                                System.Threading.CancellationToken cancellationToken )" )
                          .Append( "await @this.ExecuteNonQueryAsync( cmd, cancellationToken );" )
                          .Append( "return resultBuilder( cmd );" );
                    }
                    string funcName = 'f' + dynamicAssembly.NextUniqueNumber();
                    string fieldName = "_" + funcName;
                    string tFuncReturnType = _unwrappedReturnedType.ToCSharpName();
                    string tFunc = $"Func<{SqlObjectItem.TypeCommand.FullName},{tFuncReturnType}>";
                    t.Append( "internal static readonly " )
                        .Append( tFunc )
                        .Append( " " )
                        .Append( fieldName )
                        .Append( " = " )
                        .Append( funcName )
                        .Append( ";" )
                        .NewLine();

                    var fT = t.CreateFunction( h =>
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

                    fieldFullName = t.FullName + '.' + fieldName;
                    dynamicAssembly.Memory[funcKey] = fieldFullName;
                }
                return fieldFullName;
            }

            #endregion

        }

    }
}
