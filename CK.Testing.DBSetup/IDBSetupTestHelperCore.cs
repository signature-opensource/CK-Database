using CK.Testing.SqlServer;
using CKSetup;
using System;

namespace CK.Testing.DBSetup
{
    /// <summary>
    /// DBSetup core helper exposes only a the <see cref="RunDBSetup"/> method.
    /// This helper heavily relies on <see cref="CKSetup.ICKSetupTestHelperCore"/>, with
    /// the help of <see cref="StObjSetup.IStObjSetupTestHelperCore"/> and <see cref="SetupableSetup.ISetupableSetupTestHelperCore"/>.
    /// </summary>
    public interface IDBSetupTestHelperCore
    {
        /// <summary>
        /// Runs the database setup in <see cref="IBasicTestHelper.BinFolder"/> on the default database
        /// (see <see cref="ISqlServerTestHelperCore.DefaultDatabaseOptions"/>).
        /// Automatically called by <see cref="StObjMap.IStObjMapTestHelperCore.StObjMap"/>
        /// when the StObjMap is not yet initialized.
        /// This method uses CKSetup.Core (thanks to <see cref="ICKSetupTestHelper"/>).
        /// </summary>
        /// <param name="db">Defaults to the default database (<see cref="ISqlServerTestHelperCore.DefaultDatabaseOptions"/>).</param>
        /// <param name="traceStObjGraphOrdering">True to trace input and output of StObj graph ordering.</param>
        /// <param name="traceSetupGraphOrdering">True to trace input and output of setup graph ordering.</param>
        /// <param name="revertNames">True to revert names in ordering.</param>
        /// <returns>The setup result: succeed, failed or up-to-date.</returns>
        CKSetupRunResult RunDBSetup( ISqlServerDatabaseOptions db = null, bool traceStObjGraphOrdering = false, bool traceSetupGraphOrdering = false, bool revertNames = false );
    }
}
