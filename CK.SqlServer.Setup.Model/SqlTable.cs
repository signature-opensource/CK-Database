namespace CK.Core
{
    /// <summary>
    /// Base class for table objects. 
    /// Unless marked with <see cref="AmbientDefinerAttribute"/>, direct specializations are de facto real objects.
    /// A table is a <see cref="SqlPackage"/> with a <see cref="TableName"/>.
    /// </summary>
    [AmbientDefiner]
    public class SqlTable : SqlPackage
    {
        /// <summary>
        /// Initializes a new <see cref="SqlTable"/> with a null <see cref="TableName"/>.
        /// </summary>
        protected SqlTable()
        {
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
}
