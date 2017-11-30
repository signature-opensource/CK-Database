using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using CK.Core;
using CK.Reflection;
using System.Linq;
using System.Text;
using CK.SqlServer.Parser;
using System.Threading;

namespace CK.SqlServer.Setup
{
    public partial class SqlCallableAttributeImpl
    {
        bool GenerateCreateSqlCommand( 
            IDynamicAssembly dynamicAssembly, 
            GenerationType gType, 
            IActivityMonitor monitor, 
            MethodInfo createCommand, 
            ISqlServerCallableObject sqlObject, 
            MethodInfo m, 
            ParameterInfo[] mParameters, 
            TypeBuilder tB, 
            bool isVirtual,
            bool hasRefSqlCommand )
        {
            MethodAttributes mA = m.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.VtableLayoutMask);
            if( isVirtual ) mA |= MethodAttributes.Virtual;
            MethodBuilder mB = tB.DefineMethod( m.Name, mA );
            if( m.ContainsGenericParameters )
            {
                int i = 0;

                Type[] genericArguments = m.GetGenericArguments();
                string[] names = genericArguments.Select( t => string.Format( "T{0}", i++ ) ).ToArray();

                var genericParameters = mB.DefineGenericParameters( names );
                for( i = 0; i < names.Length; ++i )
                {
                    Type genericTypeArgument = genericArguments[i];
                    GenericTypeParameterBuilder genericTypeBuilder = genericParameters[i];

                    genericTypeBuilder.SetGenericParameterAttributes( genericTypeArgument.GetTypeInfo().GenericParameterAttributes );
                    genericTypeBuilder.SetInterfaceConstraints( genericTypeArgument.GetTypeInfo().GetGenericParameterConstraints() );
                }
            }
            mB.SetReturnType( m.ReturnType );
            mB.SetParameters( ReflectionHelper.CreateParametersType( mParameters ) );

            ILGenerator g = mB.GetILGenerator();

            // First actual method parameter index (skips the ByRefSqlCommand if any).
            int mParameterFirstIndex = hasRefSqlCommand ? 1 : 0;

            // Starts by initializing out parameters to their Type's default value.
            for( int iM = mParameterFirstIndex; iM < mParameters.Length; ++iM )
            {
                ParameterInfo mP = mParameters[iM];
                if( mP.IsOut ) g.StoreDefaultValueForOutParameter( mP );
            }
            LocalBuilder locCmd = g.DeclareLocal( SqlObjectItem.TypeCommand );
            LocalBuilder locParams = g.DeclareLocal( SqlObjectItem.TypeParameterCollection );
            LocalBuilder locOneParam = g.DeclareLocal( SqlObjectItem.TypeParameter );
            LocalBuilder tempObjToSet = g.DeclareLocal( typeof( object ) );

            Label setValues = g.DefineLabel();

            if( hasRefSqlCommand )
            {
                GenerateByRefInitialization( g, locCmd, locParams, setValues );
            }
            // The SqlCommand must be created: we call the low-level createCommand method.
            g.Emit( OpCodes.Call, createCommand );
            g.Emit( OpCodes.Dup );
            g.StLoc( locCmd );
            g.Emit( OpCodes.Call, SqlObjectItem.MCommandGetParameters );
            g.StLoc( locParams );

            // We are in the Create command part.
            // Analyses parameters and generate removing of optional parameters if C# does not use them.
            int nbError = 0;

            SqlParameterHandlerList sqlParamHandlers = new SqlParameterHandlerList( sqlObject );

            // We initialize the SetUsedByReturnedType information on parameters 
            // so that they can relax their checks on Sql parameter direction accordingly.
            if( (gType & GenerationType.IsCall) != 0 && m.ReturnType != typeof( void ) )
            {
                if( !sqlParamHandlers.HandleNonVoidCallingReturnedType( monitor, m.ReturnType ) ) ++nbError;
            }

