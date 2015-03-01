#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObjModelExtension.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    static public class StObjModelExtension
    {
        public static T Obtain<T>( this IContextualStObjMap @this )
        {
            return (T)@this.Obtain( typeof( T ) );
        }
    }
}
