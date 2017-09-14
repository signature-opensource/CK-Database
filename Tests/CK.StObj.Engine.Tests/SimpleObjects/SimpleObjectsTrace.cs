#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\SimpleObjectsTrace.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Reflection;
using CK.Core;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    public static class SimpleObjectsTrace
    {
        public static void LogMethod( MethodBase m )
        {
            TestHelper.Monitor.Trace( $"{m.DeclaringType.Name}.{m.Name} {(m.IsVirtual ? "(virtual)" : "")} has been called." );
        }
    }
}
