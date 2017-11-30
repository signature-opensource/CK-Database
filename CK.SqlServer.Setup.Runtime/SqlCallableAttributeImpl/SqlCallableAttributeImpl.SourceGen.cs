using System;
using System.Collections;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using CK.Reflection;
using CK.CodeGen;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CK.CodeGen.Abstractions;

namespace CK.SqlServer.Setup
{

    public partial class SqlCallableAttributeImpl
    {
        [Flags]
        enum GenerationType
        {
            ReturnSqlCommand = 1,
            ByRefSqlCommand = 2,
            ReturnWrapper = 3,
            IsCall = 4,
            ExecuteNonQuery = IsCall | 0
        }

        protected override bool DoImplement(
            IActivityMonitor monitor,
            MethodInfo m,
            SqlBaseItem sqlItem,
            IDynamicAssembly dynamicAssembly,
            ITypeScope cB )
        {
            ISqlCallableItem item = sqlItem as ISqlCallableItem;
            if( item == null )
            {
                monitor.Fatal( $"The item '{item.FullName}' must be a ISqlCallableItem object to be able to generate call implementation." );
                return false;
            }
            IFunctionScope mCreateCommand = item.AssumeSourceCommandBuilder( monitor, dynamicAssembly );
            if( mCreateCommand == null )
            {
                monitor.Error( $"Invalid low level SqlCommand creation method for '{item.FullName}'." );
                return false;
            }
            ParameterInfo[] mParameters = m.GetParameters();
            GenerationType gType;

            // ExecuteCall parameter on the attribute.
            bool executeCall = !Attribute.NoCall;
            bool hasRefSqlCommand = mParameters.Length >= 1
                                    && mParameters[0].ParameterType.IsByRef
                                    && !mParameters[0].IsOut
                                    && mParameters[0].ParameterType.GetElementType() == SqlObjectItem.TypeCommand;

            // Simple case: void with a by ref command and no ExecuteCall.
            if( !executeCall && m.ReturnType == typeof( void ) && hasRefSqlCommand )
            {
                gType = GenerationType.ByRefSqlCommand;
            }
            else
            {
                if( m.ReturnType == SqlObjectItem.TypeCommand )
                {
                    if( executeCall )
                    {
                        monitor.Error( $"Method '{m.DeclaringType.FullName}.{m.Name}': When a SqlCommand is returned, ExecuteCall must not be specified." );
                        return false;
                    }
                    gType = GenerationType.ReturnSqlCommand;
                }
                else
                {
                    if( m.ReturnType.GetConstructors().Any( ctor => ctor.GetParameters().Any( p => p.ParameterType == SqlObjectItem.TypeCommand && !p.ParameterType.IsByRef && !p.HasDefaultValue ) ) )
                    {
                        if( executeCall )
                        {
                            monitor.Error( $"Method '{m.DeclaringType.FullName}.{m.Name}': When a Wrapper is returned, ExecuteNonQuery must not be specified." );
                            return false;
                        }
                        gType = GenerationType.ReturnWrapper;
                    }
                    else if( executeCall )
                    {
                        gType = GenerationType.ExecuteNonQuery;
                    }
                    else
                    {
                        monitor.Error( $"Method '{m.DeclaringType.FullName}.{m.Name}' must return a SqlCommand -OR- a type that has at least one constructor with a non optional SqlCommand (among other parameters) -OR- accepts a SqlCommand by reference as its first argument -OR- use ExecuteNonQuery mode." );
                        return false;
                    }
                }
            }
            return GenerateCreateSqlCommand( dynamicAssembly, gType, monitor, mCreateCommand, item.CallableObject, m, mParameters, cB, hasRefSqlCommand );
        }

