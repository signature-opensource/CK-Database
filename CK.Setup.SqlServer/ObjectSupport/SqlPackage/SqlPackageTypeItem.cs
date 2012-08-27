using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlPackageTypeItem : PackageTypeItem
    {
        internal SqlPackageTypeItem( Type itemType, SqlPackageAttribute attr )
            : base( itemType, attr, "SqlPackageType" )
        {
        }

        internal SqlPackageTypeItem( Type itemType, SqlPackageAttribute attr, PackageTypeItem inherited )
            : base( itemType, attr, inherited, "SqlPackageType" )
        {
        }

        public new SqlPackageAttribute Attribute
        {
            get { return (SqlPackageAttribute)base.Attribute; }
        }

        public SqlDatabaseItem DefaultDatabaseItem { get; set; }

        //protected override void InitDependentItem( IActivityLogger logger, ITypedObjectMapper mapper )
        //{
        //    DefaultDatabaseItem = (SqlDatabaseItem)mapper[Attribute.DefaultDatabase ?? typeof(SqlDefaultDatabase)];

        //    if( Container == null ) Container = DefaultDatabaseItem;

        //}

    }
}
