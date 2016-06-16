using CK.SqlServer.Parser;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime.SqlZonePackage.Zone
{
    public class GroupHome
    {
        public void CmdDemoCreate( SqlTransformContext ctx )
        {
            // "add parameter @SecurityZoneId int = 0  before @GroupName;"
            SqlStoredProcedure p = (SqlStoredProcedure)((SqlProcedureItem)ctx.Item).FinalStatement;
        }
    }

}
