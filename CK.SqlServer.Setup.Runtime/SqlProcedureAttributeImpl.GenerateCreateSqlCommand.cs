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
        enum GenerationType
        {
            ReturnSqlCommand,
            ByRefSqlCommand,
            ReturnWrapper
        }

        private bool GenerateCreateSqlCommand( GenerationType gType, IActivityMonitor monitor, MethodInfo createCommand, SqlExprMultiIdentifier sqlName, SqlExprParameterList sqlParameters, MethodInfo m, ParameterInfo[] mParameters, TypeBuilder tB, bool isVirtual )
        {
            MethodAttributes mA = m.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.VtableLayoutMask);
            if( isVirtual ) mA |= MethodAttributes.Virtual;
            MethodBuilder mB = tB.DefineMethod( m.Name, mA, m.ReturnType, ReflectionHelper.CreateParametersType( mParameters ) );
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
                // When ByRef, generates code that checks for null arguments:
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
            // The SqlCommand must be created: we call the low-level createCommand method.
            g.Emit( OpCodes.Call, createCommand );
            g.Emit( OpCodes.Dup );
            g.StLoc( locCmd );
            g.Emit( OpCodes.Call, SqlObjectItem.MCommandGetParameters );
            g.StLoc( locParams );

            // We are in the Create command part.
            // Analyses parameters and generate removing of optional parameters if C# does not use them.
            int nbError = 0;
            
            // The notFoundSql initially contains all sqlParameters. 
            // When they are associated (by name) the a method parameter, the slot is set to null.
            var notFoundSql = sqlParameters.ToArray();

            // We directly manage the first occurrence of a SqlConnection and a SqlTransaction parameters by setting
            // them on the SqlCommand (whatever the generation type is).
            // - For mere SqlCommand (be it the returned object or the ByRef parameter) we must not have more extra 
            //   parameters (C# parameters that can not be found by name in stored procedure).
            // - When we create a wrapper, extra parameters are injected into the wrapper constructor (as long as we can map them).
            ParameterInfo firstSqlConnectionParameter = null;
            ParameterInfo firstSqlTransactionParameter = null;
            List<ParameterInfo> extraMethodParameters = gType == GenerationType.ReturnWrapper ? new List<ParameterInfo>() : null;

            // This 2 lists are used to sets SqlParameter initial value to the actual method parameter value (for input and /*input*/output sql parameters).
            List<ParameterInfo> valuesToSetParam = null;
            List<int> valuesToSetSqlIndex = null;
            
            int iS = 0;
            for( int iM = mParameterFirstIndex; iM < mParameters.Length; ++iM )
            {
                ParameterInfo mP = mParameters[iM];
                int iSFound = IndexOf( sqlParameters, iS, '@' + mP.Name );
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
                        // Then handle any other extra parameters for wrappers.
                        if( gType == GenerationType.ReturnWrapper )
                        {
                            extraMethodParameters.Add( mP );
                        }
                        else
                        {
                            Debug.Assert( extraMethodParameters == null );
                            monitor.Error().Send( "Parameter '{0}' not found in procedure parameters. Defined C# parameters must respect the actual stored procedure order.", mP.Name );
                            ++nbError;
                        }
                    }
                }
                else
                {
                    SqlExprParameter p = sqlParameters[ iSFound ];
                    notFoundSql[iSFound] = null;
                    if( !CheckParameter( mP, p, monitor ) ) ++nbError;
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
                for( int iN = 0; iN < notFoundSql.Length; ++iN )
                {
                    SqlExprParameter p = notFoundSql[iN];
                    if( p != null )
                    {
                        if( !p.IsInput )
                        {
                            monitor.Info().Send( "Ctor '{0}' does not declare the Sql Parameter '{1}'. Since it is an output parameter, it will be ignored.", m.Name, p.ToStringClean() );
                        }
                        else if( p.DefaultValue == null )
                        {
                            monitor.Error().Send( "Sql parameter '{0}' in procedure parameters has no default value. The method '{1}' must declare it.", p.Variable.Identifier.Name, m.Name );
                            ++nbError;
                        }
                        else
                        {
                            monitor.Trace().Send( "Ctor '{0}' will use the default value for the Sql Parameter '{1}'.", m.Name, p.Variable.Identifier.Name, p.ToStringClean() );
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
            // Entering the SetValues part.
            if( gType == GenerationType.ByRefSqlCommand ) g.MarkLabel( setValues );
            if( nbError == 0 )
            {
                // Configures Connection and Transaction properties if such method parameters appear.
                if( firstSqlConnectionParameter != null || firstSqlTransactionParameter != null )
                {
                    SetConnectionAndTransactionProperties( g, locCmd, firstSqlConnectionParameter, firstSqlTransactionParameter );
                }
                if( valuesToSetParam != null )
                {
                    for( int i = 0; i < valuesToSetParam.Count; ++i )
                    {
                        g.LdLoc( locParams );
                        g.LdInt32( valuesToSetSqlIndex[i] );
                        g.Emit( OpCodes.Call, SqlObjectItem.MParameterCollectionGetParameter );
                        Label notNull = g.DefineLabel();
                        g.LdArgBox( valuesToSetParam[i] );
                        g.Emit( OpCodes.Dup );
                        g.Emit( OpCodes.Brtrue_S, notNull );
                        g.Emit( OpCodes.Pop );
                        g.Emit( OpCodes.Ldsfld, SqlObjectItem.FieldDBNullValue );
                        g.MarkLabel( notNull );
                        g.Emit( OpCodes.Call, SqlObjectItem.MParameterSetValue );
                    }
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
                                                    .Select( ctor => new CtorMatcher( ctor, extraMethodParameters, m.DeclaringType ) )
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
                    CtorMatcher matcher = availableCtors.FirstOrDefault( c => c.IsCallable() );
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

        class CtorMatcher
        {
            public readonly ConstructorInfo Ctor;
            public readonly IReadOnlyList<ParameterInfo> Parameters;
            // Contains either:
            // - ParameterInfo from _methodParameters or
            // - ParameterInfo from our Parameters if the default value exists and must be used or
            // - The _declaringTypeMarker.
            readonly ParameterInfo[] _mappedParameters;
            readonly MethodParameter[] _methodParameters;
            readonly int _idxSqlCommand;
            readonly Type _declaringType;
            readonly static ParameterInfo _declaringTypeMarker = typeof( CtorMatcher ).GetConstructors()[0].GetParameters()[2];
            readonly StringBuilder _warnings;

            public CtorMatcher( ConstructorInfo m, IReadOnlyList<ParameterInfo> methodParameters, Type declaringType )
            {
                Ctor = m;
                Parameters = m.GetParameters();
                _declaringType = declaringType;
                _mappedParameters = new ParameterInfo[Parameters.Count];
                _methodParameters = methodParameters.Select( p => new MethodParameter( p ) ).ToArray();
                _idxSqlCommand = Parameters.IndexOf( p => p.ParameterType == SqlObjectItem.TypeCommand && !p.ParameterType.IsByRef && !p.HasDefaultValue );
                _warnings = new StringBuilder();
            }

            class MethodParameter
            {
                public readonly ParameterInfo Parameter;
                public int IdxTarget;

                public MethodParameter( ParameterInfo p )
                {
                    Parameter = p;
                    IdxTarget = -1;
                }
            }

            public bool HasSqlCommand
            {
                get { return _idxSqlCommand >= 0; }
            }

            internal bool IsCallable()
            {
                Debug.Assert( _idxSqlCommand >= 0 );
                Debug.Assert( _methodParameters.All( p => p.IdxTarget == -1 ) );
                Debug.Assert( _mappedParameters.All( p => p == null ) );

                for( int i = 0; i < Parameters.Count; ++i )
                {
                    if( i == _idxSqlCommand ) continue;
                    
                    ParameterInfo toMatch = Parameters[i];
                    Debug.Assert( toMatch.Position == i );
 
                    var exactCandidates = _methodParameters.Where( p => p.IdxTarget == -1 
                                                                    && toMatch.ParameterType.Equals( p.Parameter.ParameterType ) 
                                                                    && toMatch.IsOut == p.Parameter.IsOut ).ToList();
                    if( TrySetCandidate( toMatch, exactCandidates ) ) continue;
                    var candidates = _methodParameters.Where( p => p.IdxTarget == -1
                                                                && !toMatch.ParameterType.IsByRef
                                                                && !p.Parameter.ParameterType.IsByRef
                                                                && !toMatch.ParameterType.Equals( p.Parameter.ParameterType )
                                                                && toMatch.ParameterType.IsAssignableFrom( p.Parameter.ParameterType ) ).ToList();
                    if( TrySetCandidate( toMatch, candidates ) ) continue;
                    // No method parameter found. Try the declaring type itself, or the default value.
                    if( !toMatch.ParameterType.IsByRef )
                    {
                        if( toMatch.ParameterType.IsAssignableFrom( _declaringType ) )
                        {
                            _mappedParameters[i] = _declaringTypeMarker;
                        }
                        else if( IsValidDefaultValue( toMatch ) ) _mappedParameters[i] = toMatch;
                    }
                }
                return _methodParameters.All( p => p.IdxTarget >= 0 ) && !_mappedParameters.Where( ( p, idx ) => idx != _idxSqlCommand && p == null ).Any();
            }

            internal void ExplainFailure( IActivityMonitor monitor )
            {
                using( monitor.OpenInfo().Send( "Considering constructor: {0}.", DumpParameters( _methodParameters.Select( p => p.Parameter ) ) ) )
                {
                    foreach( var bothP in _mappedParameters.Select( ( p, idx ) => idx != _idxSqlCommand && p != _declaringTypeMarker && p.Member != Ctor
                                                                                ? new { CtorP = Parameters[idx], MethodP = p }
                                                                                : null )
                                                        .Where( p => p != null ) )
                    {
                        monitor.Trace().Send( "Constructor parameter {0} is bound to method parameter {1}.", DumpParameter( bothP.CtorP ), bothP.MethodP );
                    }
                    foreach( var cP in _mappedParameters.Select( ( p, idx ) => idx != _idxSqlCommand && p.Member == Ctor ? Parameters[idx] : null )
                                                        .Where( p => p != null ) )
                    {
                        monitor.Trace().Send( "Constructor parameter {0} uses its default value.", DumpParameter( cP ) );
                    }
                    foreach( var cP in _mappedParameters.Where( ( p, idx ) => p == _declaringTypeMarker ) )
                    {
                        monitor.Trace().Send( "Constructor parameter {0} is bound to the Type that defines the method ({1}).", DumpParameter( cP ), _declaringType.FullName );
                    }
                    foreach( var cP in _mappedParameters.Select( ( p, idx ) => p == null && idx != _idxSqlCommand ? Parameters[idx] : null )
                                                        .Where( p => p != null ) )
                    {
                        if( cP.HasDefaultValue ) monitor.Error().Send( "Unable to use default value for constructor parameter {0}.", DumpParameter( cP ) );
                        else monitor.Error().Send( "Unable to map constructor parameter {0}.", DumpParameter( cP ) );
                    }
                    foreach( var mP in _methodParameters.Where( p => p.IdxTarget == -1 ) )
                    {
                        monitor.Error().Send( "Unable to map extra method parameter {0}.", DumpParameter( mP.Parameter ) );
                    }
                }
            }

            internal void LogWarnings( IActivityMonitor monitor )
            {
                if( _warnings.Length > 0 ) monitor.Warn().Send( _warnings.ToString() );
            }

            internal void LdParameters( ModuleBuilder mB, ILGenerator g, LocalBuilder locCmd )
            {
                int i = 0;
                foreach( var mP in _mappedParameters )
                {
                    if( i == _idxSqlCommand )
                    {
                        g.LdLoc( locCmd );
                    }
                    else if( mP == _declaringTypeMarker )
                    {
                        g.LdArg( 0 );
                        g.Emit( OpCodes.Castclass, Parameters[i].ParameterType );
                    }
                    else if( mP.Member == Ctor )
                    {
                        Debug.Assert( IsValidDefaultValue( mP ) );
                        Debug.Assert( mP.Position == i, "This is the PrameterInfo of the constructor." );

                        object d =  mP.DefaultValue;
                        if( d == null )
                        {
                            g.Emit( OpCodes.Ldnull );
                        }
                        else
                        {
                            Type dT = d.GetType();
                            if( dT.Equals( typeof( Int32 ) ) || dT.Equals( typeof( Int16 ) ) || dT.Equals( typeof( sbyte ) ) )
                            {
                                g.LdInt32( (int)d );
                            }
                            else if( dT.Equals( typeof( string ) ) )
                            {
                                g.Emit( OpCodes.Ldstr, (string)d );
                            }
                            else if( dT.Equals( typeof( double ) ) )
                            {
                                g.Emit( OpCodes.Ldc_R8, (double)d );
                            }
                            else if( dT.Equals( typeof( float ) ) )
                            {
                                g.Emit( OpCodes.Ldc_R4, (float)d );
                            }
                        }
                    }
                    else
                    {
                        g.LdArg( mP.Position + 1 );
                    }
                    ++i;
                }
            }

            static bool IsValidDefaultValue( ParameterInfo p )
            {
                if( !p.HasDefaultValue ) return false;
                object d =  p.DefaultValue;
                if( d == null ) return true;
                Type dT = d.GetType();
                if( dT.Equals( typeof( int )  )|| dT.Equals( typeof( Int16 ) ) || dT.Equals( typeof( sbyte ) ) ) return true;
                if( dT.Equals( typeof( string ) ) ) return true;
                if( dT.Equals( typeof( double ) ) ) return true;
                if( dT.Equals( typeof( float ) ) ) return true;
                return false;
            }

            bool TrySetCandidate( ParameterInfo toMatch, IEnumerable<MethodParameter> candidates )
            {
                var c = candidates.ToList();
                if( c.Count == 1 )
                {
                    var only = c[0];
                    SetMatch( toMatch.Position, only );
                    if( only.Parameter.Name != toMatch.Name )
                    {
                        if( _warnings.Length > 0 ) _warnings.AppendLine();
                        _warnings.AppendFormat( "Parameter {0} has been mapped to method parameter {1} because it was the only candidate in terms of Type. Both parameters SHOULD use the same name.", toMatch.Name, only.Parameter.Name );
                    }
                }
                else
                {
                    var byName = c.FirstOrDefault( mp => mp.Parameter.Name == toMatch.Name );
                    if( byName != null ) SetMatch( toMatch.Position, byName );
                    else return false;
                }
                return true;
            }

            void SetMatch( int iParameter, MethodParameter methodParam )
            {
                _mappedParameters[iParameter] = methodParam.Parameter;
                methodParam.IdxTarget = iParameter;
            }
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

        bool CheckParameter( ParameterInfo mP, SqlExprParameter p, IActivityMonitor monitor )
        {
            int nbError = CheckParameterDirection( mP, p, monitor );
            //TODO: Check .Net type against: p.Variable.TypeDecl.ActualType.DbType 
            return nbError == 0;
        }

        private static int CheckParameterDirection( ParameterInfo mP, SqlExprParameter p, IActivityMonitor monitor )
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
                    monitor.Warn().Send( "Sql parameter '{0}' is an /*input*/output parameter. The method '{1}' should use 'ref' to retreive the new value after the call.", p.Variable.Identifier.Name, mP.Member.Name );
                }
                else if( sqlIsOutput )
                {
                    monitor.Error().Send( "Sql parameter '{0}' is an output parameter. The method '{1}' must use 'out' for the parameter (you can also simply remove the method's parameter the output value can be ignored).", p.Variable.Identifier.Name, mP.Member.Name );
                    ++nbError;
                }
            }
            return nbError;
        }

    }

}
