using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using NUnit.Framework;

namespace CK.Setup.StObj.Tests.SimpleObjects.LoggerInjection
{
    public class LoggerInjected : IAmbiantContract
    {
        void Construct( IActivityLogger logger, IActivityLogger anotherLogger = null )
        {
            Assert.That( logger, Is.Not.Null, "This is the Setup logger. Parameter must be exactly 'IActivityLogger logger'." );
            Assert.That( anotherLogger, Is.Null, "This is NOT the Setup logger. Since it is optional, it works." );
            logger.Trace( "Setup logger can be used by Construct method." );
        }
    }
}