            // We directly manage the first occurrence of a SqlConnection and a SqlTransaction parameters by setting
            // them on the SqlCommand (whatever the generation type is).
            // - For mere SqlCommand (be it the returned object or the ByRef parameter) we must not have more extra 
            //   parameters (C# parameters that can not be found by name in stored procedure).
            // - When we create a wrapper, extra parameters are injected into the wrapper constructor (as long as we can map them).
            // - Method parameters that are SqlCallContext objects are registered in order to consider their properties as method parameters. 
            ParameterInfo firstSqlConnectionParameter = null;
            ParameterInfo firstSqlTransactionParameter = null;
            List<ParameterInfo> extraMethodParameters = gType == GenerationType.ReturnWrapper ? new List<ParameterInfo>() : null;
            SqlCallContextInfo sqlCallContexts = new SqlCallContextInfo( gType, m.ReturnType, mParameters );

            int iS = 0;
            for( int iM = mParameterFirstIndex; iM < mParameters.Length; ++iM )
            {
                ParameterInfo mP = mParameters[iM];
                int iSFound = sqlParamHandlers.IndexOf( iS, mP );
                if( iSFound < 0 )
                {
                    Debug.Assert( SqlObjectItem.TypeConnection.GetTypeInfo().IsSealed && SqlObjectItem.TypeTransaction.GetTypeInfo().IsSealed );
                    // Catches first Connection and Transaction parameters.
                    if( firstSqlConnectionParameter == null && mP.ParameterType == SqlObjectItem.TypeConnection && !mP.ParameterType.IsByRef )
                    {
                        firstSqlConnectionParameter = mP;
                    }
                    else if( firstSqlTransactionParameter == null && mP.ParameterType == SqlObjectItem.TypeTransaction && !mP.ParameterType.IsByRef )
                    {
                        firstSqlTransactionParameter = mP;
                    }
                    else
                    {
                        // When we return a wrapper, we keep any extra parameters for wrappers.
                        if( gType == GenerationType.ReturnWrapper )
                        {
                            extraMethodParameters.Add( mP );
                        }
                        // If the parameter is a parameter source, we register it.
                        bool isParameterSourceOrCommandExecutor = sqlCallContexts.AddParameterSourceAndSqlCommandExecutor( mP, monitor, dynamicAssembly.GetPocoInfo() );

                        if( mP.ParameterType.IsByRef && sqlParamHandlers.IsAsyncCall )
                        {
                            monitor.Error( $"Parameter '{mP.Name}' is ref or out: ref or out are not compatible with an asynchronous execution (the returned type of the method is a Task)." );
                            ++nbError;
                        }
                        else if( !isParameterSourceOrCommandExecutor
                                 && !(sqlParamHandlers.IsAsyncCall && mP.ParameterType == typeof(CancellationToken)) 
                                 && gType != GenerationType.ReturnWrapper )
                        {
                            // When direct parameters can not be mapped to Sql parameters, this is an error.
                            Debug.Assert( extraMethodParameters == null );
                            monitor.Error( $"Parameter '{mP.Name}' not found in procedure parameters. Defined C# parameters must respect the actual stored procedure order." );
                            ++nbError;
                        }
                    }
                }
                else
                {
                    var set = sqlParamHandlers.Handlers[iSFound];
                    if( !set.SetParameterMapping( mP, monitor ) ) ++nbError;
                    iS = iSFound + 1;
                }
            }
            
