using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    public class SqlPackageItem : SqlPackageBaseItem
    {
        public SqlPackageItem( SqlPackage package )
            : base( "ObjPackage", typeof( SqlPackageSetupDriver ), package )
        {
        }

        public SqlPackageItem( IActivityLogger logger, IStObjSetupData data )
            : base( logger, data )
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
