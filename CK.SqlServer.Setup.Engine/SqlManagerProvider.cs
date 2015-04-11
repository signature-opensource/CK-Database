#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Engine\SqlManagerProvider.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using CK.Core;
using System.Diagnostics;
using System.Data.SqlClient;

namespace CK.SqlServer.Setup
{

    public class SqlManagerProvider : ISqlManagerProvider, IDisposable
    {
        readonly IActivityMonitor _monitor;
        readonly Dictionary<string, Item> _items;
        readonly Action<ISqlManager> _dbConfigurator;

        class Item
        {
            public string ConnectionString;
            public ISqlManager Manager;
            public bool DoNotDispose;
            public bool AutoCreate;

            public override string ToString()
            {
                return String.Format( "{0} - {1}", Manager != null, ConnectionString );
            }
        }

        public SqlManagerProvider( IActivityMonitor monitor, Action<ISqlManager> dbConfigurator = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            _monitor = monitor;
            _items = new Dictionary<string, Item>();
            _dbConfigurator = dbConfigurator ?? Util.ActionVoid;
        }

        internal void AddConfiguredDefaultDatabase( ISqlManager m )
        {
            Debug.Assert( m.IsOpen() );
            Item i = new Item() { ConnectionString = m.Connection.ConnectionString, Manager = m, DoNotDispose = true };
            _items.Add( SqlDatabase.DefaultDatabaseName, i );
            _items.Add( i.ConnectionString, i );
        }
        
        public void Add( string name, string connectionString, bool autoCreate )
        {
            Item i = new Item() { ConnectionString = connectionString, AutoCreate = autoCreate };
            _items.Add( name, i );
            _items[connectionString] =  i;
        }

        public ISqlManager FindManagerByName( string name )
        {
            if( !String.IsNullOrWhiteSpace( name ) )
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
                    if( item.Key != null && item.Value.Manager != null && item.Value.DoNotDispose == false ) item.Value.Manager.Dispose();
                }
                _items.Clear();
            }
        }

    }
}
