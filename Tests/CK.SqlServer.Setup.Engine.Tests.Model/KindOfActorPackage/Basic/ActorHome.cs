#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\KindOfActorPackage\Basic\ActorHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Data;
using System.Data.SqlClient;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tActor", Package = typeof( Package ) ), Versions( "CK.tActor=2.12.9, 2.12.10" )]
    [SqlObjectItem( "sActorCreate" )]
    [TestAutoHeaderSP( "--Injected From ActorHome - TestAutoHeaderAttribute.", "sActorCreate, sActorGuidRefTest" )]
    public abstract class ActorHome : SqlTable
    {
        [SqlProcedure( "sActorGuidRefTest" )]
        public abstract void CmdGuidRefTest( ref SqlCommand cmd, Guid? inOnly, ref Guid? inAndOut, out string textResult );

        public void ManualCmdGuidRefTest( ref SqlCommand commandRef1, Guid? nullable1, ref Guid? nullableRef1, out string textRef1 )
        {
            SqlParameterCollection parameters;
            textRef1 = null;
            SqlCommand command = commandRef1;
            if( command != null )
            {
                parameters = command.Parameters;
            }
            else
            {
                parameters = (command = ManualdbCKsActorGuidRefTest()).Parameters;
            }
            object set1 = nullable1;
            parameters[0].Value = set1 ?? DBNull.Value;
            object set2 = nullableRef1;
            parameters[1].Value = set2 ?? DBNull.Value;
            commandRef1 = command;
        }

        internal static SqlCommand ManualdbCKsActorGuidRefTest()
        {
            SqlCommand command = new SqlCommand("CK.sActorGuidRefTest") {
                CommandType = CommandType.StoredProcedure
            };
            SqlParameterCollection parameters = command.Parameters;
            SqlParameter parameter = new SqlParameter("@InOnly", SqlDbType.UniqueIdentifier);
            parameters.Add(parameter);
            parameter = new SqlParameter("@InAndOut", SqlDbType.UniqueIdentifier) {
                Direction = ParameterDirection.InputOutput
            };
            parameters.Add(parameter);
            parameter = new SqlParameter("@TextResult", SqlDbType.NVarChar, 0x80) {
                Direction = ParameterDirection.Output
            };
            parameters.Add(parameter);
            return command;
        }
    }
}
