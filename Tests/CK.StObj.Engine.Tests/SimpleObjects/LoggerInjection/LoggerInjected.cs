#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjects\LoggerInjection\LoggerInjected.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests.SimpleObjects.LoggerInjection
{
    public class LoggerInjected : IAmbientContract
    {
        void Construct( IActivityMonitor monitor, IActivityMonitor anotherLogger = null )
        {
            Assert.That( monitor, Is.Not.Null, "This is the Setup monitor. Parameter must be exactly 'IActivityMonitor monitor'." );
            Assert.That( anotherLogger, Is.Null, "This is NOT the Setup monitor. Since it is optional, it works." );
            monitor.Trace().Send( "Setup monitor can be used by Construct method." );
        }
    }
}
