#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\DriverListExtension.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public static class DriverListExtension
    {
        public static T Find<T>( this IDriverBaseList @this, string fullName, bool throwIfNotFound )
            where T : DriverBase
        {
            T result = @this[fullName] as T;
            if( result == null && throwIfNotFound )
            {
                var existing = @this[fullName];
                if( existing == null )
                {
                    throw new CKException( "Unable to find object '{0}' (while looking for a driver of type '{1}').", fullName, typeof( T ).Name );
                }
                else
                {
                    throw new CKException( "Object named '{0}' should be a driver of type '{1}' but its type is '{2}'.", fullName, typeof( T ).Name, existing.GetType().Name );
                }
            }
            return result;
        }
    }
}
