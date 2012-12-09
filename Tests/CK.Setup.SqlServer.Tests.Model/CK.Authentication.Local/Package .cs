using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup;
using CK.Setup.SqlServer;

namespace CK.Authentication.Local
{
    [SqlPackage( Database = typeof(SqlDefaultDatabase), Schema="CK", ResourceType = typeof( Package ), ResourcePath = "Res" ), Versions( "1.0.0" )]
    public class Package : SqlPackage
    {
        [AmbientContract]
        public UserHome UserHome { get; protected set; }
    }
}
