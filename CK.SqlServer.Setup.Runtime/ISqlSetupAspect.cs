using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup;

/// <summary>
/// Sql setup aspect. Provides a <see cref="SqlParser"/> and <see cref="SqlDatabases"/>.
/// </summary>
public interface ISqlSetupAspect
{
    /// <summary>
    /// Gets the <see cref="ISqlServerParser"/> to use.
    /// </summary>
    ISqlServerParser SqlParser { get; }

    /// <summary>
    /// Gets the default database as a <see cref="ISqlManagerBase"/> object.
    /// </summary>
    ISqlManagerBase DefaultSqlDatabase { get; }

    /// <summary>
    /// Gets the available databases (including the <see cref="DefaultSqlDatabase"/>).
    /// It is initialized with <see cref="SqlSetupAspectConfiguration.Databases"/> content but can be changed.
    /// </summary>
    ISqlManagerProvider SqlDatabases { get; }

    /// <summary>
    /// Gets whether the resolution of objects must be done globally.
    /// This is a temporary property: this should eventually be the only mode...
    /// </summary>
    bool GlobalResolution { get; }

}
