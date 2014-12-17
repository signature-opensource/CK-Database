#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\SqlZonePackage\GroupHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Data.SqlClient;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{
    [SqlTable( "tGroup", Package = typeof( Package ), ResourcePath=".Group" ), Versions( "CK.tGroup-Zone=2.11.25, 2.12.10" )]
    public abstract class GroupHome : SqlActorPackage.Basic.GroupHome
    {
        void Construct( SecurityZoneHome zone )
        {
        }
        
        /// <summary>
        /// Finds or creates a Group. 
        /// </summary>
        /// <param name="securityZoneId">SecurityZone identifier of the group. Defaults to 0.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="groupIdResult">Group identifier.</param>
        /// <returns>The sql command object.</returns>
        [SqlProcedure( "sGroupCreate" )]
        public abstract SqlCommand CmdCreate( int securityZoneId, string groupName, out int groupIdResult );
        
        /// <summary>
        /// Can out parameters be optional? Yes, if they have a default value or are purely output (ie. not tagged with /*input*/ comment).
        /// </summary>
        /// <param name="c">The sql command that will be created or configured.</param>
        /// <param name="securityZoneId">SecurityZone identifier of the group. Defaults to 0.</param>
        /// <param name="groupName">Name of the group.</param>
        [SqlProcedure( "sGroupCreate" )]
        public abstract void CmdDemoCreate( ref SqlCommand c, int securityZoneId, string groupName );
    }
}
