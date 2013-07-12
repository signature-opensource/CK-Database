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
        /// Default and trivial implementation of <see cref="IStObjRuntimeBuilder"/>.
        /// </summary>
        public readonly static IStObjRuntimeBuilder DefaultStObjRuntimeBuilder = new SimpleStObjRuntimeBuilder();

        class SimpleStObjRuntimeBuilder : IStObjRuntimeBuilder
        {
            public object CreateInstance( Type finalType )
            {
                return Activator.CreateInstance( finalType, true );
            }
        }

    }
}
