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
            Item.Target.SqlObject = Item.SqlObject.SafeTransform( Engine.Monitor, Item.Target.SqlObject );
            if( Item.Target.SqlObject == null )
            {
                Engine.Monitor.Error().Send( "Transformation failed." );
                return false;
            }
            // If this is not the last transformer, we log the result of this intermediate transformation.
            if( Item != Item.Source.Transformers[Item.Source.Transformers.Count-1] )
            {
                using( Engine.Monitor.OpenTrace().Send( "Intermediate transform result:" ) )
                {
                    Engine.Monitor.Trace().Send( Item.Target.SqlObject.ToFullString() );
                }
            }
            return true;
        }

    }
}
