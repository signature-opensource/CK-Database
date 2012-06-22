using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Parameter )]
    public class OutputAttribute : Attribute
    {

    }
    
    [AttributeUsage( AttributeTargets.Parameter )]
    public class ReturnAttribute : Attribute
    {

    }
}

namespace CK.Setup.SqlServer.Tests
{
    [Package( "1.0.0" )]
    public class MainPackage : Package, IAmbiantContract
    {
        SqlProcedure FirstProc( IActivityLogger logger, int i, int j, [Output,Return]int k )
        {
            return SqlObjectBuilder.LoadProcedureFromResource( logger, typeof( MainPackage ), "Res.sFirstProc.sql" );
        }
    }
}
