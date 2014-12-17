#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Engine\Labo\Copy of Tests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using CK.Core;
//using CK.SqlServer.Setup;

//namespace CK.Setup
//{
//    public class SqlPackage : IAmbientContractDefiner
//    {
//    }

//    public class SqlTable : IAmbientContractDefiner
//    {
//        public SqlTable()
//        {
//            Schema = "CK";
//            TableName = "t" + GetType().Name;
//        }
//        public SqlDatabase Database { get; set; }
//        public string Schema { get; set; }
//        public string TableName { get; set; }
//    }

//    [SqlPackage( "1.0.0", FullName = "Package.Resource", Database = typeof( DefaultDatabase ), Schema = "CK" )]
//    public class ResourcePackage : SqlPackage
//    {
//    }

//    [SqlTable( "1.0.0", FullName = "Table.Res", Package = typeof( ResourcePackage ), Database = typeof( ResourceDatabase ) )]
//    public class Res : SqlTable
//    {
//    }

//    [SqlTable( "1.0.0", Package = typeof( ResourcePackage ) )]
//    class ResName : SqlTable
//    {
//    }

//    [SqlTable( "1.0.0", Package = typeof( ResourcePackage ) )]
//    class ResTitle : SqlTable
//    {
//    }

//    [SqlPackage( "1.0.0" )]
//    class MultiCulturePackage : SqlPackage
//    {
//    }

//    [SqlTable( "1.0.0", Package = typeof( MultiCulturePackage ) )]
//    public class MCCultureInfo : SqlTable
//    {
//    }

//    [SqlTable( "1.0.0", Package = typeof( MultiCulturePackage ), Requires="Un truc..., et un autre" )]
//    public class MCXLCID : SqlTable
//    {
//    }

//    [SqlTable( "1.0.0", Package = typeof( MultiCulturePackage ) )]
//    public class MCXLCIDMap : SqlTable
//    {
//    }

//    [SqlPackage( "2.5.31.18" )]
//    class MCResourcePackage : SqlPackage
//    {
//    }

//    [SqlPackage( "1.0.0" )]
//    class ActorPackage : SqlPackage
//    {
//    }

//    [SqlTable( "1.0.0" )]
//    class Actor : SqlTable
//    {
//    }

//    [SqlTable( "1.0.0" )]
//    class User : SqlTable
//    {
//    }

//    [SqlTable( "1.0.0", TableName="tGroup" )]
//    class GroupBase : SqlTable
//    {
//        Column GroupId { get; private set; }
//        Column GroupName { get; private set; }
//        Column ResId { get; private set; }

//        void Construct( Actor actor, Res res )
//        {
//            GroupId = new Column( this, "GroupId", actor.PrimaryKey.ColumnType );
//            GroupName = new Column( this, "GroupName", "varchar(32)" );
//            ResId = new Column( this, "ResId", res.PrimaryKey.ColumnType );
//        }
//    }

//    [SqlTable( "1.0.0" )]
//    class SecurityZone : SqlTable
//    {
//        Column SecurityZoneId { get; private set; }
//        Column ZoneName { get; private set; }

//        void Construct( GroupBase group )
//        {
//            SecurityZoneId = new Column( this, "SecurityZoneId", group.PrimaryKey.ColumnType );
//            ZoneName = new Column( this, "ZoneName", "varchar(12)" );
//        }

//        SqlProcedure SecurityZoneCreate( int actorId, string name, [Return]out int resultId )
//        {

//        }
//    }

//    [SqlTable( "1.0.0" )]
//    class Group : GroupBase
//    {
//        Column SecurityZoneId { get; private set; }

//        void Construct( SecurityZone zone )
//        {
//            SecurityZoneId = new Column( this, "SecurityZoneId", zone.PrimaryKey.ColumnType );
//        }
//    }


//}
