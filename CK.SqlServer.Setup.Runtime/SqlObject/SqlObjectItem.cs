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
    /// Specialized <see cref="SqlBaseItem"/>: its <see cref="SqlObject"/> is a <see cref="ISqlServerObject"/>.
    /// Base class for <see cref="SqlViewItem"/> and <see cref="SqlCallableItem{T}"/>.
    /// </summary>
    public abstract class SqlObjectItem : SqlBaseItem
    {
        /// <summary>
        /// Initializes a new <see cref="SqlObjectItem"/>.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        /// <param name="itemType">Item type ("Function", "Procedure", "View", etc.).</param>
        /// <param name="parsed">The parsed object.</param>
        protected SqlObjectItem( SqlContextLocName name, string itemType, ISqlServerObject parsed )
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

        /// <summary>
        /// Gets the transform target item if this item has associated <see cref="SqlBaseItem.Transformers">Transformers</see>.
        /// This object is created as a clone of this object by the first call 
        /// to this <see cref="SetupObjectItem.AddTransformer"/> method.
        /// </summary>
        public new SqlObjectItem TransformTarget => (SqlObjectItem)base.TransformTarget;

        /// <summary>
        /// Writes the drop instruction protected by a if object_id(...) is not null.
        /// </summary>
        /// <param name="b">The target <see cref="StringBuilder"/>.</param>
        public void WriteSafeDrop( StringBuilder b )
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
        /// Writes the creation or alteration of the object.
        /// </summary>
        /// <param name="b">The target <see cref="StringBuilder"/>.</param>
        /// <param name="alreadyExists">
        /// True if the object is known to exist (an alter should be emitted if possible).
        /// </param>
        public void WriteCreate( StringBuilder b, bool alreadyExists )
        {
            var alterOrCreate = SqlObject as ISqlServerAlterOrCreateStatement;
            if( alterOrCreate != null )
            {
                if( alreadyExists )
                {
                    if( alterOrCreate.StatementPrefix != CreateOrAlterStatementPrefix.Alter )
                    {
                        alterOrCreate = alterOrCreate.WithStatementPrefix( CreateOrAlterStatementPrefix.Alter );
                    }
                }
                else if( alterOrCreate.StatementPrefix != CreateOrAlterStatementPrefix.Create )
                {
                    alterOrCreate = alterOrCreate.WithStatementPrefix( CreateOrAlterStatementPrefix.Create );
                }
                alterOrCreate.Write( b );
            }
            else
            {
                if( alreadyExists ) WriteSafeDrop( b );
                SqlObject.Write( b );
            }
        }

        /// <summary>
        /// Override the base <see cref="SqlBaseItem.Initialize"/> method to
        /// call <see cref="CheckSchemaAndObjectName"/> before calling the base implementation.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="firstContainer">
        /// The first container that defined this object: it is different than the <paramref name="packageItem"/>
        /// if it is a replacement.
        /// On success, this will be the package of the item if the item does not specify a container.
        /// </param>
        /// <param name="packageItem">
        /// The package that defined the item.
        /// </param>
        /// <returns>True on success, false on error.</returns>
        protected override bool Initialize( IActivityMonitor monitor, IDependentItemContainer firstContainer, IDependentItemContainer packageItem )
        {
            return CheckSchemaAndObjectName( monitor ) && base.Initialize( monitor, firstContainer, packageItem );
        }

        /// <summary>
        /// Checks and corrects when possible <see cref="SqlObject"/>'s <see cref="ISqlServerObject.Schema"/>.
        /// </summary>
        /// <param name="monitor">The monitor used to raise errors or warnings.</param>
        /// <returns>True on success, false otherwise.</returns>
        protected bool CheckSchemaAndObjectName( IActivityMonitor monitor )
        {
            if( ContextLocName.ObjectName != SqlObject.Name )
            {
                monitor.Error( $"Definition of '{ContextLocName.Name}' instead of '{SqlObject.Name}'. Names must match." );
                return false;
            }
            if( SqlObject.Schema == null )
            {
                if( !string.IsNullOrWhiteSpace( ContextLocName.Schema ) )
                {
                    SqlObject = SqlObject.SetSchema( ContextLocName.Schema );
                    monitor.Trace( $"{ItemType} '{SqlObject.Name}' does not specify a schema: it will use '{ContextLocName.Schema}' schema." );
                }
            }
            else if( SqlObject.Schema != ContextLocName.Schema )
            {
                monitor.Error( $"{ItemType} is defined in the schema '{SqlObject.Schema}' instead of '{ContextLocName.Schema}'." );
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


        internal readonly static MethodInfo MCommandGetParameters = TypeCommand.GetProperty( "Parameters", SqlObjectItem.TypeParameterCollection ).GetGetMethod();

        internal readonly static MethodInfo MCommandSetConnection = TypeCommand.GetProperty( "Connection", SqlObjectItem.TypeConnection ).GetSetMethod();
        internal readonly static MethodInfo MCommandSetTransaction = TypeCommand.GetProperty( "Transaction", SqlObjectItem.TypeTransaction ).GetSetMethod();
        internal readonly static MethodInfo MCommandSetCommandType = TypeCommand.GetProperty( "CommandType" ).GetSetMethod();
        internal readonly static MethodInfo MParameterCollectionAddParameter = TypeParameterCollection.GetMethod( "Add", new Type[] { TypeParameter } );
        internal readonly static MethodInfo MParameterCollectionGetParameter = TypeParameterCollection.GetProperty( "Item", TypeParameter, new Type[] { typeof( Int32 ) } ).GetGetMethod();
        internal readonly static MethodInfo MParameterSetDirection = TypeParameter.GetProperty( "Direction" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetSqlDbType = TypeParameter.GetProperty("SqlDbType").GetSetMethod();
        internal readonly static MethodInfo MParameterSetPrecision = TypeParameter.GetProperty( "Precision" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetScale = TypeParameter.GetProperty( "Scale" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetValue = TypeParameter.GetProperty( "Value" ).GetSetMethod();
        internal readonly static MethodInfo MParameterGetValue = TypeParameter.GetProperty( "Value" ).GetGetMethod();
        internal readonly static FieldInfo FieldDBNullValue = typeof( DBNull ).GetField( "Value", BindingFlags.Public | BindingFlags.Static );
        
#if NET461
        internal readonly static MethodInfo MParameterSetUdtTypeName = TypeParameter.GetProperty("UdtTypeName").GetSetMethod();
#endif

        internal readonly static MethodInfo MExecutorCallNonQuery = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQuery" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsync = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsync" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsyncTyped = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsyncTyped" );

        internal readonly static ConstructorInfo CtorDecimalBits = typeof( decimal ).GetConstructor( new Type[] { typeof( int[] ) } );
#endregion

    }
}
