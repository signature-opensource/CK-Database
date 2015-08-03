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
        readonly ISqlServerCallableObject _storedProc;

        internal SqlProcedureItem( SqlObjectProtoItem p, ISqlServerCallableObject storedProc )
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
        public ISqlServerCallableObject OriginalStatement { get { return _storedProc; } }

        /// <summary>
        /// Gets or generates the method that creates the <see cref="SqlCommand"/> for this <see cref="SqlProcedureItem."/>
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="dynamicAssembly">Use the memory associated to the dynamic to share the static class that implements the creation methods
        /// and the PushFinalAction to actually create it.</param>
        /// <returns>The method info. Null if <see cref="IsValid"/> is false or if an error occurred while generating it.</returns>
        internal MethodInfo AssumeCommandBuilder( IActivityMonitor monitor, IDynamicAssembly dynamicAssembly )
        {
            if( _storedProc == null ) return null;
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
                using( monitor.OpenTrace().Send( "Low level SqlCommand create method for: '{0}'.", _storedProc.ToStringSignature( true ) ) )
                {
                    try
                    {
                        m = GenerateCreateSqlCommand( tB, FullName, ContextLocName.Name, _storedProc.Parameters );
                        dynamicAssembly.Memory[methodKey] = m;
                        foreach( var p in _storedProc.Parameters )
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

        private static MethodBuilder GenerateCreateSqlCommand( TypeBuilder tB, string methodName, string spSchemaName, ISqlServerParameterList sqlParameters )
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

            foreach( ISqlServerParameter p in sqlParameters )
            {
                g.LdLoc( locParams );
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
                if( p.IsOutput )
                {
                    g.Emit( OpCodes.Dup );
                    ParameterDirection dir = p.IsInputOutput ? ParameterDirection.InputOutput : ParameterDirection.Output;
                    g.LdInt32( (int)dir );
                    g.Emit( OpCodes.Callvirt, MParameterSetDirection );
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
                g.Emit( OpCodes.Callvirt, MParameterCollectionAddParameter );
                g.Emit( OpCodes.Pop );
            }
            g.LdLoc( locCmd );
            g.Emit( OpCodes.Ret );
            return mB;
        }

    }
}
