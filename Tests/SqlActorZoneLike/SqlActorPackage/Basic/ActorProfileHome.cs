#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\KindOfActorPackage\Basic\ActorProfileHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;

namespace SqlActorPackage.Basic;

[SqlTable( "tActorProfile", Package = typeof( Package ) ), Versions( "CK.tActorProfile=2.12.9, 2.12.10" )]
public class ActorProfileHome : SqlTable
{
    void StObjConstruct( ActorHome actor, GroupHome group )
    {
    }
}
