using CK.Core;
using System;
using System.Xml.Linq;

namespace CK.Setup;

/// <summary>
/// Associates a name to a connection string.
/// </summary>
public class SqlDatabaseDescriptor
{
    /// <summary>
    /// Initializes a new <see cref="SqlDatabaseDescriptor"/>.
    /// </summary>
    /// <param name="name">Logical database name.</param>
    /// <param name="connectionString">Connection string to the database.</param>
    /// <param name="autoCreate">Whether the database should be created if opening the connection fails.</param>
    public SqlDatabaseDescriptor( string name, string connectionString, bool autoCreate = false )
    {
        LogicalDatabaseName = name;
        ConnectionString = connectionString;
        AutoCreate = autoCreate;
    }

    static readonly XName xLogicalDatabaseName = XNamespace.None + "LogicalDatabaseName";
    static readonly XName xConnectionString = XNamespace.None + "ConnectionString";
    static readonly XName xAutoCreate = XNamespace.None + "AutoCreate";
    static readonly XName xHasCKCore = XNamespace.None + "HasCKCore";
    static readonly XName xUseSnapshotIsolation = XNamespace.None + "UseSnapshotIsolation";

    /// <summary>
    /// Initializes a new <see cref="SqlDatabaseDescriptor"/> from its xml representation.
    /// </summary>
    /// <param name="e">The element.</param>
    public SqlDatabaseDescriptor( XElement e )
    {
        LogicalDatabaseName = e.Element( xLogicalDatabaseName )?.Value ?? e.AttributeRequired( xLogicalDatabaseName )!.Value;
        var connection = e.Element( xConnectionString )?.Value;
        if( connection == null ) throw new System.Xml.XmlException( "Missing required element \"ConnectionString\"." );
        ConnectionString = connection;
        AutoCreate = (bool?)e.Element( xAutoCreate ) ?? (bool?)e.Attribute( xAutoCreate ) ?? false;
        HasCKCore = (bool?)e.Element( xHasCKCore ) ?? (bool?)e.Attribute( xHasCKCore ) ?? false;
        UseSnapshotIsolation = (bool?)e.Element( xUseSnapshotIsolation ) ?? (bool?)e.Attribute( xUseSnapshotIsolation ) ?? false;
    }


    /// <summary>
    /// Serializes its content in the provided <see cref="XElement"/> and returns it.
    /// The <see cref="SqlDatabaseDescriptor(XElement)"/> constructor will be able to read this element back.
    /// </summary>
    /// <param name="e">The element to populate.</param>
    /// <returns>The <paramref name="e"/> element.</returns>
    public XElement Serialize( XElement e )
    {
        e.Add( new XAttribute( xLogicalDatabaseName, LogicalDatabaseName ),
               AutoCreate ? new XAttribute( xAutoCreate, true ) : null,
               HasCKCore ? new XAttribute( xHasCKCore, true ) : null,
               UseSnapshotIsolation ? new XAttribute( xUseSnapshotIsolation, true ) : null,
               new XElement( xConnectionString, ConnectionString ) );
        return e;
    }

    /// <summary>
    /// Gets or sets the logical name of the database.
    /// It is independent of the actual database name.
    /// </summary>
    public string LogicalDatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets whether the database should be created
    /// if opening the connection fails.
    /// Defaults to false.
    /// </summary>
    public bool AutoCreate { get; set; }

    /// <summary>
    /// Gets or sets whether the CKCore schema and its helpers must be installed.
    /// Defaults to false.
    /// </summary>
    public bool HasCKCore { get; set; }

    /// <summary>
    /// Gets or sets whether snapshot isolation ("SET ALLOW_SNAPSHOT_ISOLATION ON") is configured on the database
    /// and activated ("SET READ_COMMITTED_SNAPSHOT ON") so that the default READ_COMITTED is actually READ_COMMITTED_SNAPSHOT.
    /// <para>
    /// When false (the default), no guaranty exists: this totally depends on the database, no attempt is made to alter it in any way.
    /// </para>
    /// This is always true for the default database.
    /// </para>
    /// <para>
    /// See https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/snapshot-isolation-in-sql-server)
    /// </para>
    /// </summary>
    public bool UseSnapshotIsolation { get; set; }

}
