using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// A sql inline table function item is a specialized <see cref="SqlCallableItem{T}"/> where T, the SqlObject,
    /// is a <see cref="ISqlServerFunctionInlineTable"/>.
    /// </summary>
    public class SqlFunctionInlineTableItem : SqlCallableItem<ISqlServerFunctionInlineTable>
    {
        /// <summary>
        /// Initializes an inline table function item with its name and code.
        /// </summary>
        /// <param name="name">Name of the function.</param>
        /// <param name="inlineFunction">Code of the function.</param>
        public SqlFunctionInlineTableItem( SqlContextLocName name, ISqlServerFunctionInlineTable inlineFunction )
            : base( name, "Function", inlineFunction )
        {
        }
    }
}
