using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlManagerProvider : IDisposable
    {
        IActivityLogger _logger;
        Dictionary<string, Item> _items;

        class Item
        {
            public string ConnectionString;
            public SqlManager Manager;
        }

        public SqlManagerProvider( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
            _items = new Dictionary<string, Item>();
        }

        public void AddOpenedManager( string name, SqlManager manager )
        {
            if( manager == null ) throw new ArgumentNullException( "manager" );
            if( !manager.IsOpen() ) throw new ArgumentException( "Manager must be opened.", "manager" );
            _items.Add( name, new Item() { Manager = manager } );
        }
        
        public void Add( string name, string connectionString )
        {
            _items.Add( name, new Item() { ConnectionString = connectionString } );
        }

        public SqlManager Obtain( string name )
        {
            Item i;
            if( _items.TryGetValue( name, out i ) )
            {
                if( i.Manager == null )
                {
                    SqlManager m = new SqlManager();
                    m.Logger = _logger;
                    m.OpenFromConnectionString( i.ConnectionString );
                }
                return i.Manager;
            }
            return null;
        }

        public void Dispose()
        {
            if( _items != null )
            {
                foreach( var item in _items )
                {
                    if( item.Key != null && item.Value.Manager != null ) item.Value.Manager.Dispose();
                }
                _items = null;
            }
        }

    }
}
