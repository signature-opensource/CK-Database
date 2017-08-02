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
using CK.CodeGen;

namespace CK.SqlServer.Setup
{

    public partial class SqlCallableAttributeImpl : SqlBaseItemMethodAttributeImplBase
    {
        public SqlCallableAttributeImpl( SqlCallableAttributeBase a )
            : base( a, a.ObjectType )
        {
        }

        protected new SqlCallableAttributeBase Attribute => (SqlCallableAttributeBase)base.Attribute;

        protected override bool DoImplement( IActivityMonitor monitor, MethodInfo m, SqlBaseItem sqlItem, IDynamicAssembly dynamicAssembly, System.Reflection.Emit.TypeBuilder tB, bool isVirtual )
        {
            ISqlCallableItem item = sqlItem as ISqlCallableItem;
            if( item == null )
            {
                monitor.Fatal().Send( $"The item '{item.FullName}' must be a ISqlCallableItem object to be able to generate call implementation." );
                return false;
            }
            MethodInfo mCreateCommand = item.AssumeCommandBuilder( monitor, dynamicAssembly );
            if( mCreateCommand == null )
            {
                monitor.Error().Send( $"Invalid low level SqlCommand creation method for '{item.FullName}'." );
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
                            monitor.Error().Send( "When a Wrapper is returned, ExecuteNonQuery must not be specified.", m.DeclaringType.FullName, m.Name );
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
                        monitor.Error().Send( "Method '{0}.{1}' must return a SqlCommand -OR- a type that has at least one constructor with a non optional SqlCommand (among other parameters) -OR- accepts a SqlCommand by reference as its first argument -OR- use ExecuteNonQuery mode.", m.DeclaringType.FullName, m.Name );
                        return false;
                    }
                }
            }
            return GenerateCreateSqlCommand( dynamicAssembly, gType, monitor, mCreateCommand, item.CallableObject, m, mParameters, tB, isVirtual, hasRefSqlCommand );
        }

        /// <summary>
        /// Tests whether a type has a corresponding <see cref="SqlDbType"/>. 
        /// It is all the types that are mapped by <see cref="FromSqlDbTypeToNetType"/> except <see cref="object"/> 
        /// plus <see cref="char"/> and enum (provided their underlying type is mapped) and 
        /// any <see cref="Nullable{T}"/> where T is mapped.
        /// </summary>
        /// <param name="t">Type to challenge.</param>
        /// <returns>True if this type can be mapped to a basic Sql type.</returns>
        static public bool IsNetTypeMapped(Type t)
        {
            if (t == null) throw new ArgumentNullException();
            if (t == typeof(object)) return false;
            var nT = Nullable.GetUnderlyingType(t);
            if (nT != null) t = nT;
            if (t == typeof(char)) return true;
            if (t.GetTypeInfo().IsEnum) t = t.GetTypeInfo().GetEnumUnderlyingType();
            return SqlHelper.HasDirectMapping(t);
        }

    }
}