            if( nbError == 0 )
            {
                // If there are sql parameters not covered, then they MUST:
                // - be found as a property of one SqlCallContext object,
                // - OR specify a default value,
                // - OR be purely output.
                // Otherwise,  have a default value.
                foreach( var setter in sqlParamHandlers.Handlers )
                {
                    if( !setter.IsMappedToMethodParameterOrParameterSourceProperty )
                    {
                        var sqlP = setter.SqlExprParam;
                        if( sqlCallContexts == null 
                            || !sqlCallContexts.MatchPropertyToSqlParameter( setter, monitor ) )
                        {
                            if( sqlP.DefaultValue == null )
                            {
                                // If it is a pure output parameters then we don't care setting a value for it.
                                if( sqlP.IsPureOutput )
                                {
                                    if( !setter.IsUsedByReturnType )
                                    {
                                        monitor.Info( $"Method '{m.Name}' does not declare the Sql Parameter '{sqlP.ToStringClean()}'. Since it is a pure output parameter, it will be ignored." );
                                    }
                                    setter.SetMappingToIgnoredOutput();
                                }
                                else
                                {
                                    monitor.Error( $"Sql parameter '{sqlP.Name}' in procedure parameters has no default value. The method '{m.Name}' must declare it (or a property must exist in one of the [ParameterSource] parameters) or the procedure must specify the default value." );
                                    ++nbError;
                                }
                            }
                            else
                            {
                                // The parameter has a default value.
                                if( sqlP.IsPureOutput )
                                {
                                    monitor.Warn( $"Sql parameter '{sqlP.Name}' in procedure is a pure output parameter that has a default value. If the input matters, it should be marked /*input*/output." );
                                }
                                setter.SetMappingToSqlDefaultValue();
                            }
                        }
                    }
                }
            }
            // Entering the SetValues part.
            if( hasRefSqlCommand ) g.MarkLabel( setValues );
            if( nbError == 0 )
            {
                Debug.Assert( sqlParamHandlers.Handlers.All( h => h.MappingDone ) );
                // Configures Connection and Transaction properties if such method parameters appear.
                if( firstSqlConnectionParameter != null || firstSqlTransactionParameter != null )
                {
                    SetConnectionAndTransactionProperties( g, locCmd, firstSqlConnectionParameter, firstSqlTransactionParameter );
                }
                foreach( var setter in sqlParamHandlers.Handlers )
                {
                    if( !setter.EmitSetSqlParameterValue( monitor, g, locParams ) ) ++nbError;
                }
            }

            if( hasRefSqlCommand )
            {
                g.LdArg( 1 );
                g.LdLoc( locCmd );
                g.Emit( OpCodes.Stind_Ref );
            }

            if( gType == GenerationType.ReturnSqlCommand )
            {
                g.LdLoc( locCmd );
            }
            else if( gType == GenerationType.ReturnWrapper )
            {
                var availableCtors = m.ReturnType.GetConstructors()
                                                    .Select( ctor => new WrapperCtorMatcher( ctor, extraMethodParameters, m.DeclaringType ) )
                                                    .Where( matcher => matcher.HasSqlCommand && matcher.Parameters.Count >= 1 + extraMethodParameters.Count )
                                                    .OrderByDescending( matcher => matcher.Parameters.Count )
                                                    .ToList();
                if( availableCtors.Count == 0 )
                {
                    monitor.Error( $"The returned type '{m.ReturnType.Name}' has no public constructor that takes a SqlCommand and the {extraMethodParameters.Count} extra parameters of the method." );
                    ++nbError;
                }
                else
                {
                    WrapperCtorMatcher matcher = availableCtors.FirstOrDefault( c => c.IsCallable() );
                    if( matcher == null )
                    {
                        using( monitor.OpenError( $"Unable to find a constructor for the returned type '{m.ReturnType.Name}': the {extraMethodParameters.Count} extra parameters of the method cannot be mapped." ) )
                        {
                            foreach( var mFail in availableCtors ) mFail.ExplainFailure( monitor );
                        }
                        ++nbError;
                    }
                    else
                    {
                        matcher.LogWarnings( monitor );
                        matcher.LdParameters( (ModuleBuilder)tB.Module, g, locCmd );
                        g.Emit( OpCodes.Newobj, matcher.Ctor );
                    }
                }
            }
            else if( (gType & GenerationType.IsCall) != 0 )
            {
                if( sqlCallContexts == null || sqlCallContexts.SqlCommandExecutorParameter == null )
                {
                    monitor.Error( $"When calling with {gType}, at least one ISqlCallContext object must be or exposes a ISqlCommandExecutor." );
                    ++nbError;
                }
                else if( nbError == 0 )
                {
                    Debug.Assert( gType == GenerationType.ExecuteNonQuery );
                    if( sqlCallContexts.RequiresReturnTypeBuilder )
                    {
                        Debug.Assert( sqlCallContexts.IsAsyncCall );
                        FieldInfo fR = sqlParamHandlers.AssumeResultBuilder( dynamicAssembly );
                        sqlCallContexts.GenerateExecuteNonQueryCall( g, locCmd, fR );
                        // The Async call leaves the task on the stack.
                    }
                    else
                    {
                        sqlCallContexts.GenerateExecuteNonQueryCall( g, locCmd, null );
                        if( !sqlCallContexts.IsAsyncCall )
                        {
                            foreach( var h in sqlParamHandlers.Handlers )
                            {
                                h.EmitSetRefOrOutParameter( g, locParams );
                            }
                            if( m.ReturnType != typeof(void) )
                            {
                                sqlParamHandlers.EmitInlineReturn( g, locParams ); 
                            }
                        }
                        // The Async call leaves the task on the stack.
                    }
                }
            }
            if( nbError != 0 )
            {
                monitor.Info( GenerateBothSignatures( sqlObject, m, mParameters, extraMethodParameters ) );
            }
            g.Emit( OpCodes.Ret );
            return nbError == 0;
        }

