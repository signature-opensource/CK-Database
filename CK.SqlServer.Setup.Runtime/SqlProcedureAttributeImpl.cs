#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlProcedureAttributeImpl.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{

    public partial class SqlProcedureAttributeImpl : SqlObjectItemMethodAttributeImplBase
    {
        public SqlProcedureAttributeImpl( SqlProcedureAttribute a )
            : base( a, SqlObjectProtoItem.TypeProcedure )
        {
        }

        protected override bool DoImplement( IActivityMonitor monitor, MethodInfo m, SqlObjectItem objectItem, IDynamicAssembly dynamicAssembly, TypeBuilder tB, bool isVirtual )
        {
            SqlProcedureItem item = objectItem as SqlProcedureItem;
            MethodInfo mCreateCommand = item != null ? item.AssumeCommandBuilder( monitor, dynamicAssembly, (ModuleBuilder)tB.Module ) : null;
            if( mCreateCommand == null )
            {
                monitor.Error().Send( "Invalid low level SqlCommand creation method for '{0}'.", item.FullName );
                return false;
            }

            ParameterInfo[] mParameters = m.GetParameters();
            GenerationType gType;
            if( m.ReturnType == typeof( void )
                    && mParameters.Length >= 1
                    && mParameters[0].ParameterType.IsByRef
                    && !mParameters[0].IsOut
                    && mParameters[0].ParameterType.GetElementType() == SqlObjectItem.TypeCommand )
            {
                gType = GenerationType.ByRefSqlCommand;
            }
            else
            {
                if( m.ReturnType == SqlObjectItem.TypeCommand ) gType = GenerationType.ReturnSqlCommand;
                else
                {
                    if( !m.ReturnType.GetConstructors().Any( ctor => ctor.GetParameters().Any( p => p.ParameterType == SqlObjectItem.TypeCommand && !p.ParameterType.IsByRef && !p.HasDefaultValue ) ) )
                    {
                        monitor.Error().Send( "Ctor '{0}.{1}' must return a SqlCommand -OR- a type that has at least one constructor with a non optional SqlCommand (among other parameters) -OR- accepts a SqlCommand by reference as its first argument.", m.DeclaringType.FullName, m.Name );
                        return false;
                    }
                    gType = GenerationType.ReturnWrapper;
                }
            }
            SqlExprParameterList sqlParameters = item.OriginalStatement.Parameters;
            return GenerateCreateSqlCommand( gType, monitor, mCreateCommand, item.OriginalStatement.Name, sqlParameters, m, mParameters, tB, isVirtual );
        }

    }
}
