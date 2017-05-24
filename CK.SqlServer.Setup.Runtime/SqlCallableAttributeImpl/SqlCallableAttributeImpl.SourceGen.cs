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

namespace CK.SqlServer.Setup
{

    static class CGExtensions
    {
        public static StringBuilder AppendIf(this StringBuilder @this, Action<StringBuilder> condition, Action<StringBuilder> then, Action<StringBuilder> @else = null )
        {
            @this.Append("if( ");
            condition(@this);
            @this.AppendLine(" )");
            @this.AppendLine("{");
            then(@this);
            @this.AppendLine("}");
            if( @else != null )
            {
                @this.AppendLine("else");
                @this.AppendLine("{");
                @else(@this);
                @this.AppendLine("}");
            }
            return @this;
        }
    }

    public partial class SqlCallableAttributeImpl
    {
        protected override bool DoImplement(
            IActivityMonitor monitor, 
            MethodInfo m, 
            SqlBaseItem sqlItem, 
            IDynamicAssembly dynamicAssembly, 
            ClassBuilder cB)
        {
            ISqlCallableItem item = sqlItem as ISqlCallableItem;
            if (item == null)
            {
                monitor.Fatal().Send($"The item '{item.FullName}' must be a ISqlCallableItem object to be able to generate call implementation.");
                return false;
            }
            MethodBuilder mCreateCommand = item.AssumeSourceCommandBuilder(monitor, dynamicAssembly);
            if (mCreateCommand == null)
            {
                monitor.Error().Send($"Invalid low level SqlCommand creation method for '{item.FullName}'.");
                return false;
            }
            ParameterInfo[] mParameters = m.GetParameters();
            GenerationType gType;
            ExecutionType execType = Attribute.ExecuteCall;

            // ExecuteCall parameter on the attribute.
            bool executeCall = execType != ExecutionType.Unknown;
            bool hasRefSqlCommand = mParameters.Length >= 1
                                    && mParameters[0].ParameterType.IsByRef
                                    && !mParameters[0].IsOut
                                    && mParameters[0].ParameterType.GetElementType() == SqlObjectItem.TypeCommand;

            // Simple case: void with a by ref command and no ExecuteCall.
            if (!executeCall && m.ReturnType == typeof(void) && hasRefSqlCommand)
            {
                gType = GenerationType.ByRefSqlCommand;
            }
            else
            {
                if (m.ReturnType == SqlObjectItem.TypeCommand)
                {
                    if (executeCall)
                    {
                        monitor.Error().Send("When a SqlCommand is returned, ExecuteCall must not be specified.", m.DeclaringType.FullName, m.Name);
                        return false;
                    }
                    gType = GenerationType.ReturnSqlCommand;
                }
                else
                {
                    if (m.ReturnType.GetConstructors().Any(ctor => ctor.GetParameters().Any(p => p.ParameterType == SqlObjectItem.TypeCommand && !p.ParameterType.IsByRef && !p.HasDefaultValue)))
                    {
                        if (executeCall)
                        {
                            monitor.Error().Send("When a Wrapper is returned, ExecuteNonQuery must not be specified.", m.DeclaringType.FullName, m.Name);
                            return false;
                        }
                        gType = GenerationType.ReturnWrapper;
                    }
                    else if (executeCall)
                    {
                        Debug.Assert(execType == ExecutionType.ExecuteNonQuery, "For the moment only ExecuteNonQuery is supported. Other modes will lead to new CallXXX generation types.");
                        gType = GenerationType.ExecuteNonQuery;
                    }
                    else
                    {
                        monitor.Error().Send("Method '{0}.{1}' must return a SqlCommand -OR- a type that has at least one constructor with a non optional SqlCommand (among other parameters) -OR- accepts a SqlCommand by reference as its first argument -OR- use ExecuteNonQuery mode.", m.DeclaringType.FullName, m.Name);
                        return false;
                    }
                }
            }
            return GenerateCreateSqlCommand(dynamicAssembly, gType, monitor, mCreateCommand, item.CallableObject, m, mParameters, cB, hasRefSqlCommand);
        }

