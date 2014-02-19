using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    public abstract partial class StObjContextRoot
    {
        /// <summary>
        /// Default and trivial implementation of <see cref="IStObjRuntimeBuilder"/> where <see cref="IStObjRuntimeBuilder.CreateInstance"/> implementation 
        /// uses <see cref="Activator.CreateInstance"/> to call the public default constructor of the type.
        /// </summary>
        public readonly static IStObjRuntimeBuilder DefaultStObjRuntimeBuilder = new SimpleStObjRuntimeBuilder();

        class SimpleStObjRuntimeBuilder : IStObjRuntimeBuilder
        {
            public object CreateInstance( Type finalType )
            {
                return Activator.CreateInstance( finalType, false );
            }
        }

    }
}
