using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using System.Diagnostics;

namespace CK.SqlServer.Setup;

/// <summary>
/// Driver for <see cref="SqlTransformerItem"/> item: its <see cref="Install"/> applies the 
/// transformation to the target Sql object.
/// </summary>
public class SqlTransformerItemDriver : SetupItemDriver
{
    /// <summary>
    /// Initializes a new <see cref="SqlTransformerItemDriver"/>.
    /// </summary>
    /// <param name="info">Driver build information (required by base SetupItemDriver).</param>
    public SqlTransformerItemDriver( BuildInfo info )
        : base( info )
    {
    }


    /// <summary>
    /// Masked to formally associates a <see cref="SqlTransformerItem"/> item.
    /// </summary>
    public new SqlTransformerItem Item => (SqlTransformerItem)base.Item;

    /// <summary>
    /// Installs the transformer by applying the transformation.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="beforeHandlers">When true, nothing is done: the transformation is applied after the handlers.</param>
    /// <returns>True on success, false otherwise.</returns>
    protected override bool Install( IActivityMonitor monitor, bool beforeHandlers )
    {
        if( beforeHandlers ) return true;
        var transformed = Item.SqlObject.SafeTransform( monitor, Item.Target.SqlObject );
        if( transformed == null )
        {
            using( monitor.OpenError( "Transformation source:" ) )
            {
                monitor.Error( Item.Target.SqlObject.ToFullString() );
            }
            return false;
        }
        var objectDriver = Drivers[Item.Source] as SqlObjectItemDriver;
        if( objectDriver != null )
        {
            return objectDriver.OnTargetTransformed( monitor, this, (ISqlServerObject)transformed );
        }
        // We are transforming... a transformer!
        Debug.Assert( Drivers[Item.Target] is SqlTransformerItemDriver );
        Item.Target.SqlObject = transformed;
        using( monitor.OpenTrace( "Transformation of the Transformer:" ) )
        {
            monitor.Trace( Item.Target.SqlObject.ToFullString() );
        }
        return true;
    }

}
