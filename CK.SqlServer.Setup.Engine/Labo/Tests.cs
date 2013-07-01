//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using CK.Core;
//using CK.SqlServer.Setup;

//namespace CK.Setup
//{

//    public interface IColumn
//    {
//        string ColumnName { get; }
//        string ColumnType { get; }
//    }

//    public interface IMultiColumn : IReadOnlyList<IColumn>
//    {
//    }

//    class Table : IAmbientContractDefiner
//    {
//        public Column PrimaryKey { get; }
//        public MultiColumn PrimaryKeys { get; }
//    }

//    abstract class Column
//    {
//        public Column( Table table )
//        {
//            Table = table;
//        }

//        public Table Table { get; private set; }

//        public abstract string ColumnName { get; }

//        public abstract string ColumnType { get; }
//    }

//    class DataColumn : Column
//    {
//        string _columnName;
//        string _columnType;

//        public DataColumn( Table table, string columnName, string columnType )
//            : base( table )
//        {
//            _columnName = columnName;
//            _columnType = columnType;
//        }

//        public override string ColumnName { get { return _columnName; } }

//        public override string ColumnType { get { return _columnType; } }
//    }

//    class LinkedColumn : Column
//    {
//        string _nameFormat;
//        Column _linked;

//        public LinkedColumn( Table table, Column linked, string nameFormat = "{0}" )
//            : base( table )
//        {
//            _linked = linked;
//            _nameFormat = nameFormat;
//        }

//        public override string ColumnName { get { return String.Format( _nameFormat, _linked.ColumnName ); } }

//        public override string ColumnType { get { return _linked.ColumnType; } }
//    }

//    class MultiColumn : IMultiColumn
//    {
//        IReadOnlyList<IColumn> _columns;

//        public MultiColumn( IEnumerable<IColumn> columns )
//        {
//            _columns = columns.ToReadOnlyList();
//        }

//        public int IndexOf( object item )
//        {
//            return _columns.IndexOf( item );
//        }

//        public IColumn this[int index]
//        {
//            get { return _columns[index]; }
//        }

//        public bool Contains( object item )
//        {
//            return _columns.Contains( item );
//        }

//        public int Count
//        {
//            get { return _columns.Count; }
//        }

//        public IEnumerator<IColumn> GetEnumerator()
//        {
//            return _columns.GetEnumerator();
//        }

//        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
//        {
//            return _columns.GetEnumerator();
//        }
//    }

//    class PKAutoIntColumn : Column
//    {
//        string _columnName;

//        public PKAutoIntColumn( Table table, string columnName, bool hasUniverse, bool hasSystem )
//            : base( table )
//        {
//            _columnName = columnName;
//        }

//        public override string ColumnName { get { return _columnName; } }

//        public override string ColumnType { get { return "int"; } }
//    }

//    public class SqlPackage : IAmbientContractDefiner
//    {
//        public SqlDatabase DefaultDatabase { get; set; }
        
//        public string DefaultSchema { get; set; }

//    }

//    [SqlPackage( "1.0.0", FullName="CK-Package.Resource", Database=typeof(DefaultDatabase) )]
//    class ResourcePackage : SqlPackage
//    {
//    }

//    [Table( "1.0.0", Package = typeof( ResourcePackage ) )]
//    class Res : Table
//    {
//        PKAutoIntColumn ResId { get; private set; }

//        void Construct()
//        {
//            ResId = new PKAutoIntColumn( this, "ResId", true, true );
//        }
//    }

//    [Table( "1.0.0", Package = typeof( ResourcePackage ) )]
//    class ResName : Table
//    {
//        Column ResId { get; private set; }
//        Column ResName { get; private set; }

//        void Construct( Res res )
//        {
//            ResId = new LinkedColumn( this, res.PrimaryKey );
//            ResName = new DataColumn( this, "ResName", "varchar(96)" );
//        }
//    }

//    [Table( "1.0.0", Package = typeof( ResourcePackage ) )]
//    class ResTitle : Table
//    {
//        Column ResId { get; private set; }
//        Column Value { get; private set; }

//        void Construct( Res res )
//        {
//            ResId = new LinkedColumn( this, res.PrimaryKey );
//            Value = new DataColumn( this, "Value", "nvarchar(400)" );
//        }
//    }

//    [SqlPackage( "1.0.0" )]
//    class MultiCulturePackage : Package
//    {
//    }

//    [Table( "1.0.0", Package = typeof( MultiCulturePackage ) )]
//    public class MCCultureInfo : Table
//    {
//        public Column LCID { get; private set; }
//        public Column EnglishName { get; private set; }

//        void Construct()
//        {
//            LCID = new Column( this, "LCID", "smallint" );
//            EnglishName = new Column( this, "EnglishName", "nvarchar(64)" );
//        }
//    }

//    [Table( "1.0.0", Package = typeof( MultiCulturePackage ) )]
//    public class MCXLCID : Table
//    {
//        Column XLCID { get; private set; }
//        Column BestLCID { get; private set; }

//        void Construct( MCCultureInfo cInfo )
//        {
//            XLCID = new Column( this, "XLCID", cInfo.PrimaryKey.ColumnType );
//            BestLCID = new Column( this, "BestLCID", cInfo.PrimaryKey.ColumnType );
//        }

//    }

//    [Table( "1.0.0", Package = typeof( MultiCulturePackage ) )]
//    public class MCXLCIDMap : Table
//    {
//        Column XLCID { get; private set; }
//        Column LCID { get; private set; }
//        Column Idx { get; private set; }

//        void Construct( MCCultureInfo cInfo, MCXLCID xlcid )
//        {
//            XLCID = new Column( this, "XLCID", xlcid.PrimaryKey.ColumnType );
//            LCID = new LinkedColumn( this, cInfo.LCID );
//            Idx = new Column( this, "Idx", "smallint" );
//        }

//    }

//    [SqlPackage( "2012.05.31.18" )]
//    class MCResourcePackage : Package
//    {
//        Column ResTitleLCID { get; private set; }

//        void Construct( ResTitle resTitle, MCCultureInfo cInfo )
//        {
//            ResTitleLCID = new Column( resTitle, "LCID", cInfo.PrimaryKey.ColumnType );
//        }
//    }

//    [SqlPackage( "1.0.0" )]
//    class ActorPackage : Package
//    {
//    }

//    [Table( "1.0.0" )]
//    class Actor : Table
//    {
//        PKAutoIntColumn ActorId { get; private set; }

//        void Construct()
//        {
//            ActorId = new PKAutoIntColumn( this, "ActorId", true, true );
//        }
//    }

//    [Table( "1.0.0" )]
//    class User : Table
//    {
//        Column UserId { get; private set; }
//        Column UserName { get; private set; }
//        Column Email { get; private set; }

//        void Construct( Actor actor )
//        {
//            UserId = new Column( this, "UserId", actor.PrimaryKey.ColumnType );
//            UserName = new Column( this, "UserName", "nvarchar(64)" );
//            Email = new Column( this, "Email", "nvarchar(96)" );
//        }
//    }

//    [Table( "1.0.0" )]
//    class GroupBase : Table
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

//    [Table( "1.0.0" )]
//    class SecurityZone : Table
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

//    [Table( "1.0.0" )]
//    class Group : GroupBase
//    {
//        Column SecurityZoneId { get; private set; }

//        void Construct( SecurityZone zone )
//        {
//            SecurityZoneId = new Column( this, "SecurityZoneId", zone.PrimaryKey.ColumnType );
//        }
//    }


//}
