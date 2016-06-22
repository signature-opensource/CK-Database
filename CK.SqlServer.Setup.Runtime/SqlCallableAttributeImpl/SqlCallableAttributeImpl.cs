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
using CK.Reflection;

namespace CK.SqlServer.Setup
{

    public partial class SqlCallableAttributeImpl : SqlObjectItemMethodAttributeImplBase
    {
        public SqlCallableAttributeImpl( SqlCallableAttributeBase a )
            : base( a, a.ObjectType )
        {
        }

        protected new SqlCallableAttributeBase Attribute => (SqlCallableAttributeBase)base.Attribute;

        protected override bool DoImplement( IActivityMonitor monitor, MethodInfo m, SqlObjectItem objectItem, IDynamicAssembly dynamicAssembly, TypeBuilder tB, bool isVirtual )
        {
            ISqlCallableItem item = objectItem as ISqlCallableItem;
            MethodInfo mCreateCommand = item != null ? item.AssumeCommandBuilder( monitor, dynamicAssembly ) : null;
            if( mCreateCommand == null )
            {
                monitor.Error().Send( "Invalid low level SqlCommand creation method for '{0}'.", item.FullName );
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
                        monitor.Error().Send( "When a SqlCommand is returned, ExecuteCall must not be specified.", m.DeclaringType.FullName, m.Name );
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
                            monitor.Error().Send( "When a Wrapper is returned, ExecuteCall must not be specified.", m.DeclaringType.FullName, m.Name );
                            return false;
                        }
                        gType = GenerationType.ReturnWrapper;
                    }
                    else if( executeCall )
                    {
                        Debug.Assert( execType == ExecutionType.ExecuteNonQuery, "For the moment only ExecuteNonQuery is supported. Other modes will lead to new CallXXX generation types." );
                        gType = GenerationType.ExecuteNonQuery;
                    }
                    else
                    {
                        monitor.Error().Send( "Ctor '{0}.{1}' must return a SqlCommand -OR- a type that has at least one constructor with a non optional SqlCommand (among other parameters) -OR- accepts a SqlCommand by reference as its first argument -OR- use auto execution and exploit the execute return (can be void).", m.DeclaringType.FullName, m.Name );
                        return false;
                    }
                }
            }
            return GenerateCreateSqlCommand( dynamicAssembly, gType, monitor, mCreateCommand, item.CallableObject, m, mParameters, tB, isVirtual, hasRefSqlCommand );
        }

    }
}