        static bool GenerateCreateSqlCommand(
            IDynamicAssembly dynamicAssembly,
            GenerationType gType,
            IActivityMonitor monitor,
            IFunctionScope mCreateCommand,
            ISqlServerCallableObject sqlObject,
            MethodInfo m,
            ParameterInfo[] mParameters,
            ITypeScope tB,
            bool hasRefSqlCommand )
        {
            int nbError = 0;
            List<ParameterInfo> extraMethodParameters = null;
            IFunctionScope cB = tB.CreateOverride( m );
            // We may need a temporary object.
            string tempObjectName = null;
            Func<string> GetTempObjectName = () =>
                {
                    if( tempObjectName == null )
                    {
                        cB.Append( "object tempObj;" ).NewLine();
                        tempObjectName = "tempObj";
                    }
                    return tempObjectName;
                };
            // First actual method parameter index (skips the ByRefSqlCommand if any).
            int mParameterFirstIndex = hasRefSqlCommand ? 1 : 0;

            // Starts by initializing out parameters to their Type's default value.
            for( int iM = mParameterFirstIndex; iM < mParameters.Length; ++iM )
            {
                ParameterInfo mP = mParameters[iM];
                if( mP.IsOut )
                {
                    Debug.Assert( mP.ParameterType.IsByRef );
                    cB.Append( $"{mP.Name} = default({mP.ParameterType.GetElementType().ToCSharpName()});" ).NewLine();
                }
            }
            cB.Append( $"SqlCommand cmd_loc;" ).NewLine();
            if( hasRefSqlCommand )
            {
                string sqlCommandRefName = mParameters[0].Name;
                cB.Append( $"if({sqlCommandRefName} == null ) {sqlCommandRefName} = {mCreateCommand.EnclosingType.FullName}.{mCreateCommand.FunctionName.NakedName}();" ).NewLine();
                cB.Append( "cmd_loc = " ).Append( sqlCommandRefName ).Append( ";" ).NewLine();
            }
            else cB.Append( $"cmd_loc = {mCreateCommand.EnclosingType.FullName}.{mCreateCommand.FunctionName.NakedName}();" ).NewLine();
            cB.Append( $"SqlParameterCollection cmd_parameters = cmd_loc.Parameters;" ).NewLine();
            // SqlCommand is created.
            // Analyses parameters and generate removing of optional parameters if C# does not use them.

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
            extraMethodParameters = gType == GenerationType.ReturnWrapper
                                                ? new List<ParameterInfo>()
                                                : null;
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
                                 && !(sqlParamHandlers.IsAsyncCall && mP.ParameterType == typeof( CancellationToken ))
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
                foreach( var handler in sqlParamHandlers.Handlers )
                {
                    if( !handler.IsMappedToMethodParameterOrParameterSourceProperty )
                    {
                        var sqlP = handler.SqlExprParam;
                        if( sqlCallContexts == null
                            || !sqlCallContexts.MatchPropertyToSqlParameter( handler, monitor ) )
                        {
                            if( sqlP.DefaultValue == null )
                            {
                                // If it is a pure output parameters then we don't care setting a value for it.
                                if( sqlP.IsPureOutput )
                                {
                                    if( !handler.IsUsedByReturnType )
                                    {
                                        monitor.Info( $"Method '{m.Name}' does not declare the Sql Parameter '{sqlP.ToStringClean()}'. Since it is a pure output parameter, it will be ignored." );
                                    }
                                    handler.SetMappingToIgnoredOutput();
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
                                handler.SetMappingToSqlDefaultValue();
                            }
                        }
                    }
                }
            }
            if( nbError == 0 )
            {
                Debug.Assert( sqlParamHandlers.Handlers.All( h => h.MappingDone ) );
                // Configures Connection and Transaction properties if such method parameters appear.
                // See: http://stackoverflow.com/questions/4013906/why-both-sqlconnection-and-sqltransaction-are-present-in-sqlcommand-constructor
                if( firstSqlConnectionParameter != null && firstSqlTransactionParameter != null )
                {
                    // If both parameters are presents, the transaction wins if it is not null.
                    cB.Append( "if( " ).Append( firstSqlTransactionParameter.Name ).Append( " != null )" ).NewLine()
                        .Append( "{" ).NewLine()
                        .Append( "  cmd_loc.Transaction = " ).Append( firstSqlTransactionParameter.Name ).Append( ";" ).NewLine()
                        .Append( "  cmd_loc.Connection = " ).Append( firstSqlTransactionParameter.Name ).Append( ".Connection;" ).NewLine()
                        .Append( "}" ).NewLine()
                        .Append( "else cmd_loc.Connection = " ).Append( firstSqlConnectionParameter.Name ).Append( ";" ).NewLine();
                }
                else if( firstSqlConnectionParameter != null )
                {
                    // Only the connection parameter.
                    cB.Append( "cmd_loc.Connection = " ).Append( firstSqlConnectionParameter.Name ).Append( ";" ).NewLine();
                }
                else if( firstSqlTransactionParameter != null )
                {
                    // Only the transaction parameter: the connection is the one of the transaction if
                    // it is not null.
                    cB.Append( "  cmd_loc.Transaction = " ).Append( firstSqlTransactionParameter.Name ).Append( ";" ).NewLine()
                    .Append( "  cmd_loc.Connection = " ).Append( firstSqlTransactionParameter.Name ).Append( "?.Connection;" ).NewLine();
                }
                if( sqlParamHandlers.Handlers.Count > 0 )
                {
                    cB.Append( $"SqlParameter tP;" ).NewLine();
                    foreach( var setter in sqlParamHandlers.Handlers )
                    {
                        cB.Append( "tP = cmd_parameters[" ).Append( setter.Index ).Append( "];" ).NewLine();
                        if( !setter.EmitSetSqlParameterValue( monitor, cB, "tP" ) ) ++nbError;
                    }
                }
            }

            if( gType == GenerationType.ReturnSqlCommand )
            {
                cB.Append( "return cmd_loc;" ).NewLine();
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
                        cB.Append( "return new " )
                            .AppendCSharpName( matcher.Ctor.DeclaringType )
                            .Append( "( " );
                        matcher.LdParameters( cB, "cmd_loc", mParameters );
                        cB.Append( " );" );
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
                        string funcName = sqlParamHandlers.AssumeSourceFuncResultBuilder( dynamicAssembly );
                        cB.Append( "return " );
                        sqlCallContexts.GenerateExecuteNonQueryCall( cB, "cmd_loc", funcName, mParameters );
                    }
                    else
                    {
                        if( sqlCallContexts.IsAsyncCall ) cB.Append( "return " );
                        sqlCallContexts.GenerateExecuteNonQueryCall( cB, "cmd_loc", null, mParameters );
                        if( !sqlCallContexts.IsAsyncCall )
                        {
                            foreach( var h in sqlParamHandlers.Handlers )
                            {
                                h.EmitSetRefOrOutParameter( cB, "cmd_parameters", GetTempObjectName );
                            }
                            if( m.ReturnType != typeof( void ) )
                            {
                                string resultVarName = sqlParamHandlers.EmitInlineReturn( cB, "cmd_parameters", GetTempObjectName );
                                cB.Append( "return " ).Append( resultVarName ).Append( ";" ).NewLine();
                            }
                        }
                    }
                }
            }

            if( nbError == 0 ) return true;
            monitor.Info( GenerateBothSignatures( sqlObject, m, mParameters, extraMethodParameters ) );
            return false;
        }
    }
}
