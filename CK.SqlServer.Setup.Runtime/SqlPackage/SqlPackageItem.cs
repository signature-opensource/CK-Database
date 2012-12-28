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
            if( Object.HasModel ) EnsureModel();
        }

        public SqlPackageItem( IActivityLogger logger, IStObjSetupData data )
            : base( logger, data )
        {
            Debug.Assert( typeof( SqlPackageSetupDriver ).IsAssignableFrom( data.DriverType ) );
            Name = data.FullNameWithoutContext;
            if( Object.HasModel ) EnsureModel();
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
