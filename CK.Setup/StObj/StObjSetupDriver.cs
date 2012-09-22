using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Can be used as a base class for specialized setup driver associated to <see cref="StObjDynamicPackageItem"/>.
    /// Simply adds typed accessors to the <see cref="Item"/> (that is a <see cref="IStructuredObjectHolder"/>) and its structured object.
    /// </summary>
    public class StObjSetupDriver<T> : SetupDriver
    {
        public StObjSetupDriver( SetupDriver.BuildInfo info )
            : base( info )
        {
            Object = (T)Item.StructuredObject;
        }

        /// <summary>
        /// Masked item to formally be associated to <see cref="IStructuredObjectHolder"/> item.
        /// </summary>
        public new IStructuredObjectHolder Item { get { return (IStructuredObjectHolder)base.Item; } }

        /// <summary>
        /// Gets the <see cref="IStructuredObjectHolder.StructuredObject">Structured object</see>.
        /// </summary>
        public T Object { get; private set; }

    }
}
