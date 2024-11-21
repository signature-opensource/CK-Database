using CK.Setup;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace CK.Core;

/// <summary>
/// Base class for actual packages and <see cref="SqlTable"/>.
/// </summary>
[RealObject( ItemKind = DependentItemKindSpec.Container )]
[StObjProperty( PropertyName = "ResourceLocation", PropertyType = typeof( IResourceLocator ) )]
[CKTypeDefiner]
public class SqlPackage : SqlServer.ISqlConnectionStringProvider, IRealObject
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

    string SqlServer.ISqlConnectionStringProvider.ConnectionString => Database?.ConnectionString ?? string.Empty;
}
