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

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl
    {
        enum GenerationType
        {
            ReturnSqlCommand,
            ByRefSqlCommand,
            ReturnWrapper
        }

        private bool GenerateCreateSqlCommand( GenerationType gType, IActivityMonitor monitor, MethodInfo createCommand, SqlExprMultiIdentifier sqlName, SqlExprParameterList sqlParameters, MethodInfo m, ParameterInfo[] mParameters, TypeBuilder tB, bool isVirtual )
        {
            MethodAttributes mA = m.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.VtableLayoutMask );
            if( isVirtual ) mA |= MethodAttributes.Virtual;
            MethodBuilder mB = tB.DefineMethod( m.Name, mA );
            if( m.ContainsGenericParameters )
            {
                int i = 0;

                Type[] genericArguments = m.GetGenericArguments();
                string[] names = genericArguments.Select( t => String.Format( "T{0}", i++ ) ).ToArray();

                var genericParameters = mB.DefineGenericParameters( names );
                for( i = 0; i<names.Length; ++i)
                {
                    Type genericTypeArgument = genericArguments[i];
                    GenericTypeParameterBuilder genericTypeBuilder = genericParameters[i];


                    genericTypeBuilder.SetGenericParameterAttributes( genericTypeArgument.GenericParameterAttributes );
                    genericTypeBuilder.SetInterfaceConstraints( genericTypeArgument.GetGenericParameterConstraints() );
                }
            }
            mB.SetReturnType( m.ReturnType );
            mB.SetParameters( ReflectionHelper.CreateParametersType( mParameters ) );

            ILGenerator g = mB.GetILGenerator();

            // First actual method parameter index (skips the ByRefSqlCommand if any).
            int mParameterFirstIndex = gType == GenerationType.ByRefSqlCommand ? 1 : 0;
            
            // Starts by initializing out parameters to their Type's default value.
            for( int iM = mParameterFirstIndex; iM < mParameters.Length; ++iM )
            {
                ParameterInfo mP = mParameters[iM];
                if( mP.IsOut ) g.StoreDefaultValueForOutParameter( mP );
            }
            LocalBuilder locCmd = g.DeclareLocal( SqlObjectItem.TypeCommand );
            LocalBuilder locParams = g.DeclareLocal( SqlObjectItem.TypeParameterCollection );
            LocalBuilder locOneParam = g.DeclareLocal( SqlObjectItem.TypeParameter );
            LocalBuilder tempObjToSet = g.DeclareLocal( typeof(object) );

            Label setValues = g.DefineLabel();
            if( gType == GenerationType.ByRefSqlCommand )
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

            SqlParametersSetter setters = new SqlParametersSetter( sqlParameters );

            // We directly manage the first occurrence of a SqlConnection and a SqlTransaction parameters by setting
            // them on the SqlCommand (whatever the generation type is).
            // - For mere SqlCommand (be it the returned object or the ByRef parameter) we must not have more extra 
            //   parameters (C# parameters that can not be found by name in stored procedure).
            // - When we create a wrapper, extra parameters are injected into the wrapper constructor (as long as we can map them).
            // - Method parameters that are SqlCallContext objects are registered in order to consider their properties as method parameters. 
            ParameterInfo firstSqlConnectionParameter = null;
            ParameterInfo firstSqlTransactionParameter = null;
            List<ParameterInfo> extraMethodParameters = gType == GenerationType.ReturnWrapper ? new List<ParameterInfo>() : null;
            SqlCallContextInfo sqlCallContexts = null;
            
            int iS = 0;
            for( int iM = mParameterFirstIndex; iM < mParameters.Length; ++iM )
            {
                ParameterInfo mP = mParameters[iM];
                int iSFound = setters.IndexOf( iS, mP );
                if( iSFound < 0 )
                {
                    Debug.Assert( SqlObjectItem.TypeConnection.IsSealed && SqlObjectItem.TypeTransaction.IsSealed );
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
                        // If the parameter is a SqlCallContext, we register it
                        // in order to consider its properties as method parameters.
                        if( SqlCallContextInfo.IsSqlCallContext( mP ) )
                        {
                            if( sqlCallContexts == null ) sqlCallContexts = new SqlCallContextInfo();
                            if( !sqlCallContexts.Add( mP, monitor ) ) ++nbError;
                        }
                        else if( gType != GenerationType.ReturnWrapper )
                        {
                            // When direct parameters can not be mapped to Sql parameters, this is an error.
                            Debug.Assert( extraMethodParameters == null );
                            monitor.Error().Send( "Parameter '{0}' not found in procedure parameters. Defined C# parameters must respect the actual stored procedure order.", mP.Name );
                            ++nbError;
                        }
                    }
                }
                else
                {
                    var set = setters.Setters[iSFound];
                    if( !set.SetParameterMapping( mP, monitor ) ) ++nbError;
                    iS = iSFound + 1;
                }
            }
            if( nbError == 0 )
            {
                // If there are sql parameters not covered, then they MUST:
                // - be purely output
                // - OR be found as a property of one SqlCallContext object,
                // - OR have a default value.
                foreach( var setter in setters.Setters )
                {
                    if( !setter.IsMapped )
                    {
                        var sqlP = setter.SqlExprParam;
                        if( !sqlP.IsInput )
                        {
                            monitor.Info().Send( "Method '{0}' does not declare the Sql Parameter '{1}'. Since it is an output parameter, it will be ignored.", m.Name, sqlP.ToStringClean() );
                            setter.SetMappingToIgnoredOutput();
                        }
                        else if( sqlCallContexts == null || !sqlCallContexts.FindMatchingProperty( setter, monitor ) )
                        {
                            if( sqlP.DefaultValue == null )
                            {
                                monitor.Error().Send( "Sql parameter '{0}' in procedure parameters has no default value. The method '{1}' must declare it.", sqlP.Variable.Identifier.Name, m.Name );
                                ++nbError;
                            }
                            else
                            {
                                monitor.Trace().Send( "Method '{0}' will use the default value for the Sql Parameter '{1}'.", m.Name, sqlP.Variable.Identifier.Name, sqlP.ToStringClean() );
                                setter.RemoveParameterForOptionalDefaultValue( g, locParams );
                            }
                        }
                    }
                }
            }
            // Entering the SetValues part.
            if( gType == GenerationType.ByRefSqlCommand ) g.MarkLabel( setValues );
            if( nbError == 0 )
            {
                // Configures Connection and Transaction properties if such method parameters appear.
                if( firstSqlConnectionParameter != null || firstSqlTransactionParameter != null )
                {
                    SetConnectionAndTransactionProperties( g, locCmd, firstSqlConnectionParameter, firstSqlTransactionParameter );
                }
                foreach( var setter in setters.Setters )
                {
                    setter.EmitSetParameter( g, locParams );
                }
            }
            if( gType == GenerationType.ByRefSqlCommand )
            {
                g.LdArg( 1 );
                g.LdLoc( locCmd );
                g.Emit( OpCodes.Stind_Ref );
            }
            else if( gType == GenerationType.ReturnSqlCommand )
            {
                g.LdLoc( locCmd );
            }
            else
            {
                var availableCtors = m.ReturnType.GetConstructors()
                                                    .Select( ctor => new WrapperCtorMatcher( ctor, extraMethodParameters, m.DeclaringType ) )
                                                    .Where( matcher => matcher.HasSqlCommand && matcher.Parameters.Count >= 1 + extraMethodParameters.Count )
                                                    .OrderByDescending( matcher => matcher.Parameters.Count )
                                                    .ToList();
                if( availableCtors.Count == 0 )
                {
                    monitor.Error().Send( "The returned type '{0}' has no public constructor that takes a SqlCommand and the {1} extra parameters of the method.", m.ReturnType.Name, extraMethodParameters.Count );
                    ++nbError;
                }
                else
                {
                    WrapperCtorMatcher matcher = availableCtors.FirstOrDefault( c => c.IsCallable() );
                    if( matcher == null )
                    {
                        using( monitor.OpenError().Send( "Unable to find a constructor for the returned type '{0}': the {1} extra parameters of the method cannot be mapped.", m.ReturnType.Name, extraMethodParameters.Count ) )
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
            if( nbError != 0 )
            {
                monitor.Info().Send( GenerateBothSignatures( sqlName, sqlParameters, m, mParameters, mParameterFirstIndex, extraMethodParameters ) );
            }
            g.Emit( OpCodes.Ret );
            return nbError == 0;
        }

        private static void GenerateByRefInitialization( ILGenerator g, LocalBuilder locCmd, LocalBuilder locParams, Label setValues )
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

        private static string GenerateBothSignatures( SqlExprMultiIdentifier sqlName, SqlExprParameterList sqlParameters, MethodInfo m, ParameterInfo[] mParameters, int mParameterFirstIndex, IList<ParameterInfo> extraParameters )
        {
            StringBuilder b = new StringBuilder();
            b.Append( "Procedure '" );
            sqlName.Tokens.WriteTokensWithoutTrivias( String.Empty, b );
            b.Append( "': " ).Append( sqlParameters.ToStringClean() );
            b.Append( Environment.NewLine );
            b.Append( "Method '" ).Append( m.DeclaringType.Name ).Append( '.' ).Append( m.Name ).Append( "': " );
            DumpParameters( b, mParameters.Skip( mParameterFirstIndex ) );
            if( m.ReturnType != typeof( void ) )
            {
                b.Append( " => " ).Append( m.ReturnType.Name );
            }
            if( extraParameters != null && extraParameters.Count > 0 )
            {
                b.Append( Environment.NewLine );
                b.Append( " - Extra Parameters: " );
                DumpParameters( b, extraParameters );
            }
            return b.ToString();
        }

        static string DumpParameters( IEnumerable<ParameterInfo> parameters )
        {
            StringBuilder b = new StringBuilder();
            DumpParameters( b, parameters );
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

        int IndexOf( SqlExprParameterList parameters, int iStart, string name )
        {
            while( iStart < parameters.Count )
            {
                if( StringComparer.OrdinalIgnoreCase.Equals( parameters[iStart].Variable.Identifier.Name, name ) ) return iStart;
                ++iStart;
            }
            return -1;
        }

        static bool CheckParameterType( Type t, SqlExprParameter p, IActivityMonitor monitor )
        {
            //TODO: Check .Net type against: p.Variable.TypeDecl.ActualType.DbType 
            return true;
        }

    }

}
