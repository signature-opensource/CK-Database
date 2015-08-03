using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    public interface ISqlSetupAspect
    {
        /// <summary>
        /// Gets the <see cref="ISqlServerParser"/> to use.
        /// </summary>
        ISqlServerParser SqlParser { get; }

        /// <summary>
        /// Gets the default database as a <see cref="SqlManager"/> object.
        /// </summary>
        ISqlManager DefaultSqlDatabase { get; }

        /// <summary>
        /// Gets the available databases (including the <see cref="DefaultSqlDatabase"/>).
        /// It is initialized with <see cref="SqlSetupAspectConfiguration.Databases"/> content but can be changed.
        /// </summary>
        ISqlManagerProvider SqlDatabases { get; }

    }
}
