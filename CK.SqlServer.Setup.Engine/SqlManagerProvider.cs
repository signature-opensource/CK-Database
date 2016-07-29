using System;
using System.Collections.Generic;
using CK.Core;
using System.Diagnostics;
using System.Data.SqlClient;

namespace CK.SqlServer.Setup
{

    class SqlManagerProvider : ISqlManagerProvider, IDisposable
    {
        readonly IActivityMonitor _monitor;
        readonly Dictionary<string, Item> _items;
        readonly Action<ISqlManagerBase> _dbConfigurator;

        class Item
        {
            public string ConnectionString;
            public ISqlManager Manager;
            public bool AutoCreate;

            public override string ToString() => $"{Manager != null} - {ConnectionString}";
        }

        public SqlManagerProvider( IActivityMonitor monitor, Action<ISqlManagerBase> dbConfigurator = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            _monitor = monitor;
            _items = new Dictionary<string, Item>();
            _dbConfigurator = dbConfigurator ?? Util.ActionVoid;
        }
        
        public void Add( string name, string connectionString, bool autoCreate )
        {
            Item i = new Item() { ConnectionString = connectionString, AutoCreate = autoCreate };
            _items.Add( name, i );
            _items[connectionString] =  i;
        }

        public ISqlManagerBase FindManagerByName( string name )
        {
            if( !string.IsNullOrWhiteSpace( name ) )
            {
                Item i;
                if( _items.TryGetValue( name, out i ) )
                {
                    if( i.Manager == null ) CreateManager( i );
                    return i.Manager;
                }
            }
            return null;
        }

        void CreateManager( Item i )
        {
            SqlManager m = new SqlManager( _monitor );
            if( !m.OpenFromConnectionString( i.ConnectionString, i.AutoCreate ) )
            {
                throw new CKException( "Unable to {1} database for '{0}'.", i.ConnectionString, i.AutoCreate ? "create" : "open" );
            }
            _dbConfigurator( m );
            i.Manager = m;
        }

        public ISqlManager FindManagerByConnectionString( string connectionString )
        {
            if( !String.IsNullOrWhiteSpace( connectionString ) )
            {
                Item i;
                if( _items.TryGetValue( connectionString, out i ) )
                {
                    if( i.Manager == null ) CreateManager( i );
                    return i.Manager;
                }
            }
            return null;
        }

        public void Dispose()
        {
            if( _items.Count > 0 )
            {
                foreach( var item in _items )
                {
                    if( item.Key != null && item.Value.Manager != null ) item.Value.Manager.Dispose();
                }
                _items.Clear();
            }
        }

        ISqlManagerBase ISqlManagerProvider.FindManagerByName( string logicalName ) => FindManagerByName( logicalName );

        ISqlManagerBase ISqlManagerProvider.FindManagerByConnectionString( string connectionString ) => FindManagerByConnectionString( connectionString );

    }
}