        static void GenerateByRefInitialization( ILGenerator g, LocalBuilder locCmd, LocalBuilder locParams, Label setValues )
        {
            // For ByRef SqlCommand, generates code that checks for null arguments:
            // we must create the SqlCommand in this case.
            Label doCreate = g.DefineLabel();
            g.LdArg( 1 );
            g.Emit( OpCodes.Ldind_Ref );
            g.Emit( OpCodes.Dup );
            g.StLoc( locCmd );
            g.Emit( OpCodes.Ldnull );
            g.Emit( OpCodes.Beq_S, doCreate );

            // Generates the code that retrieves the get_Parameters() method from
            // the already created SqlCommand and jumps to setValues section.
            g.LdLoc( locCmd );
            g.Emit( OpCodes.Call, SqlObjectItem.MCommandGetParameters );
            g.StLoc( locParams );

            g.Emit( OpCodes.Br, setValues );
            g.MarkLabel( doCreate );
        }

        static void SetConnectionAndTransactionProperties( ILGenerator g, LocalBuilder locCmd, ParameterInfo firstSqlConnectionParameter, ParameterInfo firstSqlTransactionParameter )
        {
            // 1 - Sets SqlCommand.Connection from the parameter if it exists.
            if( firstSqlConnectionParameter != null )
            {
                g.LdLoc( locCmd );
                g.LdArg( firstSqlConnectionParameter.Position + 1 );
                g.Emit( OpCodes.Call, SqlObjectItem.MCommandSetConnection );
            }
            // 2 - Sets SqlCommand.Transaction from the parameter if it exists.
            if( firstSqlTransactionParameter != null )
            {
                // See: http://stackoverflow.com/questions/4013906/why-both-sqlconnection-and-sqltransaction-are-present-in-sqlcommand-constructor
                g.LdLoc( locCmd );
                g.LdArg( firstSqlTransactionParameter.Position + 1 );
                g.Emit( OpCodes.Call, SqlObjectItem.MCommandSetTransaction );

                // 2-bis: Sets SqlCommand.Connection from the SqlTransaction parameter if it exists.
                Label endConnFromTransaction = g.DefineLabel();
                if( firstSqlConnectionParameter != null )
                {
                    // If the Connection parameter exists, checks if it is set to a non null Connection:
                    // when a non null connection has been set, does nothing.
                    g.LdArg( firstSqlConnectionParameter.Position + 1 );
                    g.Emit( OpCodes.Brtrue_S, endConnFromTransaction );
                }
                // If the SqlTransaction is null, we set a null connection to be coherent.
                Label setNullConnection = g.DefineLabel();
                g.LdLoc( locCmd );
                g.LdArg( firstSqlTransactionParameter.Position + 1 );
                g.Emit( OpCodes.Brfalse_S, setNullConnection );

                g.LdArg( firstSqlTransactionParameter.Position + 1 );
                g.Emit( OpCodes.Call, SqlObjectItem.MTransactionGetConnection );
                g.Emit( OpCodes.Call, SqlObjectItem.MCommandSetConnection );
                g.Emit( OpCodes.Br_S, endConnFromTransaction );

                g.MarkLabel( setNullConnection );
                g.Emit( OpCodes.Ldnull );
                g.Emit( OpCodes.Call, SqlObjectItem.MCommandSetConnection );

                g.MarkLabel( endConnFromTransaction );
            }
        }

