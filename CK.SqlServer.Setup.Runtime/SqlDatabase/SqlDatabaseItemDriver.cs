#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlDatabase\SqlDatabaseSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Setup;
using CK.Core;
using CK.SqlServer.Parser;
using System.Collections.Generic;
using System;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseItemDriver : SetupItemDriver
    {
        readonly SqlDatabaseConnectionItemDriver _connection;
        readonly List<ISqlServerObject> _sqlObjects;
        readonly Dictionary<object,object> _sharedState;

        public SqlDatabaseItemDriver( BuildInfo info )
            : base( info )
        {
            _connection = (SqlDatabaseConnectionItemDriver)Engine.Drivers[Item.ConnectionItem];
            _sqlObjects = new List<ISqlServerObject>();
            _sharedState = new Dictionary<object, object>();
        }

        /// <summary>
        /// Masked Item to formally be associated to a <see cref="SqlDatabaseItem"/> item.
        /// </summary>
        public new SqlDatabaseItem Item => (SqlDatabaseItem)base.Item;

        /// <summary>
        /// Gets the Sql manager for this database.
        /// </summary>
        public ISqlManagerBase SqlManager => _connection.SqlManager;

        /// <summary>
        /// Gets a shared state for drivers: drivers from the same database can
        /// easily share any service scoped to their database: <see cref="RegisterService{T}(T)"/>
        /// and 
        /// </summary>
        public IDictionary<object, object> SharedState => _sharedState;

        /// <summary>
        /// Registers a unique service of a given type in the <see cref="SharedState"/>.
        /// The type <typeparamref name="T"/> must not already exist otherwise an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type of the service to register.</typeparam>
        /// <param name="service">The service instance.</param>
        public void RegisterService<T>( T service ) => _sharedState.Add( typeof( T ), service );

        /// <summary>
        /// Gets a previously registered service from the <see cref="SharedState"/>. 
        /// </summary>
        /// <typeparam name="T">The type of the service to retrieve.</typeparam>
        /// <param name="mustExist">True to throw an exception if the service is not registered.</param>
        /// <returns>The instance or null if it not registered and <param name="mustExist"/> is false.</returns>
        public T GetService<T>( bool mustExist = false ) => mustExist ? (T)_sharedState[typeof(T)] : (T)_sharedState.GetValueWithDefault( typeof( T ), null );
    }
}
