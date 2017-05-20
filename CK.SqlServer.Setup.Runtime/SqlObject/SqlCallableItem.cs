using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;
using System.Text;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Base class for item type "Function": <see cref="SqlFunctionInlineTableItem"/>, <see cref="SqlFunctionScalarItem"/>, 
    /// <see cref="SqlFunctionTableItem"/>, and "Procedure": <see cref="SqlProcedureItem"/>.
    /// </summary>
    /// <typeparam name="T">Type of the callable.</typeparam>
    public class SqlCallableItem<T> : SqlObjectItem, ISqlCallableItem where T : ISqlServerCallableObject
    {
        const string _builderTypeName = "CK._g.CreatorForSqlCommand";

        /// <summary>
        /// Initializes a new <see cref="SqlObjectItem"/>.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        /// <param name="itemType">Item type ("Function" or "Procedure").</param>
        /// <param name="parsed">The parsed callable object.</param>
        public SqlCallableItem( SqlContextLocName name, string itemType, T procOrFunc )
            : base( name, itemType, procOrFunc )
        {
        }

        /// <summary>
        /// Gets or sets the sql object. 
        /// </summary>
        public new T SqlObject
        {
            get { return (T)base.SqlObject; }
            set { base.SqlObject = value; }
        }

        public new SqlCallableItem<T> TransformTarget => (SqlCallableItem<T>)base.TransformTarget;

        ISqlServerCallableObject ISqlCallableItem.CallableObject => SqlObject;

        MethodInfo ISqlCallableItem.AssumeCommandBuilder( IActivityMonitor monitor, IDynamicAssembly dynamicAssembly )
        {
            if( SqlObject == null ) return null;
            TypeBuilder tB = (TypeBuilder)dynamicAssembly.Memory[_builderTypeName];
            if( tB == null )
            {
                tB = dynamicAssembly.ModuleBuilder.DefineType( _builderTypeName, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.NotPublic );
                dynamicAssembly.Memory.Add( _builderTypeName, tB );
                dynamicAssembly.PushFinalAction( FinalizeSqlCreator );
            }
            string methodKey = _builderTypeName + ':' + FullName;
            MethodInfo m = (MethodInfo)dynamicAssembly.Memory[methodKey];
            if( m == null && m != MCommandGetParameters )
            {
                // Adds a fake key to avoid multiple attempts.
                dynamicAssembly.Memory.Add( methodKey, MCommandGetParameters );
                using( monitor.OpenTrace().Send( "Low level SqlCommand create method for: '{0}'.", SqlObject.ToStringSignature( true ) ) )
                {
                    try
                    {
                        m = GenerateCreateSqlCommand( tB, FullName, SqlObject );
                        dynamicAssembly.Memory[methodKey] = m;
                        foreach( var p in SqlObject.Parameters )
                        {
                            if( p.IsPureOutput && p.DefaultValue != null )
                            {
                                monitor.Warn().Send( "Sql parameter '{0}' is an output parameter but has a default value: if it is used as an input parameter it should be marked as /*input*/output.", p.Name );
                            }
                        }
                    }
                    catch( Exception ex )
                    {
                        monitor.Error().Send( ex );
                    }
                }
            }
            return m;
        }

        static void FinalizeSqlCreator( IDynamicAssembly dynamicAssembly )
        {
            TypeBuilder tB = (TypeBuilder)dynamicAssembly.Memory[_builderTypeName];
            tB.CreateTypeInfo();
        }

        private static MethodBuilder GenerateCreateSqlCommand( TypeBuilder tB, string methodName, ISqlServerCallableObject sqlObject )
        {
            MethodBuilder mB = tB.DefineMethod( methodName, MethodAttributes.Assembly | MethodAttributes.Static, TypeCommand, Type.EmptyTypes );

            ILGenerator g = mB.GetILGenerator();

            LocalBuilder locCmd = g.DeclareLocal( TypeCommand );
            LocalBuilder locParams = g.DeclareLocal( TypeParameterCollection );
            LocalBuilder locOneParam = g.DeclareLocal( TypeParameter );

            g.Emit( OpCodes.Ldstr, sqlObject.SchemaName );
            g.Emit( OpCodes.Newobj, TypeCommand.GetConstructor( new Type[] { typeof( string ) } ) );
            g.StLoc( locCmd );

            g.LdLoc( locCmd );
            g.LdInt32( (int)CommandType.StoredProcedure );
            g.Emit( OpCodes.Callvirt, MCommandSetCommandType );
            g.LdLoc( locCmd );
            g.Emit( OpCodes.Callvirt, MCommandGetParameters );
            g.StLoc( locParams );

            ISqlServerFunctionScalar func = sqlObject as ISqlServerFunctionScalar;
            if( func != null )
            {
                g.LdLoc( locParams );
                GenerateCreateSqlParameter( g, new SqlParameterReturnedValue( func.ReturnType ) );
                g.Emit( OpCodes.Dup );
                g.LdInt32( (int)ParameterDirection.ReturnValue );
                g.Emit( OpCodes.Callvirt, MParameterSetDirection );
                g.Emit( OpCodes.Callvirt, MParameterCollectionAddParameter );
                // SqlParameterCollection.Add returns the added parameter.
                g.Emit( OpCodes.Pop );
            }

            foreach( ISqlServerParameter p in sqlObject.Parameters )
            {
                g.LdLoc( locParams );
                GenerateCreateSqlParameter( g, p );
                if( p.IsOutput )
                {
                    g.Emit( OpCodes.Dup );
                    ParameterDirection dir = p.IsInputOutput ? ParameterDirection.InputOutput : ParameterDirection.Output;
                    g.LdInt32( (int)dir );
                    g.Emit( OpCodes.Callvirt, MParameterSetDirection );
                }
                g.Emit( OpCodes.Callvirt, MParameterCollectionAddParameter );
                g.Emit( OpCodes.Pop );
            }
            g.LdLoc( locCmd );
            g.Emit( OpCodes.Ret );
            return mB;
        }

        private static void GenerateCreateSqlParameter( ILGenerator g, ISqlServerParameter p )
        {
            g.Emit( OpCodes.Ldstr, p.Name );
            g.LdInt32( (int)p.SqlType.DbType );
            int size = p.SqlType.SyntaxSize;
            if( size != 0 && size != -2 )
            {
                g.LdInt32( size );
                g.Emit( OpCodes.Newobj, SqlParameterCtor3 );
            }
            else
            {
                g.Emit( OpCodes.Newobj, SqlParameterCtor2 );
            }
            var precision = p.SqlType.SyntaxPrecision;
            if( precision != 0 )
            {
                g.Emit( OpCodes.Dup );
                g.LdInt32( precision );
                g.Emit( OpCodes.Callvirt, MParameterSetPrecision );
                var scale = p.SqlType.SyntaxScale;
                if( scale != 0 )
                {
                    g.Emit( OpCodes.Dup );
                    g.LdInt32( scale );
                    g.Emit( OpCodes.Callvirt, MParameterSetScale );
                }
            }
        }
    }
}
