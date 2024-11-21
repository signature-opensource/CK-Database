using CK.Core;

namespace CK.SqlServer.Setup;

public class SqlTransformContext
{
    public readonly IActivityMonitor Monitor;
    public readonly SqlPackageBaseItem Container;
    public readonly SqlObjectItem Item;

    public SqlTransformContext( IActivityMonitor monitor, SqlPackageBaseItem container, SqlObjectItem item )
    {
        Monitor = monitor;
        Container = container;
        Item = item;
    }
}
