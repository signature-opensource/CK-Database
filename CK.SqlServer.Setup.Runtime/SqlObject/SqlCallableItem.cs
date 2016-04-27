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
    internal interface ISqlCallableItem : ISetupItem
    {
        /// <summary>
        /// Gets whether the definition of this item is valid (its body is available).
        /// </summary>
        bool IsValid { get; }

        ISqlServerCallableObject CallableObject { get; }

        /// <summary>
        /// Gets or generates the method that creates the <see cref="SqlCommand"/> for this callable item.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="dynamicAssembly">Use the memory associated to the dynamic to share the static class that implements the creation methods
        /// and the PushFinalAction to actually create it.</param>
        /// <returns>The method info. Null if <see cref="IsValid"/> is false or if an error occurred while generating it.</returns>
        MethodInfo AssumeCommandBuilder( IActivityMonitor monitor, IDynamicAssembly dynamicAssembly );
    }

    public class SqlCallableItem<T> : SqlObjectItem, ISqlCallableItem where T : ISqlServerCallableObject
    {
        const string _builderTypeName = "CK.<CreatorForSqlCommand>";
        readonly T _originalCallable;
        T _finalCallable;

        internal SqlCallableItem( SqlObjectProtoItem p, T procOrFunc )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeProcedure || p.ItemType == SqlObjectProtoItem.TypeFunction );
            if( (_finalCallable = _originalCallable = procOrFunc) != null )
            {
                if( p.ContextLocName.Schema != null && p.ContextLocName.Schema != procOrFunc.SchemaName )
                {
                    _finalCallable = (T)procOrFunc.SetSchema( p.ContextLocName.Schema );
                }
            }
        }

        /// <summary>
        /// Gets whether the definition of this item is valid (its body is available).
        /// </summary>
        public bool IsValid => _originalCallable != null;

        /// <summary>
        /// Gets the original parsed object. 
        /// Can be null if an error occurred during parsing.
        /// </summary>
        public T OriginalStatement => _originalCallable;

        ISqlServerCallableObject ISqlCallableItem.CallableObject => _finalCallable;

        /// <summary>
        /// Gets or sets a replacement of the <see cref="OriginalStatement"/>.
        /// This is initialized with <see cref="OriginalStatement"/> but can be changed.
        /// </summary>
        public T FinalStatement
        {
            get { return _finalCallable; }
            set { _finalCallable = value; }
        }

        protected override void DoWriteCreate( StringBuilder b )
        {
            ISqlServerCallableObject s = FinalStatement;
            if( s != null )
            {
                if( s.IsAlterKeyword ) s = (ISqlServerCallableObject)s.ToggleAlterKeyword();
                s.Write( b );
            }
        }

        MethodInfo ISqlCallableItem.AssumeCommandBuilder( IActivityMonitor monitor, IDynamicAssembly dynamicAssembly )
        {
            if( _finalCallable == null ) return null;
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
                using( monitor.OpenTrace().Send( "Low level SqlCommand create method for: '{0}'.", _finalCallable.ToStringSignature( true ) ) )
                {
                    try
                    {
                        m = GenerateCreateSqlCommand( tB, FullName, ContextLocName.Name, _finalCallable );
                        dynamicAssembly.Memory[methodKey] = m;
                        foreach( var p in _finalCallable.Parameters )
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
            tB.CreateType();
        }

        private static MethodBuilder GenerateCreateSqlCommand( TypeBuilder tB, string methodName, string spSchemaName, ISqlServerCallableObject sqlObject )
        {
            MethodBuilder mB = tB.DefineMethod( methodName, MethodAttributes.Assembly | MethodAttributes.Static, TypeCommand, Type.EmptyTypes );

            ILGenerator g = mB.GetILGenerator();

            LocalBuilder locCmd = g.DeclareLocal( TypeCommand );
            LocalBuilder locParams = g.DeclareLocal( TypeParameterCollection );
            LocalBuilder locOneParam = g.DeclareLocal( TypeParameter );

            g.Emit( OpCodes.Ldstr, spSchemaName );
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
