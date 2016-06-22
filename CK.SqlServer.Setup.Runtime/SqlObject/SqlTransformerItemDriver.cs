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
    public class SqlTransformerItemDriver : SetupItemDriver
    {
        public SqlTransformerItemDriver( BuildInfo info )
            : base( info )
        {
        }

        public new SqlTransformerItem Item => (SqlTransformerItem)base.Item;

        protected override bool Init( bool beforeHandlers )
        {
            if( beforeHandlers )
            {
                SetupItemDriver target = Engine.Drivers[Item.ContextLocName.TransformArg];
                Debug.Assert( target != null );
                target.AddInstallHandler( DoTransform ); 
            }
            return true;
        }

        bool DoTransform( SetupItemDriver target )
        {
            var t = target.Item as SqlBaseItem;
            if( t == null )
            {
                Engine.Monitor.Error().Send( $"Target object type must be a SqlBaseItem. '{Item.ContextLocName.TransformArg}' is {target.Item.GetType().Name}." );
                return false;
            }
            t.SqlObject = Item.SqlObject.SafeTransform( Engine.Monitor, t.SqlObject );
            return true;
        }
    }
}