        bool GenerateCreateSqlCommand(
            IDynamicAssembly dynamicAssembly, 
            GenerationType gType, 
            IActivityMonitor monitor, 
            MethodBuilder mCreateCommand, 
            ISqlServerCallableObject sqlObject, 
            MethodInfo m, 
            ParameterInfo[] mParameters, 
            ClassBuilder cB, 
            bool hasRefSqlCommand)
        {
            int nbError = 0;
            List<ParameterInfo> extraMethodParameters = null;
            cB.Build().DefineOverrideMethod(m, b =>
            {
                // We may use a temporary object.
                string tempObjectName = null;
                Func<string> GetTempObjectName = () =>
                {
                    if( tempObjectName == null )
                    {
                        b.AppendLine("object tempObj;");
                        tempObjectName = "tempObj";
                    }
                    return tempObjectName;
                };

                // First actual method parameter index (skips the ByRefSqlCommand if any).
                int mParameterFirstIndex = hasRefSqlCommand ? 1 : 0;

                // Starts by initializing out parameters to their Type's default value.
                for (int iM = mParameterFirstIndex; iM < mParameters.Length; ++iM)
                {
                    ParameterInfo mP = mParameters[iM];
                    if (mP.IsOut)
                    {
                        Debug.Assert(mP.ParameterType.IsByRef);
                        b.AppendLine($"{mP.Name} = default({mP.ParameterType.GetElementType()});");
                    }
                }
                b.AppendLine($"SqlCommand cmd_loc;");
                if (hasRefSqlCommand)
                {
                    string sqlCommandRefName = mParameters[0].Name;
                    b.AppendLine($"if({sqlCommandRefName} == null ) {sqlCommandRefName} = {mCreateCommand.TypeBuilder.FullName}.{mCreateCommand.Name}();");
                    b.AppendLine($"cmd_loc = {sqlCommandRefName};");
                }
                else b.AppendLine($"cmd_loc = {mCreateCommand.TypeBuilder.FullName}.{mCreateCommand.Name}();");
                b.AppendLine($"SqlCommandParameters cmd_parameters = cmd_loc.Parameters;");
                // SqlCommand is created.
                // Analyses parameters and generate removing of optional parameters if C# does not use them.

                SqlParameterHandlerList sqlParamHandlers = new SqlParameterHandlerList(sqlObject);
                // We initialize the SetUsedByReturnedType information on parameters 
                // so that they can relax their checks on Sql parameter direction accordingly.
                if ((gType & GenerationType.IsCall) != 0 && m.ReturnType != typeof(void))
                {
                    if (!sqlParamHandlers.HandleNonVoidCallingReturnedType(monitor, m.ReturnType)) ++nbError;
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
                SqlCallContextInfo sqlCallContexts = new SqlCallContextInfo(gType, m.ReturnType, mParameters);
                int iS = 0;
                for (int iM = mParameterFirstIndex; iM < mParameters.Length; ++iM)
                {
                    ParameterInfo mP = mParameters[iM];
                    int iSFound = sqlParamHandlers.IndexOf(iS, mP);
                    if (iSFound < 0)
                    {
                        Debug.Assert(SqlObjectItem.TypeConnection.GetTypeInfo().IsSealed && SqlObjectItem.TypeTransaction.GetTypeInfo().IsSealed);
                        // Catches first Connection and Transaction parameters.
                        if (firstSqlConnectionParameter == null && mP.ParameterType == SqlObjectItem.TypeConnection && !mP.ParameterType.IsByRef)
                        {
                            firstSqlConnectionParameter = mP;
                        }
                        else if (firstSqlTransactionParameter == null && mP.ParameterType == SqlObjectItem.TypeTransaction && !mP.ParameterType.IsByRef)
                        {
                            firstSqlTransactionParameter = mP;
                        }
                        else
                        {
                            // When we return a wrapper, we keep any extra parameters for wrappers.
                            if (gType == GenerationType.ReturnWrapper)
                            {
                                extraMethodParameters.Add(mP);
                            }
                            // If the parameter is a parameter source, we register it.
                            bool isParameterSourceOrCommandExecutor = sqlCallContexts.AddParameterSourceAndSqlCommandExecutor(mP, monitor, dynamicAssembly.GetPocoInfo());

                            if (mP.ParameterType.IsByRef && sqlParamHandlers.IsAsyncCall)
                            {
                                monitor.Error().Send("Parameter '{0}' is ref or out: ref or out are not compatible with an asynchronous execution (the returned type of the method is a Task).", mP.Name);
                                ++nbError;
                            }
                            else if (!isParameterSourceOrCommandExecutor
                                     && !(sqlParamHandlers.IsAsyncCall && mP.ParameterType == typeof(CancellationToken))
                                     && gType != GenerationType.ReturnWrapper)
                            {
                                // When direct parameters can not be mapped to Sql parameters, this is an error.
                                Debug.Assert(extraMethodParameters == null);
                                monitor.Error().Send("Parameter '{0}' not found in procedure parameters. Defined C# parameters must respect the actual stored procedure order.", mP.Name);
                                ++nbError;
                            }
                        }
                    }
                    else
                    {
                        var set = sqlParamHandlers.Handlers[iSFound];
                        if (!set.SetParameterMapping(mP, monitor)) ++nbError;
                        iS = iSFound + 1;
                    }
                }
                if (nbError == 0)
                {
                    // If there are sql parameters not covered, then they MUST:
                    // - be found as a property of one SqlCallContext object,
                    // - OR specify a default value,
                    // - OR be purely output.
                    // Otherwise,  have a default value.
                    foreach (var setter in sqlParamHandlers.Handlers)
                    {
                        if (!setter.IsMappedToMethodParameterOrParameterSourceProperty)
                        {
                            var sqlP = setter.SqlExprParam;
                            if (sqlCallContexts == null
                                || !sqlCallContexts.MatchPropertyToSqlParameter(setter, monitor))
                            {
                                if (sqlP.DefaultValue == null)
                                {
                                    // If it is a pure output parameters then we don't care setting a value for it.
                                    if (sqlP.IsPureOutput)
                                    {
                                        // Pure output. If it is the RETURN_VALUE of a function, we do not warn.
                                        if (!(sqlP is SqlParameterReturnedValue))
                                        {
                                            monitor.Info().Send("Method '{0}' does not declare the Sql Parameter '{1}'. Since it is a pure output parameter, it will be ignored.", m.Name, sqlP.ToStringClean());
                                        }
                                        setter.SetMappingToIgnoredOutput();
                                    }
                                    else
                                    {
                                        monitor.Error().Send("Sql parameter '{0}' in procedure parameters has no default value. The method '{1}' must declare it (or a property must exist in one of the [ParameterSource] parameters) or the procedure must specify the default value.", sqlP.Name, m.Name);
                                        ++nbError;
                                    }
                                }
                                else
                                {
                                    // The parameter has a default value.
                                    if (sqlP.IsPureOutput)
                                    {
                                        monitor.Warn().Send("Sql parameter '{0}' in procedure is a pure output parameter that has a default value. If the input matters, it should be marked /*input*/output.", sqlP.Name);
                                    }
                                    setter.SetMappingToSqlDefaultValue();
                                }
                            }
                        }
                    }
                }
                if (nbError == 0)
                {
                    Debug.Assert(sqlParamHandlers.Handlers.All(h => h.MappingDone));
                    // Configures Connection and Transaction properties if such method parameters appear.
                    // 1 - Sets SqlCommand.Connection from the parameter if it exists.
                    if (firstSqlConnectionParameter != null)
                    {
                        b.AppendLine($"cmd_loc.Connection = {firstSqlConnectionParameter.Name};");
                    }
                    // 2 - Sets SqlCommand.Transaction from the parameter if it exists.
                    // See: http://stackoverflow.com/questions/4013906/why-both-sqlconnection-and-sqltransaction-are-present-in-sqlcommand-constructor
                    if (firstSqlTransactionParameter != null)
                    {
                        b.AppendLine($"cmd_loc.Transaction = {firstSqlTransactionParameter.Name};");
                        if (firstSqlConnectionParameter == null)
                        {
                            // If the SqlTransaction is null, we set a null connection to be coherent.
                            b.AppendIf(
                                cb => cb.Append($"{firstSqlTransactionParameter.Name} != null"),
                                tb => tb.Append($"cmd_loc.Connection = {firstSqlTransactionParameter.Name}.Connection;"),
                                eb => eb.Append("cmd_loc.Connection = null;") );
                        }
                    }
                    b.AppendLine($"SqlParameter the_param;");
                    foreach (var setter in sqlParamHandlers.Handlers)
                    {
                        b.AppendLine($"the_param = cmd_parameters[{setter.Index}];");
                        if (!setter.EmitSetSqlParameterValue(monitor, b, "the_param")) ++nbError;
                    }
                }

                if (gType == GenerationType.ReturnSqlCommand)
                {
                    b.AppendLine("return cmd_loc;");
                }
                else if (gType == GenerationType.ReturnWrapper)
                {
                    var availableCtors = m.ReturnType.GetConstructors()
                                                        .Select(ctor => new WrapperCtorMatcher(ctor, extraMethodParameters, m.DeclaringType))
                                                        .Where(matcher => matcher.HasSqlCommand && matcher.Parameters.Count >= 1 + extraMethodParameters.Count)
                                                        .OrderByDescending(matcher => matcher.Parameters.Count)
                                                        .ToList();
                    if (availableCtors.Count == 0)
                    {
                        monitor.Error().Send($"The returned type '{m.ReturnType.Name}' has no public constructor that takes a SqlCommand and the {extraMethodParameters.Count} extra parameters of the method." );
                        ++nbError;
                    }
                    else
                    {
                        WrapperCtorMatcher matcher = availableCtors.FirstOrDefault(c => c.IsCallable());
                        if (matcher == null)
                        {
                            using (monitor.OpenError().Send($"Unable to find a constructor for the returned type '{m.ReturnType.Name}': the {extraMethodParameters.Count} extra parameters of the method cannot be mapped." ))
                            {
                                foreach (var mFail in availableCtors) mFail.ExplainFailure(monitor);
                            }
                            ++nbError;
                        }
                        else
                        {
                            matcher.LogWarnings(monitor);
                            b.Append($"return new {matcher.Ctor.DeclaringType.FullName}( ");
                            matcher.LdParameters(b, "cmd_loc", mParameters);
                            b.Append(" );");
                        }
                    }
                }
                else if ((gType & GenerationType.IsCall) != 0)
                {
                    if (sqlCallContexts == null || sqlCallContexts.SqlCommandExecutorParameter == null)
                    {
                        monitor.Error().Send("When calling with {0}, at least one ISqlCallContext object must be or exposes a ISqlCommandExecutor.", gType);
                        ++nbError;
                    }
                    else if (nbError == 0)
                    {
                        Debug.Assert(gType == GenerationType.ExecuteNonQuery);
                        if (sqlCallContexts.RequiresReturnTypeBuilder)
                        {
                            Debug.Assert(sqlCallContexts.IsAsyncCall);
                            string funcName = sqlParamHandlers.AssumeSourceFuncResultBuilder(dynamicAssembly, GetTempObjectName() );
                            b.Append("return ");
                            sqlCallContexts.GenerateExecuteNonQueryCall(b, "cmd_loc", funcName, mParameters );
                        }
                        else
                        {
                            if (sqlCallContexts.IsAsyncCall) b.Append("return ");
                            sqlCallContexts.GenerateExecuteNonQueryCall(b, "cmd_loc", null, mParameters);
                            if (!sqlCallContexts.IsAsyncCall)
                            {
                                foreach (var h in sqlParamHandlers.Handlers)
                                {
                                    h.EmitSetRefOrOutParameter(b, "cmd_parameters");
                                }
                                if (m.ReturnType != typeof(void))
                                {
                                    sqlParamHandlers.EmitInlineReturn(b, "cmd_parameters", GetTempObjectName());
                                }
                            }
                        }
                    }
                }
            });
            if (nbError != 0)
            {
                monitor.Info().Send(GenerateBothSignatures(sqlObject, m, mParameters, extraMethodParameters));
                return false;
            }
            return true;
        }
    }
}
