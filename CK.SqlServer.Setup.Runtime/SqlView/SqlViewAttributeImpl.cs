using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlViewAttributeImpl : SqlPackageAttributeImplBase, IStObjSetupConfigurator
    {
        public SqlViewAttributeImpl( SqlViewAttribute attribute )
            : base( attribute )
        {
        }

        protected new SqlViewAttribute Attribute { get { return (SqlViewAttribute)base.Attribute; } }

        protected override void ConfigureMutableItem( IActivityMonitor monitor, IStObjMutableItem o )
        {
            if( Attribute.ViewName != null ) o.SetDirectPropertyValue( monitor, "ViewName", Attribute.ViewName );
            if( Attribute.Schema != null ) o.SetAmbiantPropertyValue( monitor, "Schema", Attribute.Schema );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityMonitor monitor, IMutableStObjSetupData data )
        {
            SetAutomaticSetupFullNamewithoutContext( monitor, data, "SqlView" );

            data.ItemType = typeof( SqlViewItem );
            data.DriverType = typeof( SqlViewSetupDriver );
        }

    }
}
