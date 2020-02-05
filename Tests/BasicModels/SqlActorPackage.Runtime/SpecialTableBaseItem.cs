using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SqlActorPackage.Runtime
{
    public class SpecialTableBaseItem : SqlTableItem, IStObjSetupDynamicInitializer, ISetupItemDriverAware
    {
        readonly ISqlServerParser _parser;
        string _specialName;

        public SpecialTableBaseItem( IActivityMonitor monitor, IStObjSetupData data, ISqlServerParser parser ) 
            : base( monitor, data )
        {
            _parser = parser;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem _, IStObjResult stObjResult )
        {
            Debug.Assert( this == _ );

            //
            var tableName = ActualObject.TableName;
            if( String.IsNullOrEmpty( tableName ) || tableName[0] != 't' || !tableName.EndsWith( "Special" ) )
            {
                state.Monitor.Error( $"{FullName}: TableName property must be set and follow the 'tXXXXSpecial' pattern." );
                return;
            }
            _specialName = tableName.Substring( 1, tableName.Length - 1 - 7 );
            SetDirectPropertyValue( state.Monitor, "SpecialName", _specialName, nameof( IStObjSetupDynamicInitializer.DynamicItemInitialize ) );

            var name = SqlBaseItem.SqlBuildFullName( this, SetupObjectItemBehavior.Transform, "sActorCreate" );
            string text = name.LoadTextResource( state.Monitor, this, out string _ );
            if( text == null )
            {
                state.Monitor.Error( $"Special table '{FullName}' requires a transfomer on sActorCreate procedure to exist." );
                return;
            }
            var parseResult = _parser.ParseTransformer( text );
            if( parseResult.IsError )
            {
                parseResult.LogOnError( state.Monitor );
                return;
            }
            var t = new SqlTransformerItem( name, parseResult.Result );
            var target = state.FindItem( name.TransformArg );
            if( target == null ) throw new CKException( "Actor Package should have defined the sActorCreate procedure." );
            target.OnItemAvailable( state.Monitor, (m,item) => item.AddTransformer( m, t ) );
            Children.Add( t );
            // Synchronizes the ordering of the transformers with the ordering of the table.
            SqlTransformerItem previous = (SqlTransformerItem)state.Memory[typeof( SpecialTableBaseItem )];
            if( previous != null ) t.Requires.Add( previous );
            state.Memory[typeof( SpecialTableBaseItem )] = t;
        }

        public class Handler : SetupHandler
        {
            readonly string _specialName;

            public Handler( SetupItemDriver d, string specialName )
                : base( d )
            {
                _specialName = specialName;
            }

            protected new SqlTableItemDriver Driver => (SqlTableItemDriver)base.Driver;
            
            protected override bool Settle( IActivityMonitor monitor )
            {
                var script = SourceCodeSetupScript.CreateFromSourceCode( Driver.Item, $@"
-- This is a settle script that is automatically generated.
-- Here we can execute something with the SpecialName = '{_specialName}'.
", "sql" );
                return Driver.DatabaseDriver.RunSetupScript( monitor, script );
            }
        }

        bool ISetupItemDriverAware.OnDriverPreInitialized( IActivityMonitor monitor, SetupItemDriver driver )
        {
            Debug.Assert( driver is SqlTableItemDriver && driver.Item == this );
            if( driver.ExternalVersion == null )
            {
                driver.AddHandler( new Handler( driver, _specialName ) );
            }
            return true;
        }
    }
}
