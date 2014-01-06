using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;
using System.Data.SqlClient;

namespace CK.Setup.SqlServer
{
    /// <summary>
    /// Sql Server based memory provider for the setup.
    /// It is used by <see cref="SqlScriptExecutor"/> (created by <see cref="SqlScriptTypeHandler"/>)
    /// to skip already executed scripts.
    /// </summary>
    public class SqlSetupSessionMemoryProvider : ISetupSessionMemoryProvider, ISetupSessionMemory
    {
        SqlManager _manager;
        bool _initialized;

        public SqlSetupSessionMemoryProvider( SqlManager manager )
        {
            if( manager == null ) throw new ArgumentNullException( "manager" );
            _manager = manager;
        }

        /// <summary>
        /// Gets the date and time of the previous start.
        /// </summary>
        public DateTime LastStartDate { get; private set; }

        /// <summary>
        /// Gets the number of non terminated setup attempts.
        /// </summary>
        public int StartCount { get; private set; }

        /// <summary>
        /// Gets a description of the last ok (set by <see cref="StopSetup"/>).
        /// </summary>
        public string LastError { get; private set; }

        /// <summary>
        /// Gets whether <see cref="StartSetup"/> has been called and <see cref="StopSetup"/> has 
        /// not yet been called.
        /// </summary>
        public bool IsStarted { get; private set; }


        void Initialize()
        {
            _manager.EnsureCKCoreIsInstalled( _manager.Logger );
            using( var cRead = new SqlCommand( _initScript ) )
            {
                var existing = _manager.Connection.ReadFirstRow( cRead );
                LastStartDate = (DateTime)existing[0];
                if( LastStartDate == Util.SqlServerEpoch ) LastStartDate = DateTime.MinValue;
                StartCount = (int)existing[1];
                LastError = existing[2] == DBNull.Value ? null : (string)existing[2];
                _initialized = true;
            }
        }

        static string _initScript = @"
if object_id('CKCore.tSetupMemory') is null
begin
    if object_id('CKCore.tSetupMemoryItem') is not null drop table CKCore.tSetupMemoryItem;
	create table CKCore.tSetupMemory
	(
		-- This table is used as a heap: the primary key is not used
		-- and is here only to be azure compliant.
        SurrogateId int not null identity(0,1),
		CreationDate datetime not null,
		LastStartDate datetime not null,
		TotalStartCount int not null,
		StartCount int not null,
		LastError nvarchar(max),
		constraint PK_CKCore_tSetupMemory primary key(SurrogateId)
	);
    insert into CKCore.tSetupMemory(CreationDate,LastStartDate,TotalStartCount,StartCount,LastError) values( getutcdate(), 0, 0, 0, null );
end
if object_id('CKCore.tSetupMemoryItem') is null
begin
	create table CKCore.tSetupMemoryItem
	(
		ItemKey nvarchar(256) not null,
		ItemValue nvarchar(max) not null,
		constraint PK_CKCore_tSetupMemoryItem primary key(ItemKey)
	);
end
select LastStartDate, StartCount, LastError from CKCore.tSetupMemory;
";
        /// <summary>
        /// Starts a setup session. <see cref="IsStarted"/> must be false 
        /// otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        public ISetupSessionMemory StartSetup()
        {
            if( IsStarted ) throw new InvalidOperationException();
            if( !_initialized ) Initialize();
            _manager.Connection.ExecuteNonQuery( "update CKCore.tSetupMemory set LastStartDate = getutcdate(), TotalStartCount = TotalStartCount+1, StartCount = StartCount+1, LastError=N'Started but not Stopped yet.'" );
            IsStarted = true;
            return this;
        }

        /// <summary>
        /// On success, the whole memory of the setup process must be cleared. 
        /// On failure (when <paramref name="error"/> is not null), the memory must be persisted.
        /// <see cref="IsStarted"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="error">
        /// Must be not null to indicate an error. Null on success. 
        /// Empty or white space will raise an <see cref="ArgumentException"/>.
        /// </param>
        public void StopSetup( string error )
        {
            if( !IsStarted ) throw new InvalidOperationException();
            if( error == null )
            {
                _manager.Connection.ExecuteNonQuery( "update CKCore.tSetupMemory set StartCount = 0, LastError=null; drop table CKCore.tSetupMemoryItem;" );
                StartCount = 0;
                LastError = null;
            }
            else
            {
                if( String.IsNullOrWhiteSpace( error ) ) throw new ArgumentException( "Must not be null or empty.", "error" );

                using( var c = new SqlCommand( @"update CKCore.tSetupMemory set LastError=@LastError; select LastStartDate, StartCount from CKCore.tSetupMemory;" ) )
                {
                    c.Parameters.AddWithValue( "@LastError", error );
                    var resync = _manager.Connection.ReadFirstRow( c );
                    LastStartDate = (DateTime)resync[0];
                    StartCount = (int)resync[1];
                }
            }
            IsStarted = false;
        }
        
        #region ISetupSessionMemory Auto implementation

        void ISetupSessionMemory.RegisterItem( string itemKey, string itemValue )
        {
            if( itemValue == null ) throw new ArgumentNullException( "itemValue" );
            if( String.IsNullOrWhiteSpace( itemKey ) || itemKey.Length > 255 ) throw new ArgumentException( "Must not be null or empty or longer than 255 characters.", "itemKey" );

            using( var c = new SqlCommand( @"
merge CKCore.tSetupMemoryItem as t 
using (select ItemKey = @ItemKey) as s
on t.ItemKey = s.ItemKey
when matched then update set ItemValue = @ItemValue
when not matched then insert(ItemKey,ItemValue) values (@ItemKey, @ItemValue);" ) )
            {
                c.Parameters.AddWithValue( "@ItemKey", itemKey );
                c.Parameters.AddWithValue( "@ItemValue", itemValue );
                _manager.Connection.ExecuteNonQuery( c );
            }
        }

        string ISetupSessionMemory.FindRegisteredItem( string itemKey )
        {
            if( String.IsNullOrWhiteSpace( itemKey ) || itemKey.Length > 255 ) throw new ArgumentException( "Must not be null or empty or longer than 255 characters.", "itemKey" );
            using( var c = new SqlCommand( @"select ItemValue from CKCore.tSetupMemoryItem where ItemKey=@ItemKey;" ) )
            {
                c.Parameters.AddWithValue( "@ItemKey", itemKey );
                return (string)_manager.Connection.ExecuteScalar( c );
            }
        }

        bool ISetupSessionMemory.IsItemRegistered( string itemKey )
        {
            if( String.IsNullOrWhiteSpace( itemKey ) || itemKey.Length > 255 ) throw new ArgumentException( "Must not be null or empty or longer than 255 characters.", "itemKey" );
            using( var c = new SqlCommand( @"select 'a' from CKCore.tSetupMemoryItem where ItemKey=@ItemKey;" ) )
            {
                c.Parameters.AddWithValue( "@ItemKey", itemKey );
                return (string)_manager.Connection.ExecuteScalar( c ) != null;
            }
        }
        #endregion

    }
}
