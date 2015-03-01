#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObject\SqlProcedureItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    public class SqlProcedureItem : SqlObjectItem
    {
        const string _builderTypeName = "CK.<CreatorForSqlCommand>";
        readonly SqlExprStStoredProc _storedProc;

        internal SqlProcedureItem( SqlObjectProtoItem p, SqlExprStStoredProc storedProc )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeProcedure );
            _storedProc = storedProc;
        }

        /// <summary>
        /// Gets whether the definition of this item is valid (its body is available).
        /// </summary>
        public bool IsValid { get { return _storedProc != null; } }

        /// <summary>
        /// Gets the original parsed stored procedure. 
        /// Can be null if an error occurred during parsing.
        /// </summary>
        public SqlExprStStoredProc OriginalStatement { get { return _storedProc; } }

        /// <summary>
        /// Gets or generates the method that creates the <see cref="SqlCommand"/> for this <see cref="SqlProcedureItem."/>
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="dynamicAssembly">Use the memory associated to the dynamic to share the static class that implements the creation methods
        /// and the PushFinalAction to actually create it.</param>
        /// <param name="module">A module builder.</param>
        /// <returns>The method info. Null if <see cref="IsValid"/> is false or if an error occurred while generating it.</returns>
        internal MethodInfo AssumeCommandBuilder( IActivityMonitor monitor, IDynamicAssembly dynamicAssembly, ModuleBuilder module )
        {
            if( _storedProc == null ) return null;
            TypeBuilder tB = (TypeBuilder)dynamicAssembly.Memory[_builderTypeName];
            if( tB == null )
            {
                tB = module.DefineType( _builderTypeName, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.NotPublic );
                dynamicAssembly.Memory.Add( _builderTypeName, tB );
                dynamicAssembly.PushFinalAction( FinalizeSqlCreator ); 
            }
            string methodKey = _builderTypeName + ':' + FullName;
            MethodInfo m = (MethodInfo)dynamicAssembly.Memory[methodKey];
            if( m == null && m != MCommandGetParameters )
            {
                // Adds a fake key to avoid multiple attempts.
                dynamicAssembly.Memory.Add( methodKey, MCommandGetParameters );
                try
                {
                    m = GenerateCreateSqlCommand( tB, FullName, SchemaName, _storedProc.Parameters );
                    dynamicAssembly.Memory[methodKey] = m;
                    monitor.Trace().Send( "Low level SqlCommand create method for: '{0}'.", _storedProc.ToStringSignature( true ) );
                }
                catch( Exception ex )
                {
                    monitor.Error().Send( ex, "While generating low level SqlCommand method creation for: '{0}'.", _storedProc.ToStringSignature( true ) );
                }
            }
            return m;
        }

        static void FinalizeSqlCreator( IDynamicAssembly dynamicAssembly )
        {
            TypeBuilder tB = (TypeBuilder)dynamicAssembly.Memory[_builderTypeName];
            tB.CreateType();
        }

        private static MethodBuilder GenerateCreateSqlCommand( TypeBuilder tB, string methodName, string spSchemaName, SqlExprParameterList sqlParameters )
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

            foreach( SqlExprParameter p in sqlParameters )
            {
                g.Emit( OpCodes.Ldstr, p.Variable.Identifier.Name );
                g.LdInt32( (int)p.Variable.TypeDecl.ActualType.DbType );
                int size = p.Variable.TypeDecl.ActualType.SyntaxSize;
                if( size != 0 && size != -2 )
                {
                    g.LdInt32( size );
                    g.Emit( OpCodes.Newobj, SqlParameterCtor3 );
                }
                else
                {
                    g.Emit( OpCodes.Newobj, SqlParameterCtor2 );
                }
                g.StLoc( locOneParam );

                if( p.IsOutput )
                {
                    g.LdLoc( locOneParam );
                    ParameterDirection dir = p.IsInputOutput ? ParameterDirection.InputOutput : ParameterDirection.Output;
                    g.LdInt32( (int)dir );
                    g.Emit( OpCodes.Callvirt, MParameterSetDirection );
                }
                g.LdLoc( locParams );
                g.LdLoc( locOneParam );
                g.Emit( OpCodes.Callvirt, MParameterCollectionAddParameter );
                g.Emit( OpCodes.Pop );
            }
            g.LdLoc( locCmd );
            g.Emit( OpCodes.Ret );
            return mB;
        }

    }
}
