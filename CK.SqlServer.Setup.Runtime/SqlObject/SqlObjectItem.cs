#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObject\SqlObjectItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{

    public class SqlObjectItem : SetupObjectItemV
    {
        internal readonly static Type TypeCommand = typeof( SqlCommand );
        internal readonly static Type TypeConnection = typeof( SqlConnection );
        internal readonly static Type TypeTransaction = typeof( SqlTransaction );
        internal readonly static Type TypeParameterCollection = typeof( SqlParameterCollection );
        internal readonly static Type TypeParameter = typeof( SqlParameter );
        internal readonly static ConstructorInfo SqlParameterCtor2 = TypeParameter.GetConstructor( new Type[] { typeof( string ), typeof( SqlDbType ) } );
        internal readonly static ConstructorInfo SqlParameterCtor3 = TypeParameter.GetConstructor( new Type[] { typeof( string ), typeof( SqlDbType ), typeof( Int32 ) } );

        internal readonly static MethodInfo MTransactionGetConnection = TypeTransaction.GetProperty( "Connection", SqlObjectItem.TypeConnection ).GetGetMethod();

        internal readonly static MethodInfo MCommandSetConnection = TypeCommand.GetProperty( "Connection", SqlObjectItem.TypeConnection ).GetSetMethod();
        internal readonly static MethodInfo MCommandSetTransaction = TypeCommand.GetProperty( "Transaction", SqlObjectItem.TypeTransaction ).GetSetMethod();

        internal readonly static MethodInfo MCommandSetCommandType = TypeCommand.GetProperty( "CommandType" ).GetSetMethod();
        internal readonly static MethodInfo MCommandGetParameters = TypeCommand.GetProperty( "Parameters", SqlObjectItem.TypeParameterCollection ).GetGetMethod();
        internal readonly static MethodInfo MParameterCollectionAddParameter = TypeParameterCollection.GetMethod( "Add", new Type[] { TypeParameter } );
        internal readonly static MethodInfo MParameterCollectionRemoveAtParameter = TypeParameterCollection.GetMethod( "RemoveAt", new Type[] { typeof( Int32 ) } );
        internal readonly static MethodInfo MParameterCollectionGetParameter = TypeParameterCollection.GetProperty( "Item", new Type[] { typeof( Int32 ) } ).GetGetMethod();

        internal readonly static MethodInfo MParameterSetDirection = TypeParameter.GetProperty( "Direction" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetValue = TypeParameter.GetProperty( "Value" ).GetSetMethod();
        internal readonly static MethodInfo MParameterGetValue = TypeParameter.GetProperty( "Value" ).GetGetMethod();
        internal readonly static FieldInfo FieldDBNullValue = typeof( DBNull ).GetField( "Value", BindingFlags.Public | BindingFlags.Static );


        SqlObjectProtoItem _protoItem;
        string _physicalDB;
        bool? _missingDependencyIsError;
        string _header;

        internal SqlObjectItem( SqlObjectProtoItem p )
            : base( p )
        {
            _protoItem = p;
            // Keeps the physical database name if the proto item defines it.
            // It is currently unused.
            _physicalDB = p.PhysicalDatabaseName;
            _header = _protoItem.Header;
            _missingDependencyIsError = p.MissingDependencyIsError;
        }

        /// <summary>
        /// Gets or sets the object that replaces this object.
        /// </summary>
        public new SqlObjectItem ReplacedBy
        {
            get { return (SqlObjectItem)base.ReplacedBy; }
            set { base.ReplacedBy = value; }
        }

        /// <summary>
        /// Gets the object that is replaced by this one.
        /// </summary>
        public new SqlObjectItem Replaces
        {
            get { return (SqlObjectItem)base.Replaces; }
        }

        public new SqlContextLocName ContextLocName
        {
            get { return (SqlContextLocName)base.ContextLocName; }
        }


        /// <summary>
        /// Gets or sets whether when installing, the informational message 'The module 'X' depends 
        /// on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must be logged as a <see cref="LogLevel.Error"/>. When false, this is a <see cref="LogLevel.Info"/>.
        /// Sets first by MissingDependencyIsError in text, otherwise an attribute (that should default to true should be applied).
        /// When not set, it is considered to be true.
        /// </summary>
        public bool? MissingDependencyIsError
        {
            get { return _missingDependencyIsError; }
            set { _missingDependencyIsError = value; }
        }

        /// <summary>
        /// Gets or sets the header part of this object. Never null (normalized to String.Empty).
        /// </summary>
        public string Header
        {
            get { return _header; }
            set { _header = value ?? String.Empty; }
        }

        protected override object StartDependencySort()
        { 
            return typeof(SqlObjectSetupDriver);
        }

        /// <summary>
        /// Writes the drop instruction.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteDrop( TextWriter b )
        {
            b.Write( "if OBJECT_ID('" );
            b.Write( ContextLocName.Name );
            b.Write( "') is not null drop " );
            b.Write( ItemType );
            b.Write( ' ' );
            b.Write( ContextLocName.Name );
            b.WriteLine( ';' );
        }

        /// <summary>
        /// Writes the whole object.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteCreate( TextWriter b )
        {
            if( _protoItem != null ) b.WriteLine( _header );
            b.Write( "create " );
            b.Write( ItemType );
            b.Write( ' ' );
            b.Write( ContextLocName.Name );
            if( ReplacedBy != null )
            {
                b.WriteLine();
                b.WriteLine( "-- This {0} is replaced.", ItemType );
                // For fonctions we must consider the actual kind of function.
                // I'll do this later.
                if( ItemType == SqlObjectProtoItem.TypeProcedure )
                {
                    b.WriteLine( "as begin" );
                    b.WriteLine( "  return 0;" );
                    b.WriteLine( "end" );
                    return;
                }
            }
            if( _protoItem != null ) b.WriteLine( _protoItem.TextAfterName );
        }

    }
}
