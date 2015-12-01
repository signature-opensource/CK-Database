#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\KindOfActorPackage\Basic\GroupHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Data.SqlClient;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tGroup", Package = typeof( Package ) ), Versions( "CK.tGroup=2.12.9, 2.12.10" )]
    public abstract class GroupHome : SqlTable
    {
        void Construct( ActorHome actor )
        {
        }

        /// <summary>
        /// Finds or creates a Group. 
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="groupIdResult">Group identifier.</param>
        /// <returns>The sql command object.</returns>
        [SqlProcedure( "sGroupCreate" )]
        public abstract SqlCommand CmdCreate( string groupName, out int groupIdResult );

    }
}
