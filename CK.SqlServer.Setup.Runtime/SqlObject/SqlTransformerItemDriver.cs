using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;
using System.Text;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Driver for <see cref="SqlTransformerItem"/> item: its <see cref="Install"/> applies the 
    /// transformation to the target Sql object.
    /// </summary>
    public class SqlTransformerItemDriver : SetupItemDriver
    {
        public SqlTransformerItemDriver( BuildInfo info )
            : base( info )
        {
        }

        public new SqlTransformerItem Item => (SqlTransformerItem)base.Item;

        protected override bool Install( bool beforeHandlers )
        {
            if( beforeHandlers ) return true;
            var transformed = Item.SqlObject.SafeTransform( Engine.Monitor, Item.Target.SqlObject );
            if( transformed == null )
            {
                using( Engine.Monitor.OpenError().Send( "Transformation source:" ) )
                {
                    Engine.Monitor.Error().Send( Item.Target.SqlObject.ToFullString() );
                }
                return false;
            }
            var objectDriver = Engine.Drivers[Item.Source] as SqlObjectItemDriver;
            if( objectDriver != null )
            {
                return objectDriver.OnTargetTransformed( this, (ISqlServerObject)transformed );
            }
            // We are transforming... a transformer!
            Debug.Assert( Engine.Drivers[Item.Target] is SqlTransformerItemDriver );
            Item.Target.SqlObject = transformed;
            using( Engine.Monitor.OpenTrace().Send( "Transformation of the Transformer:" ) )
            {
                Engine.Monitor.Trace().Send( Item.Target.SqlObject.ToFullString() );
            }
            return true;
        }

    }
}
