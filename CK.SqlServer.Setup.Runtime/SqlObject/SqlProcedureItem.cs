using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// A stored procedure item is a specialized <see cref="SqlCallableItem{T}"/> where T, the SqlObject,
    /// is a <see cref="ISqlServerStoredProcedure"/>.
    /// </summary>
    public class SqlProcedureItem : SqlCallableItem<ISqlServerStoredProcedure>
    {
        /// <summary>
        /// Initializes a stored procedure item with its name and code.
        /// </summary>
        /// <param name="name">Name of the stored procedure.</param>
        /// <param name="view">Code of the stored procedure.</param>
        public SqlProcedureItem( SqlContextLocName name, ISqlServerStoredProcedure storedProc )
            : base( name, "Procedure", storedProc )
        {
        }
    }
}
