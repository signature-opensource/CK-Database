using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    interface IStObjServiceFinalManualMapping : IStObjServiceClassFactory
    {
        /// <summary>
        /// Gets the unique number that identifies this factory.
        /// </summary>
        int Number { get; }
    }

}
