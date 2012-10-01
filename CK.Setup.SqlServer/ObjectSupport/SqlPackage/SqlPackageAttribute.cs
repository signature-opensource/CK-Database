using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlPackageAttribute : SqlPackageAttributeBase, ISetupNameAttribute, IStObjSetupConfigurator
    {
        /// <summary>
        /// Gets or sets whether this package has an associated Model.
        /// Defaults to false.
        /// </summary>
        public bool HasModel { get; set; }

        /// <summary>
        /// Gets or sets the full name (for the setup process).
        /// Defaults to the <see cref="Type.Name"/> of the decorated package type.
        /// </summary>
        public string FullName { get; set; }

        protected override void ConfigureMutableItem( IActivityLogger logger, IStObjMutableItem o )
        {
            o.SetPropertyStructuralValue( logger, "SqlPackageAttribute", "HasModel", HasModel );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
            if( data.IsDefaultFullName )
            {
                logger.Info( "SqlPackage class '{0}' uses its own name as its setup FullName.", data.StObj.ObjectType.Name );
                data.FullNameWithoutContext = data.StObj.ObjectType.Name;
            }
            data.ItemType = typeof( SqlPackageItem );
            data.DriverType = typeof( SqlPackageSetupDriver );
        }
    
    }
}
