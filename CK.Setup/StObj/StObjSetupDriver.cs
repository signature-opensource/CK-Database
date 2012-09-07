using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// This is the setup driver for structured object.
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
