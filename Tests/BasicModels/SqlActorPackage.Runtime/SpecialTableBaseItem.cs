using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SqlActorPackage.Runtime
{
    public class SpecialTableBaseItem : SqlTableItem, IStObjSetupDynamicInitializer
    {
        readonly ISqlServerParser _parser;

        public SpecialTableBaseItem( IActivityMonitor monitor, IStObjSetupData data, ISqlServerParser parser ) 
            : base( monitor, data )
        {
            _parser = parser;
        }

        public void DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            Debug.Assert( this == item );
            var name = SqlBaseItem.SqlBuildFullName( this, SetupObjectItemBehavior.Transform, "sDynObjRead" );
            string text = name.LoadTextResource( state.Monitor, this, out string fileName );
            var parseResult = _parser.ParseTransformer( text );
            if( parseResult.IsError )
            {
                parseResult.LogOnError( state.Monitor );
                return;
            }
            var t = new SqlTransformerItem( name, parseResult.Result );
            state.
            //Must find the Target in the state memory.
            // and call AddTransformer to it.
            Children.Add( t );
        }
    }
}
