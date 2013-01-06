using System;
using System.Collections.Generic;
using CK.Core;
using System.Diagnostics;

namespace CK.SqlServer.Setup
{

    public class SqlManagerProvider : ISqlManagerProvider, IDisposable
    {
        readonly IActivityLogger _logger;
        readonly Dictionary<string, Item> _items;

        class Item
        {
            public string ConnectionString;
            public SqlManager Manager;
            public bool DoNotDispose;
        }

        public SqlManagerProvider( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "_logger" );
            _logger = logger;
            _items = new Dictionary<string, Item>();
        }

        internal void AddDefaultDatabase( SqlManager m )
        {
            Debug.Assert( m.IsOpen() );
            Item i = new Item() { ConnectionString = m.CurrentConnectionString, Manager = m, DoNotDispose = true };
            _items.Add( SqlDatabase.DefaultDatabaseName, i );
            _items.Add( i.ConnectionString, i );
        }
        
        public void Add( string name, string connectionString )
        {
            Item i = new Item() { ConnectionString = connectionString };
            _items.Add( name, i );
            _items[connectionString] =  i;
        }

        public SqlManager FindManagerByName( string name )
        {
            if( !String.IsNullOrWhiteSpace( name ) )
            {
                Item i;
                if( _items.TryGetValue( name, out i ) )
                {
                    if( i.Manager == null )
                    {
                        SqlManager m = new SqlManager();
                        m.Logger = _logger;
                        m.OpenFromConnectionString( i.ConnectionString );
                        i.Manager = m;
                    }
                    return i.Manager;
                }
            }
            return null;
        }

        public SqlManager FindManagerByConnectionString( string connectionString )
        {
            if( !String.IsNullOrWhiteSpace( connectionString ) )
            {
                Item i;
                if( _items.TryGetValue( connectionString, out i ) )
                {
                    if( i.Manager == null )
                    {
                        SqlManager m = new SqlManager();
                        m.Logger = _logger;
                        m.OpenFromConnectionString( i.ConnectionString );
                        i.Manager = m;
                    }
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
                    if( item.Key != null && item.Value.Manager != null && item.Value.DoNotDispose == false ) item.Value.Manager.Dispose();
                }
                _items.Clear();
            }
        }

    }
}