        static string GenerateBothSignatures( ISqlServerCallableObject sqlObject, MethodInfo m, ParameterInfo[] mParameters, IList<ParameterInfo> extraParameters )
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine( sqlObject.ToStringSignature( false ) );
            DumpMethodSignature( b, m, mParameters );
            if( extraParameters != null && extraParameters.Count > 0 )
            {
                b.Append( Environment.NewLine );
                b.Append( " - Extra Parameters: " );
                DumpParameters( b, extraParameters );
            }
            return b.ToString();
        }

        internal static string DumpMethodSignature( MethodInfo m )
        {
            StringBuilder b = new StringBuilder();
            DumpMethodSignature( b, m );
            return b.ToString();
        }

        static void DumpMethodSignature( StringBuilder b, MethodInfo m, IEnumerable<ParameterInfo> mParameters = null )
        {
            b.Append( "Method " ).Append( m.DeclaringType.Name ).Append( '.' ).Append( m.Name ).Append( "( " );
            DumpParameters( b, mParameters ?? m.GetParameters() );
            b.Append( " )" );
            if( m.ReturnType != typeof( void ) )
            {
                b.Append( " => " ).Append( m.ReturnType.Name );
            }
        }

        internal static string DumpParameters( IEnumerable<ParameterInfo> parameters, bool withParenthesis )
        {
            StringBuilder b = new StringBuilder();
            if( withParenthesis ) b.Append( "( " );
            DumpParameters( b, parameters );
            if( withParenthesis ) b.Append( " )" );
            return b.ToString();
        }

        static void DumpParameters( StringBuilder b, IEnumerable<ParameterInfo> parameters )
        {
            bool atLeastOne = false;
            foreach( var mP in parameters )
            {
                atLeastOne = DumpParameter( b, atLeastOne, mP );
            }
        }

        static string DumpParameter( ParameterInfo mP, bool commaPrefix = false )
        {
            StringBuilder b = new StringBuilder();
            DumpParameter( b, commaPrefix, mP );
            return b.ToString();
        }

        static bool DumpParameter( StringBuilder b, bool atLeastOne, ParameterInfo mP )
        {
            if( atLeastOne ) b.Append( ", " );
            else atLeastOne = true;
            if( mP.ParameterType.IsByRef )
            {
                b.Append( mP.IsOut ? "out " : "ref " ).Append( mP.ParameterType.GetElementType().Name );
            }
            else b.Append( mP.ParameterType.Name );
            b.Append( ' ' ).Append( mP.Name );
            if( !mP.ParameterType.IsByRef && mP.HasDefaultValue )
            {
                object d = mP.DefaultValue;
                if( d == null ) b.Append( " = null" );
                else b.Append( " = " ).Append( d.ToString() );
            }
            return atLeastOne;
        }

        static bool CheckParameterType( Type t, ISqlServerParameter p, IActivityMonitor monitor )
        {
            if( p.SqlType.IsTypeCompatible( t ) ) return true;
            monitor.Error( $"Sql parameter '{p.ToStringClean()}' is not compliant with Type {t.Name}." );
            return false;
        }


    }

}
