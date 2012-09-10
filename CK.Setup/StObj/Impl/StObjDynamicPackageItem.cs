using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    internal class StObjDynamicPackageItem : DynamicPackageItem, IStructuredObjectHolder
    {
        public StObjDynamicPackageItem( StObjSetupData data )
            : base( data.NoContent ? "StObjItem" : "StObjPackage", (object)data.DriverType ?? data.DriverTypeName )
        {
            Debug.Assert( Model == null );
            ThisIsNotAContainer = data.NoContent;
            if( data.HasModel ) EnsureModel();
            SetVersionsString( data.Versions );
            StructuredObject = data.StObj.StructuredObject;
            FullName = data.FullName;
        }

        public object StructuredObject { get; private set; }

    }
}
