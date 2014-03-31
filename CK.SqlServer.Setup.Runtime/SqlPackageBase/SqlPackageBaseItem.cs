using System;
using System.Diagnostics;
using System.Linq;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlPackageBaseItem : StObjDynamicPackageItem
    {
        public SqlPackageBaseItem( SqlPackageBase package )
            : this( "ObjPackage", typeof( SqlPackageBaseSetupDriver ), package )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlPackageBaseItem"/> for a dynamic object.
        /// </summary>
        /// <param name="itemType">Type of item (must not be longer than 16 characters).</param>
        /// <param name="driverType">Type of the associated driver or its assembly qualified name.</param>
        /// <param name="obj">The final <see cref="Object"/>.</param>
        protected SqlPackageBaseItem( string itemType, object driverType, object obj )
            : base( itemType, driverType, obj )
        {
            if( Object.HasModel ) EnsureModel();
        }

        /// <summary>
        /// Initializes a new <see cref="SqlPackageBaseItem"/> bound to a StObj.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Structured Object data that contains the <see cref="Object"/>.</param>
        public SqlPackageBaseItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Context = data.StObj.Context.Context;
            if( Object.Database != null ) Location = Object.Database.Name;
            ResourceLocation = (ResourceLocator)data.StObj.GetStObjProperty( "ResourceLocation" );
            if( Object.HasModel ) EnsureModel();
            
            Debug.Assert( typeof( SqlPackageBaseSetupDriver ).IsAssignableFrom( data.DriverType ) );
            Name = data.FullNameWithoutContext;
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlPackageBase"/>.
        /// </summary>
        public new SqlPackageBase Object
        { 
            get { return (SqlPackageBase)base.Object; } 
        }

        /// <summary>
        /// Gets or sets a <see cref="ResourceLocation"/> that locates the resources associated 
        /// to this package.
        /// </summary>
        public ResourceLocator ResourceLocation { get; set; }

        protected override object StartDependencySort()
        {
            if( Model != null )
            {
                Model.Groups.AddRange( Groups.OfType<SqlDatabaseItem>() );
            }
            return base.StartDependencySort();
        }
    }
}
