using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using CK.Core;
using CK.Setup;
using System.Text;
using CK.SqlServer.Parser;
using System.Text.RegularExpressions;
using System.Linq;
using CK.Text;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Base class for <see cref="SqlViewItem"/> and <see cref="SqlCallableItem{T}"/>.
    /// </summary>
    public abstract class SqlObjectItem : SqlBaseItem
    {
        bool? _missingDependencyIsError;


        internal SqlObjectItem( SqlContextLocName name, string itemType, ISqlServerObject parsed )
            : base( name, itemType, parsed )
        {
            ExplicitRequiresMustBeTransformed = parsed.Options.SchemaBinding;
            SetDriverType( typeof( SqlObjectItemDriver ) );
        }

        /// <summary>
        /// Gets or sets the <see cref="ISqlServerObject"/> (specialized <see cref="ISqlServerParsedText"/> with
        /// schema and name). 
        /// </summary>
        public new ISqlServerObject SqlObject
        {
            get { return (ISqlServerObject)base.SqlObject; }
            set { base.SqlObject = value; }
        }

        public new SqlObjectItem TransformTarget => (SqlObjectItem)base.TransformTarget;

        /// <summary>
        /// Gets or sets whether when installing, the informational message 'The module 'X' depends 
        /// on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must be logged as a <see cref="LogLevel.Error"/>. When false, this is a <see cref="LogLevel.Info"/>.
        /// Sets by MissingDependencyIsError in SetupConfig text.
        /// When not set, it is considered to be true.
        /// </summary>
        public bool? MissingDependencyIsError
        {
            get { return _missingDependencyIsError; }
            set { _missingDependencyIsError = value; }
        }

        /// <summary>
        /// Writes the drop instruction.
        /// </summary>
        /// <param name="b">The target <see cref="StringBuilder"/>.</param>
        public void WriteDrop( StringBuilder b )
        {
            b.Append( "if OBJECT_ID('" )
                .Append( SqlObject.SchemaName )
                .Append( "') is not null drop " )
                .Append( ItemType )
                .Append( ' ' )
                .Append( SqlObject.SchemaName )
                .Append( ';' )
                .AppendLine();
        }

        /// <summary>
        /// Writes the whole object.
        /// </summary>
        /// <param name="b">The target <see cref="StringBuilder"/>.</param>
        public void WriteCreate( StringBuilder b )
        {
            var alterOrCreate = SqlObject as ISqlServerAlterOrCreateStatement;
            if( alterOrCreate != null )
            {
                if( alterOrCreate.IsAlterKeyword ) alterOrCreate = alterOrCreate.ToggleAlterKeyword();
                alterOrCreate.Write( b );
            }
            else SqlObject.Write( b );
        }

        protected override bool Initialize( IActivityMonitor monitor, IDependentItemContainer firstContainer, IDependentItemContainer packageItem )
        {
            return CheckSchemaAndObjectName( monitor ) && base.Initialize( monitor, firstContainer, packageItem );
        }

        bool CheckSchemaAndObjectName( IActivityMonitor monitor )
        {
            if( ContextLocName.ObjectName != SqlObject.Name )
            {
                monitor.Error().Send( $"Definition of '{ContextLocName.Name}' instead of '{SqlObject.Name}'. Names must match." );
                return false;
            }
            if( SqlObject.Schema == null )
            {
                if( !string.IsNullOrWhiteSpace( ContextLocName.Schema ) )
                {
                    SqlObject = SqlObject.SetSchema( ContextLocName.Schema );
                    monitor.Trace().Send( $"{ItemType} '{SqlObject.Name}' does not specify a schema: it will use '{ContextLocName.Schema}' schema." );
                }
            }
            else if( SqlObject.Schema != ContextLocName.Schema )
            {
                monitor.Error().Send( $"{ItemType} is defined in the schema '{SqlObject.Schema}' instead of '{ContextLocName.Schema}'." );
                return false;
            }
            return true;
        }

        #region static reflection objects
        internal readonly static Type TypeCommand = typeof( SqlCommand );
        internal readonly static Type TypeConnection = typeof( SqlConnection );
        internal readonly static Type TypeTransaction = typeof( SqlTransaction );
        internal readonly static Type TypeParameterCollection = typeof( SqlParameterCollection );
        internal readonly static Type TypeParameter = typeof( SqlParameter );
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
        internal readonly static MethodInfo MParameterCollectionGetParameter = TypeParameterCollection.GetProperty( "Item", TypeParameter, new Type[] { typeof( Int32 ) } ).GetGetMethod();

        internal readonly static MethodInfo MParameterSetDirection = TypeParameter.GetProperty( "Direction" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetPrecision = TypeParameter.GetProperty( "Precision" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetScale = TypeParameter.GetProperty( "Scale" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetValue = TypeParameter.GetProperty( "Value" ).GetSetMethod();
        internal readonly static MethodInfo MParameterGetValue = TypeParameter.GetProperty( "Value" ).GetGetMethod();
        internal readonly static FieldInfo FieldDBNullValue = typeof( DBNull ).GetField( "Value", BindingFlags.Public | BindingFlags.Static );

        internal readonly static MethodInfo MExecutorCallNonQuery = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQuery" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsync = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsync" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsyncTyped = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsyncTyped" );

        internal readonly static ConstructorInfo CtorDecimalBits = typeof( decimal ).GetConstructor( new Type[] { typeof( int[] ) } );
        #endregion

    }
}
