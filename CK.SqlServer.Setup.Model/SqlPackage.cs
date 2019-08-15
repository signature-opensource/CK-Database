using CK.Setup;

namespace CK.Core
{
    /// <summary>
    /// Base class for actual packages and <see cref="SqlTable"/>.
    /// </summary>
    [StObj( ItemKind = DependentItemKindSpec.Container )]
    [StObjProperty( PropertyName = "ResourceLocation", PropertyType = typeof( IResourceLocator ) )]
    [AmbientDefiner]
    public class SqlPackage : SqlServer.ISqlConnectionStringProvider, IAmbientObject
    {
        /// <summary>
        /// Gets or sets the database to which this package belongs.
        /// Typically initialized by an attribute (like <see cref="SqlPackageAttribute"/>).
        /// </summary>
        [AmbientProperty]
        public SqlDatabase Database { get; set; }

        /// <summary>
        /// Gets or sets the sql schema.
        /// Typically initialized by an attribute (like <see cref="SqlPackageAttribute"/>).
        /// </summary>
        [AmbientProperty]
        public string Schema { get; set; }

        string SqlServer.ISqlConnectionStringProvider.ConnectionString => Database?.ConnectionString;
    }
}
