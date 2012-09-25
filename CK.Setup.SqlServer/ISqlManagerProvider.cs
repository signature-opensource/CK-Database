using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{

    public interface ISqlManagerProvider
    {
        SqlManager FindManagerByName( string logicalName );
    }
}
