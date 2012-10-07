using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    public class SqlPackageBaseItem : StObjDynamicPackageItem
    {
        /// <summary>
        /// Initializes a new <see cref="SqlPackageBaseItem"/> for a dynamic object.
        /// </summary>
        /// <param name="itemType">Type of item (must not be longer than 16 characters).</param>
        /// <param name="driverType">Type of the associated driver or its assembly qualified name.</param>
        /// <param name="obj">The final <see cref="Object"/>.</param>
        protected SqlPackageBaseItem( string itemType, object driverType, object obj )
            : base( itemType, driverType, obj )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlPackageBaseItem"/> bound to a StObj.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="data">Structured Object data that contains the <see cref="Object"/>.</param>
        public SqlPackageBaseItem( IActivityLogger logger, IStObjSetupData data )
            : base( logger, data )
        {
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlPackageBase"/>.
        /// </summary>
        public new SqlPackageBase Object
        { 
            get { return (SqlPackageBase)base.Object; } 
        }

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
