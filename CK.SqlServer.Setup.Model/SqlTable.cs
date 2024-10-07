namespace CK.Core;

/// <summary>
/// Base class for table objects. 
/// Unless marked with <see cref="CKTypeDefinerAttribute"/>, direct specializations are de facto real objects.
/// A table is a <see cref="SqlPackage"/> with a <see cref="TableName"/>.
/// </summary>
[CKTypeDefiner]
public class SqlTable : SqlPackage
{
    /// <summary>
    /// Initializes a new <see cref="SqlTable"/> with an empty <see cref="TableName"/>.
    /// </summary>
    protected SqlTable()
    {
        TableName = string.Empty;
    }

    /// <summary>
    /// Initializes a new <see cref="SqlTable"/> with a <see cref="TableName"/>.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    public SqlTable( string tableName )
    {
        TableName = tableName;
    }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; protected set; }

    /// <summary>
    /// Gets the schema.name full name of the table.
    /// </summary>
    public string SchemaName => Schema + '.' + TableName;

}
