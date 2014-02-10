using System;
using System.Diagnostics;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlPackageItem : SqlPackageBaseItem
    {
        public SqlPackageItem( SqlPackage package )
            : base( "ObjPackage", typeof( SqlPackageSetupDriver ), package )
        {
        }

        public SqlPackageItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Debug.Assert( typeof( SqlPackageSetupDriver ).IsAssignableFrom( data.DriverType ) );
            Name = data.FullNameWithoutContext;
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlPackage"/>.
        /// </summary>
        public new SqlPackage Object
        { 
            get { return (SqlPackage)base.Object; } 
        }

    }
}
