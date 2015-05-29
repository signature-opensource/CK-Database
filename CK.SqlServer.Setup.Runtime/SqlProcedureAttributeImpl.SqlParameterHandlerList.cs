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

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl
    {
        /// <summary>
        /// Manages sql parameters thanks to <see cref="SqlParamHandler"/> objects that are built on <see cref="SqlExprParameter"/> objects
        /// and can be associated to <see cref="ParameterInfo"/>.
        /// </summary>
        class SqlParameterHandlerList
        {
            readonly List<SqlParamHandler> _params;
            SqlParamHandler _simpleReturnType;
            bool _isAsyncCall;
            Type _unwrappedReturnedType;
            readonly StringBuilder _funcResultBuilderSignature;

            public class SqlParamHandler
            {
                readonly SqlParameterHandlerList _holder;

                public readonly SqlExprParameter SqlExprParam;
                // ParameterName without the '@' prefix char.
                public readonly string SqlParameterName;
                readonly int _index;

                ParameterInfo _methodParam;
                SqlCallContextInfo.Property _ctxProp;
                bool _isIgnoredOutputParameter;
                bool _isUseDefaultSqlValue;
                bool _isUsedByReturnedType;

                public SqlParamHandler( SqlParameterHandlerList holder, SqlExprParameter sP, int index )
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
                public bool IsMappedToMethodParameterOrCallContextProperty
                {
                    get { return _methodParam != null || _ctxProp != null; }
                }

                public int Index { get { return _index; } }

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
                    Debug.Assert( !IsMappedToMethodParameterOrCallContextProperty );
                    _methodParam = mP;
                    if( !CheckParameter( mP, SqlExprParam, monitor ) ) return false;
                    return true;
                }

                bool CheckParameter( ParameterInfo mP, SqlExprParameter p, IActivityMonitor monitor )
                {
                    int nbError = CheckParameterDirection( mP, p, monitor );
                    return nbError == 0 && CheckParameterType( mP.ParameterType, p, monitor );
                }

                int CheckParameterDirection( ParameterInfo mP, SqlExprParameter p, IActivityMonitor monitor )
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
                            #region out method parameter
                            if( sqlIsInputOutput )
                            {
                                if( _isUsedByReturnedType )
                                {
                                    monitor.Warn().Send( "Sql parameter '{0}' is an /*input*/output parameter. The method '{1}' should use 'ref' for it (not 'out') or no ref nor out since this parameter is used by returned call.", p.Variable.Identifier.Name, mP.Member.Name );
                                }
                                else
                                {
                                    monitor.Error().Send( "Sql parameter '{0}' is an /*input*/output parameter. The method '{1}' must use 'ref' for it (not 'out').", p.Variable.Identifier.Name, mP.Member.Name );
                                    ++nbError;
                                }
                            }
                            else if( sqlIsInput )
                            {
                                Debug.Assert( !sqlIsOutput );
                                monitor.Error().Send( "Sql parameter '{0}' is an input parameter. The method '{1}' can not use 'out' for it (and 'ref' modifier will be useless).", p.Variable.Identifier.Name, mP.Member.Name );
                                ++nbError;
                            }
                            #endregion
                        }
                        else
                        {
                            // ref method parameter.
                            if( !sqlIsOutput )
                            {
                                monitor.Warn().Send( "Sql parameter '{0}' is not an output parameter. The method '{1}' uses 'ref' for it. That is useless.", p.Variable.Identifier.Name, mP.Member.Name );
                            }
                        }
                    }
                    else
                    {
                        // by value parameter.
                        if( sqlIsInputOutput )
                        {
                            if( !_isUsedByReturnedType )
                            {
                                monitor.Warn().Send( "Sql parameter '{0}' is an /*input*/output parameter and this parameter is not returned. The method '{1}' should use 'ref' to retrieve the new value after the call.", p.Variable.Identifier.Name, mP.Member.Name );
                            }
                        }
                        else if( sqlIsOutput )
                        {
                            if( _isUsedByReturnedType )
                            {
                                monitor.Warn().Send( "Sql parameter '{0}' is an output parameter whose value will be returned. Setting its value is useless. Should it be marked /*input*/output?.", p.Variable.Identifier.Name );
                            }
                            else
                            {
                                monitor.Error().Send( "Sql parameter '{0}' is an output parameter. The method '{1}' must use 'out' for the parameter (you can also simply remove the method's parameter the output value can be ignored).", p.Variable.Identifier.Name, mP.Member.Name );
                                ++nbError;
                            }
                        }
                    }
                    return nbError;
                }
                
                /// <summary>
                /// 3 - If this Sql parameter is not mapped to a method parameter, then it may be mapped by a property of one 
                /// of the SqlCallContext objects.
                /// </summary>
                public void SetParameterMapping( SqlCallContextInfo.Property prop )
                {
                    Debug.Assert( !IsMappedToMethodParameterOrCallContextProperty );
                    _ctxProp = prop;
                }

                /// <summary>
                /// 4.1 - If no previous mapping has been found and the parameter is purely output and has no Sql default value.
                /// </summary>
                public void SetMappingToIgnoredOutput()
                {
                    Debug.Assert( !IsMappedToMethodParameterOrCallContextProperty );
                    _isIgnoredOutputParameter = true;
                }

                /// <summary>
                /// 4.2 - If no previous mapping has been found and the parameter has a Sql default value.
                /// </summary>
                public void SetMappingToSqlDefaultValue()
                {
                    Debug.Assert( !IsMappedToMethodParameterOrCallContextProperty );
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
                    if( _methodParam != null )
                    {
                        typeToSet = _methodParam.ParameterType;
                        g.LdArg( _methodParam.Position + 1 );
                        if( typeToSet.IsByRef )
                        {
                            typeToSet = typeToSet.GetElementType();
                            if( typeToSet.IsValueType )
                            {
                                g.Emit( OpCodes.Ldobj, typeToSet );
                            }
                            else
                            {
                                g.Emit( OpCodes.Ldind_Ref );
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert( _ctxProp != null );
                        g.LdArg( _ctxProp.Parameter.Position + 1 );
                        g.Emit( OpCodes.Callvirt, _ctxProp.Prop.GetGetMethod() );
                        typeToSet = _ctxProp.Prop.PropertyType;
                    }
                    // The value is on the stack: it may be a value or reference type of type typeToSet.
                    // If it is null or a Nullable<T> that has no value, we must transform it into DBNull.Value.
                    Label objectIsAvailable = g.DefineLabel();
                    if( typeToSet.IsValueType )
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
                        monitor.Error().Send( "Parameter '{0}' has default value '{1}' that is a variable. This not supported.", SqlExprParam.ToStringClean(), SqlExprParam.DefaultValue.ToString() );
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
                    else if( o is string )
                    {
                        g.Emit( OpCodes.Ldstr, (string)o );
                    }
                    else
                    {
                        monitor.Error().Send( "Emit for type  '{1}' (Parameter '{0}') is not supported.", SqlExprParam.ToStringClean(), o.GetType().Name );
                        return false;
                    }
                    return true;
                }

                internal bool EmitSetSqlParameterValue( IActivityMonitor monitor, ILGenerator g, LocalBuilder locParameterCollection )
                {
                    if( _isIgnoredOutputParameter ) return true;
                    if( _isUseDefaultSqlValue )
                    {
                        monitor.Info().Send( "Parameter '{0}' will use its default value.", SqlExprParam.ToStringClean() );
                        return EmitSetParameterCode( monitor, g, locParameterCollection, LdObjectToSetFromDefaultSqlValue );
                    }
                    Debug.Assert( IsMappedToMethodParameterOrCallContextProperty );
                    // Do not set any Value if the C# parameter is out.
                    if( _methodParam != null && _methodParam.IsOut ) return true;
                    return EmitSetParameterCode( monitor, g, locParameterCollection, LdObjectToSetFromParameterOrCallContextProperty );
                }

                bool EmitSetParameterCode( IActivityMonitor monitor, ILGenerator g, LocalBuilder locParameterCollection, Func<IActivityMonitor,ILGenerator,bool> valueLoader )
                {
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
                    // Load object on the stack.
                    if( !valueLoader( monitor, g ) ) return false;
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterSetValue );
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
                    if( t.IsValueType )
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
            }

            public SqlParameterHandlerList( SqlExprParameterList parameters )
            {
                _params = parameters.Select( ( p, idx ) => new SqlParamHandler( this, p, idx ) ).ToList();
                _funcResultBuilderSignature = new StringBuilder();
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

            public bool IsAsyncCall
            {
                get { return _isAsyncCall; }
            }

            internal bool HandleNonVoidCallingReturnedType( IActivityMonitor monitor, Type returnType )
            {
                if( returnType == typeof( Task ) ) return _isAsyncCall = true;
                bool isSimpleType = IsSimpleReturnType( returnType );
                if( !isSimpleType && returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>) )
                {
                    _isAsyncCall = true;
                    returnType = returnType.GetGenericArguments()[0];
                    isSimpleType = IsSimpleReturnType( returnType );
                }
                if( isSimpleType )
                {
                    // Look for the first last (reverse order) parameter for which the returned type is compatible.
                    for( int i = _params.Count - 1; i >= 0; --i )
                    {
                        var p = _params[i];
                        if( p.SqlExprParam.IsOutput && p.SqlExprParam.Variable.TypeDecl.ActualType.IsTypeCompatible( returnType ) )
                        {
                            _simpleReturnType = p;
                            _unwrappedReturnedType = returnType;
                            _funcResultBuilderSignature.Append( _unwrappedReturnedType.FullName ).Append('-').Append( p.Index );
                            p.SetUsedByReturnedType();
                            return true;
                        }
                    }
                }
                else
                {
                }
                monitor.Error().Send( "Unable to find a way to return the required return type '{0}'.", returnType.Name );
                return false;
            }

            internal void EmitInlineReturn( ILGenerator g, LocalBuilder locParameterCollection )
            {
                if( _simpleReturnType != null )
                {
                    g.LdLoc( locParameterCollection );
                    g.LdInt32( _simpleReturnType.Index );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                    g.Emit( OpCodes.Call, SqlObjectItem.MParameterGetValue );
                    if( _unwrappedReturnedType.IsValueType )
                    {
                        g.Emit( OpCodes.Unbox_Any, _unwrappedReturnedType );
                    }
                    else
                    {
                        g.Emit( OpCodes.Castclass, _unwrappedReturnedType );
                    }
                }
                else
                {

                }

            }

            static bool IsSimpleReturnType( Type returnType )
            {
                return SqlHelper.IsNetTypeMapped( returnType );
            }

            #region Result builders functions (AssumeResultBuilder)

            const string _funcHolderTypeName = "CK.<FuncResultBuilder>";

            class FuncTypeHolder
            {
                public readonly TypeBuilder TypeBuilder;
                public FuncImpl FirstFuncImpl;
                public FuncTypeHolder( TypeBuilder b ) { TypeBuilder = b; }
            }

            class FuncImpl
            {
                public readonly FieldInfo Field;
                public readonly MethodInfo Func;
                public readonly FuncImpl Next;
                public FuncImpl( FieldInfo field, MethodInfo func, FuncImpl next ) { Field = field; Func = func; Next = next; }
            }

            internal FieldInfo AssumeResultBuilder( IDynamicAssembly dynamicAssembly )
            {
                FuncTypeHolder fB = (FuncTypeHolder)dynamicAssembly.Memory[_funcHolderTypeName];
                if( fB == null )
                {
                    TypeBuilder tB = dynamicAssembly.ModuleBuilder.DefineType( _funcHolderTypeName, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.NotPublic );
                    dynamicAssembly.Memory.Add( _funcHolderTypeName, (fB = new FuncTypeHolder( tB )) );
                    dynamicAssembly.PushFinalAction( FinalizeFuncHolderType );
                }
                string funcKey = _funcHolderTypeName + ':' + _funcResultBuilderSignature.ToString();
                FuncImpl f = (FuncImpl)dynamicAssembly.Memory[funcKey];
                if( f == null )
                {
                    string funcName = 'f' + dynamicAssembly.NextUniqueNumber();
                    string fieldName = "_" + funcName;
                    Type tFunc = typeof( Func<,> ).MakeGenericType( SqlObjectItem.TypeCommand, _unwrappedReturnedType );
                    var field = fB.TypeBuilder.DefineField( fieldName, tFunc, FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly );
                    var func = fB.TypeBuilder.DefineMethod( funcName, MethodAttributes.Private | MethodAttributes.Static, _unwrappedReturnedType, new Type[]{ SqlObjectItem.TypeCommand } );
                    ILGenerator g = func.GetILGenerator();
                    LocalBuilder locParams = g.DeclareLocal( SqlObjectItem.TypeParameterCollection );
                    g.LdArg( 0 );
                    g.Emit( OpCodes.Callvirt, SqlObjectItem.MCommandGetParameters );
                    g.StLoc( locParams );
                    EmitInlineReturn( g, locParams );
                    g.Emit( OpCodes.Ret );
                    dynamicAssembly.Memory[funcKey] = (fB.FirstFuncImpl = f = new FuncImpl( field, func, fB.FirstFuncImpl ));
                }
                return f.Field;
            }

            void FinalizeFuncHolderType( IDynamicAssembly dynamicAssembly )
            {
                FuncTypeHolder fB = (FuncTypeHolder)dynamicAssembly.Memory[_funcHolderTypeName];
                ConstructorBuilder cB = fB.TypeBuilder.DefineTypeInitializer();
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
                fB.TypeBuilder.CreateType();
            }

            #endregion

        }

    }
}
