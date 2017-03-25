#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObjContextRoot.DefaultStObjRuntimeBuilder.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
        /// uses <see cref="Activator.CreateInstance(Type)"/> to call the public default constructor of the type.
        /// </summary>
        public readonly static IStObjRuntimeBuilder DefaultStObjRuntimeBuilder = new SimpleStObjRuntimeBuilder();

        class SimpleStObjRuntimeBuilder : IStObjRuntimeBuilder
        {
            public object CreateInstance( Type finalType )
            {
                return Activator.CreateInstance( finalType );
            }
        }

    }
}
