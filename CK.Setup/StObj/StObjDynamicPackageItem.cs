using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Default <see cref="IDependentItem"/> implementation associated to <see cref="IStObj"/> object
    /// used when the <see cref="IStObjSetupData"/> does not specify a dedicated implementation (<see cref="IStObjSetupData.ItemType"/> 
    /// nor <see cref="IStObjSetupData.ItemTypeName"/> are set).
    /// Can be used as a base class for more specific item implementation.
    /// </summary>
    public class StObjDynamicPackageItem : DynamicPackageItem, IStructuredObjectHolder
    {
        /// <summary>
        /// Initializes a new <see cref="StObjDynamicPackageItem"/> fully initialized by a <see cref="IStObjSetupData"/> and a 
        /// parent <see cref=""/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="generalization"></param>
        public StObjDynamicPackageItem( IStObjSetupData data, IDependentItem generalization = null )
            : base( data.NoContent ? "StObjItem" : "StObjPackage", (object)data.DriverType ?? data.DriverTypeName )
        {
            Debug.Assert( Model == null, "Initially, a DynamicPackageItem has no model." );
            Debug.Assert( data.ItemType == null || data.ItemTypeName == null, "If we are using a DynamicPackageItem, this is because no explicit ItemType nor ItemTypeName have been set." );
            ThisIsNotAContainer = data.NoContent;
            if( data.HasModel ) EnsureModel();
            SetVersionsString( data.Versions );
            StructuredObject = data.StObj.StructuredObject;
            FullName = data.FullName;
            Generalization = generalization.GetReference();
        }

        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object).
        /// </summary>
        public object StructuredObject { get; private set; }

    }
}
