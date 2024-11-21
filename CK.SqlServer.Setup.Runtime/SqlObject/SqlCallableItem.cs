using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup;

/// <summary>
/// Base class for item type "Function": <see cref="SqlFunctionInlineTableItem"/>, <see cref="SqlFunctionScalarItem"/>, 
/// <see cref="SqlFunctionTableItem"/>, and "Procedure": <see cref="SqlProcedureItem"/>.
/// </summary>
/// <typeparam name="T">Type of the callable.</typeparam>
public partial class SqlCallableItem<T> : SqlObjectItem, ISqlCallableItem where T : ISqlServerCallableObject
{
    /// <summary>
    /// Initializes a new <see cref="SqlObjectItem"/>.
    /// </summary>
    /// <param name="name">Name of this object.</param>
    /// <param name="itemType">Item type ("Function" or "Procedure").</param>
    /// <param name="procOrFunc">The parsed callable object.</param>
    public SqlCallableItem( SqlContextLocName name, string itemType, T procOrFunc )
        : base( name, itemType, procOrFunc )
    {
    }

    /// <summary>
    /// Gets or sets the sql object. 
    /// </summary>
    public new T SqlObject
    {
        get { return (T)base.SqlObject; }
        set { base.SqlObject = value; }
    }

    /// <summary>
    /// Gets the transform target item if this item has associated <see cref="SqlBaseItem.Transformers">Transformers</see>.
    /// This object is created as a clone of this object by the first call to this AddTransformer method.
    /// </summary>
    public new SqlCallableItem<T> TransformTarget => (SqlCallableItem<T>)base.TransformTarget;

    ISqlServerCallableObject ISqlCallableItem.CallableObject => SqlObject;

}
