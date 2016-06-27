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

    public abstract class SqlObjectItem : SqlBaseItem
    {
        bool? _missingDependencyIsError;

        internal SqlObjectItem()
        {
        }

        internal SqlObjectItem( SqlContextLocName name, string itemType, ISqlServerObject parsed )
            : base( name, itemType, parsed )
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="ISqlServerObject"/>. 
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
        /// Sets first by MissingDependencyIsError in text, otherwise an attribute (that should default to true should be applied).
        /// When not set, it is considered to be true.
        /// </summary>
        public bool? MissingDependencyIsError
        {
            get { return _missingDependencyIsError; }
            set { _missingDependencyIsError = value; }
        }

        protected override object StartDependencySort() => typeof( SqlObjectItemDriver );

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

        class ConfigReader : SetupConfigReader
        {
            protected override void OnUnknownProperty( StringMatcher m, string propName, ISetupObjectTransformerItem transformer, IMutableSetupObjectItem target )
            {
                if( propName == "MissingDependencyIsError" )
                {
                    bool x = m.TryMatchText( "true" );
                    if( !x && !m.TryMatchText( "false" ) ) m.SetError( "true or false" );
                    else ((SqlObjectItem)target).MissingDependencyIsError = x;
                }
                else base.OnUnknownProperty( m, propName, transformer, target );
            }
        }

        internal override bool Initialize( IActivityMonitor monitor, string fileName, IDependentItemContainer packageItem )
        {
            if( !CheckSchemaAndObjectName( monitor, fileName, packageItem ) ) return false;
            bool foundConfig;
            string h = SqlObject.HeaderComments.Select( c => c.Text ).Concatenate( Environment.NewLine );
            if( !new ConfigReader().Apply( monitor, h, this, out foundConfig ) ) return false;
            if( !foundConfig )
            {
                monitor.Warn().Send( $"Resource '{fileName}' of '{packageItem?.FullName}' should define SetupConfig:{{}} (even an empty one)." );
                if( !OBSOLETESetPropertiesFromHeader( monitor ) || !SetMissingIsDependencyIsErrorFromHeader( monitor ) ) return false;
            }
            return true;
        }

        #region Obsolete old header...
        static Regex _rMissingDep = new Regex( @"MissingDependencyIsError\s*=\s*(?<1>\w+)",
                                            RegexOptions.CultureInvariant
                                            | RegexOptions.IgnoreCase
                                            | RegexOptions.ExplicitCapture );

        static Regex _rHeader = new Regex( @"^\s*--\s*(Version\s*=\s*(?<1>\d+(\.\d+)*|\*))?\s*(,\s*(Package\s*=\s*(?<2>(\w|\.|-)+)|Requires\s*=\s*{\s*(?<3>\??(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<3>\??(\w+|-|\^|\[|]|\.)+)\s*)*}|Groups\s*=\s*{\s*(?<4>(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<4>(\w+|-|\^|\[|]|\.)+)\s*)*}|RequiredBy\s*=\s*{\s*(?<5>(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<5>(\w+|-|\^|\[|]|\.)+)\s*)*}|PreviousNames\s*=\s*{\s*((?<6>(\w+|-|\^|\[|]|\.)+)\s*=\s*(?<6>\d+\.\d+\.\d+(\.\d+)?))\s*(,\s*((?<6>(\w+|-|\^|\[|]|\.)+)\s*=\s*(?<6>\d+(\.\d+){1,3}))\s*)*})\s*)*",
                                        RegexOptions.CultureInvariant
                                        | RegexOptions.IgnoreCase
                                        | RegexOptions.ExplicitCapture );

        protected bool OBSOLETESetPropertiesFromHeader( IActivityMonitor monitor )
        {
            string header = SqlObject.HeaderComments.Select( h => h.ToString() ).Concatenate( string.Empty );
            Match mHeader = _rHeader.Match( header );
            if( !mHeader.Success )
            {
                monitor.Error().Send( "Invalid header: -- Version=X.Y.Z or Version=* must appear first in header." );
                return false;
            }
            string packageName = null;
            IEnumerable<string> requires = null;
            IEnumerable<string> groups = null;
            IEnumerable<string> requiredBy = null;
            Version version = null;
            IEnumerable<VersionedName> previousNames = null;

            if( mHeader.Groups[2].Length > 0 ) packageName = mHeader.Groups[2].Value;
            if( mHeader.Groups[3].Captures.Count > 0 ) requires = mHeader.Groups[3].Captures.Cast<Capture>().Select( m => m.Value );
            if( mHeader.Groups[4].Captures.Count > 0 ) groups = mHeader.Groups[4].Captures.Cast<Capture>().Select( m => m.Value );
            if( mHeader.Groups[5].Captures.Count > 0 ) requiredBy = mHeader.Groups[5].Captures.Cast<Capture>().Select( m => m.Value );
            if( mHeader.Groups[6].Captures.Count > 0 )
            {
                var prevNames = mHeader.Groups[6].Captures.Cast<Capture>().Select( m => m.Value );
                var prevVer = mHeader.Groups[6].Captures.Cast<Capture>().Select( m => Version.Parse( m.Value ) );
                previousNames = prevNames.Zip( prevVer, ( n, v ) => new VersionedName( n, v ) );
            }
            if( mHeader.Groups[1].Length <= 1 ) version = null;
            else if( !Version.TryParse( mHeader.Groups[1].Value, out version ) || version.Revision != -1 || version.Build == -1 )
            {
                monitor.Error().Send( "-- Version=X.Y.Z or Version=* must appear first in header." );
                return false;
            }
            if( version != null ) Version = version;
            if( packageName != null ) Container = new NamedDependentItemContainerRef( packageName );
            if( requires != null ) Requires.Add( requires );
            if( requiredBy != null ) RequiredBy.Add( requiredBy );
            if( groups != null ) Groups.Add( groups );
            if( previousNames != null ) PreviousNames.AddRange( previousNames );
            return true;
        }

        bool SetMissingIsDependencyIsErrorFromHeader( IActivityMonitor monitor )
        {
            foreach( var h in SqlObject.HeaderComments )
            {
                Match missDep = _rMissingDep.Match( h.Text );
                if( missDep.Success )
                {
                    bool m;
                    if( bool.TryParse( missDep.Groups[1].Value, out m ) )
                    {
                        MissingDependencyIsError = m;
                        break;
                    }
                    else
                    {
                        monitor.Error().Send( "Invalid syntax: it should be MissingDependencyIsError = true or MissingDependencyIsError = false." );
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        bool CheckSchemaAndObjectName( IActivityMonitor monitor, string fileName, IDependentItemContainer packageItem )
        {
            if( ContextLocName.ObjectName != SqlObject.Name )
            {
                monitor.Error().Send( $"Resource '{fileName}' of '{packageItem?.FullName}' contains the definition of '{ContextLocName.Name}'. Names must match." );
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
                monitor.Error().Send( $"Resource '{fileName}' of '{packageItem?.FullName}' defines the {ItemType} in the schema '{SqlObject.Schema}' instead of '{ContextLocName.Schema}'." );
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
        internal readonly static MethodInfo MExecutorCallNonQueryAsyncCancellable = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsyncCancellable" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsyncTyped = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsyncTyped" );
        internal readonly static MethodInfo MExecutorCallNonQueryAsyncTypedCancellable = typeof( ISqlCommandExecutor ).GetMethod( "ExecuteNonQueryAsyncTypedCancellable" );

        internal readonly static ConstructorInfo CtorDecimalBits = typeof( decimal ).GetConstructor( new Type[] { typeof( int[] ) } );
        #endregion

    }
}
