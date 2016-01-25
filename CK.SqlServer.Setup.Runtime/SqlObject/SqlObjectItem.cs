using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using CK.Core;
using CK.Setup;
using System.Text;

namespace CK.SqlServer.Setup
{

    public class SqlObjectItem : SetupObjectItemV
    {
        internal readonly static Type TypeCommand = typeof( SqlCommand );
        internal readonly static Type TypeConnection = typeof( SqlConnection );
        internal readonly static Type TypeTransaction = typeof( SqlTransaction );
        internal readonly static Type TypeParameterCollection = typeof( SqlParameterCollection );
        internal readonly static Type TypeParameter = typeof( SqlParameter );
        internal readonly static Type TypeISqlParameterContext = typeof( ISqlParameterContext );
        internal readonly static Type TypeSqlPackageBase = typeof( SqlPackageBase );
        internal readonly static Type TypeSqlDatabase = typeof( SqlDatabase );

        internal readonly static MethodInfo MGetDatabase = TypeSqlPackageBase.GetProperty( "Database", SqlObjectItem.TypeSqlDatabase ).GetGetMethod();
        internal readonly static MethodInfo MDatabaseGetConnectionString = TypeSqlDatabase.GetProperty( "ConnectionString", typeof( string ) ).GetGetMethod();

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
        internal readonly static MethodInfo MParameterSetPrecision = TypeParameter.GetProperty( "Precision" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetScale = TypeParameter.GetProperty( "Scale" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetValue = TypeParameter.GetProperty( "Value" ).GetSetMethod();
        internal readonly static MethodInfo MParameterGetValue = TypeParameter.GetProperty( "Value" ).GetGetMethod();
        internal readonly static FieldInfo FieldDBNullValue = typeof( DBNull ).GetField( "Value", BindingFlags.Public | BindingFlags.Static );

        internal readonly static MethodInfo MExecutorCallNonQuery = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQuery" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsync = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsync" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsyncCancellable = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsyncCancellable" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsyncTyped = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsyncTyped" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsyncTypedCancellable = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsyncTypedCancellable" );

        internal readonly static ConstructorInfo CtorDecimalBits = typeof( Decimal ).GetConstructor( new Type[]{ typeof(int[]) } );

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
            _header = p.Header;
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
        /// <param name="b">The target <see cref="StringBuilder"/>.</param>
        public void WriteDrop( StringBuilder b )
        {
            b.Append( "if OBJECT_ID('" )
                .Append( ContextLocName.Name )
                .Append( "') is not null drop " )
                .Append( ItemType )
                .Append( ' ' )
                .Append( ContextLocName.Name )
                .Append( ';' )
                .AppendLine();
        }

        /// <summary>
        /// Writes the whole object.
        /// </summary>
        /// <param name="b">The target <see cref="StringBuilder"/>.</param>
        public void WriteCreate( StringBuilder b )
        {
            if( ReplacedBy != null )
            {
                b.AppendFormat( "-- This {0} is replaced.", ItemType ).AppendLine();
                b.AppendFormat( "-- create {0} {1}", ItemType, ContextLocName.Name ).AppendLine();
            }
            else DoWriteCreate( b );
        }

        protected virtual void DoWriteCreate( StringBuilder b )
        {
            b.Append( _header ).AppendLine();
            b.Append( "create " )
                .Append( ItemType )
                .Append( ' ' )
                .Append( ContextLocName.Name )
                .Append( _protoItem.TextAfterName )
                .AppendLine();
        }

    }
}
