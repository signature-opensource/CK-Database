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

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl 
    {


        private bool GenerateCreateSqlCommand( bool createOrSetValues, IActivityLogger logger, MethodInfo createCommand, SqlExprMultiIdentifier sqlName, SqlExprParameterList sqlParameters, MethodInfo m, ParameterInfo[] mParameters, TypeBuilder tB, bool isVirtual )
        {
            MethodAttributes mA = m.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.VtableLayoutMask);
            if( isVirtual ) mA |= MethodAttributes.Virtual;
            MethodBuilder mB = tB.DefineMethod( m.Name, mA, m.ReturnType, ReflectionHelper.CreateParametersType( mParameters ) );
            ILGenerator g = mB.GetILGenerator();
            // Start by initializing out parameters to their Type's defalut value.
            for( int iM = createOrSetValues ? 1 : 0; iM < mParameters.Length; ++iM )
            {
                ParameterInfo mP = mParameters[iM];
                if( mP.IsOut ) g.StoreDefaultValueForOutParameter( mP );
            }
            LocalBuilder locCmd = g.DeclareLocal( SqlObjectItem.TypeCommand );
            LocalBuilder locParams = g.DeclareLocal( SqlObjectItem.TypeParameterCollection );
            LocalBuilder locOneParam = g.DeclareLocal( SqlObjectItem.TypeParameter );
            LocalBuilder tempObjToSet = g.DeclareLocal( typeof(object) );

            Label setValues = g.DefineLabel();
            if( createOrSetValues )
            {
                Label doCreate = g.DefineLabel();
                g.LdArg( 1 );
                g.Emit( OpCodes.Ldind_Ref );
                g.Emit( OpCodes.Dup );
                g.StLoc( locCmd );
                g.Emit( OpCodes.Ldnull );
                g.Emit( OpCodes.Beq_S, doCreate );

                g.LdLoc( locCmd );
                g.Emit( OpCodes.Callvirt, SqlObjectItem.MCommandGetParameters );
                g.StLoc( locParams );

                g.Emit( OpCodes.Br, setValues );
                g.MarkLabel( doCreate );
            }

            g.Emit( OpCodes.Call, createCommand );
            g.Emit( OpCodes.Dup );
            g.StLoc( locCmd );
            g.Emit( OpCodes.Callvirt, SqlObjectItem.MCommandGetParameters );
            g.StLoc( locParams );

            // We are in the Create command part.
            // Analyses parameters and generate removing of optional parameters if C# does not use them.
            int nbError = 0;
            Debug.Assert( mParameters.Length - 1 <= sqlParameters.Count );
            var notFound = new List<SqlExprParameter>( sqlParameters );
            List<ParameterInfo> valuesToSetParam = null;
            List<int> valuesToSetSqlIndex = null;
            int iS = 0;
            for( int iM = createOrSetValues ? 1 : 0; iM < mParameters.Length; ++iM )
            {
                ParameterInfo mP = mParameters[iM];
                int iSFound = IndexOf( sqlParameters, iS, '@' + mP.Name );
                if( iSFound < 0 )
                {
                    logger.Error( "Parameter '{0}' not found in procedure parameters. Defined parameters must respect the actual procedure order.", mP.Name );
                    ++nbError;
                }
                else
                {
                    SqlExprParameter p = sqlParameters[ iSFound ];
                    notFound[iSFound] = null;
                    if( !CheckParameter( mP, p, logger ) ) ++nbError;
                    // Configures the SqlParameter.Value with the parameter value. 
                    if( p.IsInput )
                    {
                        if( valuesToSetParam == null ) 
                        {
                            valuesToSetParam = new List<ParameterInfo>(); 
                            valuesToSetSqlIndex = new List<int>(); 
                        }
                        valuesToSetParam.Add( mP );
                        valuesToSetSqlIndex.Add( iSFound );
                    }
                    iS = iSFound + 1;
                }
            }
            if( nbError == 0 )
            {
                // If there are sql parameters not covered, then they MUST
                // have a default value or be purely output.
                for( int iN = 0; iN < notFound.Count; ++iN )
                {
                    SqlExprParameter p = notFound[iN];
                    if( p != null )
                    {
                        if( !p.IsInput )
                        {
                            logger.Info( "Method '{0}' does not declare the Sql Parameter '{1}'. Since it is an output parameter, it will be ignored.", m.Name, p.ToStringClean() );
                        }
                        else if( p.DefaultValue == null )
                        {
                            logger.Error( "Sql parameter '{0}' in procedure parameters has no default value. The method '{1}' must declare it.", p.Variable.Identifier.Name, m.Name );
                            ++nbError;
                        }
                        else
                        {
                            logger.Trace( "Method '{0}' will use the default value for the Sql Parameter '{1}'.", m.Name, p.Variable.Identifier.Name, p.ToStringClean() );
                            // Removing the optional parameter.
                            g.LdLoc( locParams );
                            g.LdInt32( iN );
                            g.Emit( OpCodes.Callvirt, SqlObjectItem.MParameterCollectionRemoveAtParameter );
                            // Adjust captured position in the parameter list.
                            if( valuesToSetSqlIndex != null )
                            {
                                for( int i = 0; i < valuesToSetSqlIndex.Count; ++i )
                                {
                                    if( valuesToSetSqlIndex[i] > iN ) --valuesToSetSqlIndex[i];
                                }
                            }
                        }
                    }
                }
            }
            if( nbError != 0 )
            {
                StringBuilder b = new StringBuilder();
                b.Append( "Method '" ).Append( m.DeclaringType.Name ).Append( '.' ).Append( m.Name ).Append( "': " );
                bool atLeastOne = false;
                for( int iM = createOrSetValues ? 1 : 0; iM < mParameters.Length; ++iM )
                {
                    if( atLeastOne ) b.Append( ", " );
                    else atLeastOne = true;
                    ParameterInfo mP = mParameters[iM];
                    if( mP.ParameterType.IsByRef )
                    {
                        b.Append( mP.IsOut ? "out " : "ref " ).Append( mP.ParameterType.GetElementType().Name );
                    }
                    else b.Append( mP.ParameterType.Name );
                    b.Append( ' ' ).Append( mP.Name );
                }
                b.Append( Environment.NewLine );
                b.Append( "Procedure '" );
                sqlName.Tokens.WriteTokensWithoutTrivias( String.Empty, b );
                b.Append( "': " ).Append( sqlParameters.ToStringClean() );
                logger.Filter = LogLevelFilter.Info;
                logger.Info( b.ToString() );
            }
            // Entering the SetValues part.
            if( createOrSetValues ) g.MarkLabel( setValues );
            if( valuesToSetParam != null )
            {
                for( int i = 0; i < valuesToSetParam.Count; ++i )
                {
                    g.LdLoc( locParams );
                    g.LdInt32( valuesToSetSqlIndex[i] );
                    g.Emit( OpCodes.Callvirt, SqlObjectItem.MParameterCollectionGetParameter );
                    Label notNull = g.DefineLabel();
                    g.LdArgBox( valuesToSetParam[i] );
                    g.Emit( OpCodes.Dup );
                    g.Emit( OpCodes.Brtrue_S, notNull );
                    g.Emit( OpCodes.Pop );
                    g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                    g.MarkLabel( notNull );
                    g.Emit( OpCodes.Callvirt, SqlObjectItem.MParameterSetValue );
                }
            }
            if( createOrSetValues )
            {
                g.LdArg( 1 );
                g.LdLoc( locCmd );
                g.Emit( OpCodes.Stind_Ref );
            }
            else g.LdLoc( locCmd );
            g.Emit( OpCodes.Ret );
            return nbError == 0;
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

        bool CheckParameter( ParameterInfo mP, SqlExprParameter p, IActivityLogger logger )
        {
            int nbError = CheckParameterDirection( mP, p, logger );
            //TODO: Check .Net type against: p.Variable.TypeDecl.ActualType.DbType 
            return nbError == 0;
        }

        private static int CheckParameterDirection( ParameterInfo mP, SqlExprParameter p, IActivityLogger logger )
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
                        logger.Error( "Sql parameter '{0}' is an /*input*/output parameter. The method '{1}' must use 'ref' for it (not 'out').", p.Variable.Identifier.Name, mP.Member.Name );
                        ++nbError;
                    }
                    else if( sqlIsInput )
                    {
                        Debug.Assert( !sqlIsOutput );
                        logger.Error( "Sql parameter '{0}' is an input parameter. The method '{1}' can not use 'out' for it (and 'ref' modifier will be useless).", p.Variable.Identifier.Name, mP.Member.Name );
                        ++nbError;
                    }
                }
                else
                {
                    if( !sqlIsOutput )
                    {
                        logger.Warn( "Sql parameter '{0}' is not an output parameter. The method '{1}' uses 'ref' for it that is useless.", p.Variable.Identifier.Name, mP.Member.Name );
                    }
                }
            }
            else
            {
                if( sqlIsInputOutput )
                {
                    logger.Warn( "Sql parameter '{0}' is an /*input*/output parameter. The method '{1}' should use 'ref' to retreive the new value after the call.", p.Variable.Identifier.Name, mP.Member.Name );
                }
                else if( sqlIsOutput )
                {
                    logger.Error( "Sql parameter '{0}' is an output parameter. The method '{1}' must use 'out' for the parameter (you can also simply remove the method's parameter the output value can be ignored).", p.Variable.Identifier.Name, mP.Member.Name );
                    ++nbError;
                }
            }
            return nbError;
        }

    }

}
