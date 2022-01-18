using System.Collections.Generic;
using CK.Setup;
using CK.Core;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Defines the connection object.
    /// Its driver is <see cref="SqlDatabaseConnectionItemDriver"/>.
    /// </summary>
    public class SqlDatabaseConnectionItem : ISetupItem, IDependentItemRef
    {
        readonly SqlDatabaseItem _db;

        /// <summary>
        /// Initializes a new <see cref="SqlDatabaseConnectionItem"/>.
        /// </summary>
        /// <param name="db">The database item.</param>
        public SqlDatabaseConnectionItem( SqlDatabaseItem db )
        {
            _db = db;
        }

        /// <summary>
        /// Gets the <see cref="SqlDatabaseItem"/>.
        /// </summary>
        public SqlDatabaseItem SqlDatabaseItem => _db;

        /// <summary>
        /// Gets the <see cref="SqlDatabase"/> object instance (the <see cref="SqlDatabaseItem.ActualObject"/>).
        /// </summary>
        public SqlDatabase SqlDatabase => _db.ActualObject;

        /// <summary>
        /// Gets the full name of this connection: : it is the FullName of the <see cref="SqlDatabase"/> suffixed with ".Connection".
        /// </summary>
        public string FullName => _db.FullName + ".Connection";

        /// <summary>
        /// Gets the name of this connection: it is the Name of the <see cref="SqlDatabase"/> suffixed with ".Connection".
        /// </summary>
        public string Name => _db.Name + ".Connection"; 

        IDependentItemContainerRef IDependentItem.Container => null;

        IDependentItemRef IDependentItem.Generalization => null;

        IEnumerable<IDependentItemRef> IDependentItem.Requires => null; 

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups => null; 

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy => null; 

        object IDependentItem.StartDependencySort( IActivityMonitor m ) => typeof( SqlDatabaseConnectionItemDriver );

        bool IDependentItemRef.Optional => false; 

        /// <summary>
        /// Gets the context name.
        /// </summary>
        public string Context => _db.Context; 

        /// <summary>
        /// Gets the location.
        /// </summary>
        public string Location => _db.Location;

        string IContextLocNaming.TransformArg => null;

        IContextLocNaming IContextLocNaming.CombineName( string n ) => new ContextLocName( Context, Location, Name ).CombineName( n );
    }
}
