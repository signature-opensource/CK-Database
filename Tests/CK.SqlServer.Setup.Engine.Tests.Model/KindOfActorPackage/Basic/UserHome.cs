using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tUser", Package = typeof( Package ) ), Versions( "CK.tUser=2.12.9, 2.12.10" )]
    public abstract class UserHome : SqlTable
    {
        void Construct( ActorHome actor )
        {
        }

        [SqlProcedure( "sUserCreate" )]
        public abstract SqlCommand CmdCreate( string userName, out int userIdResult );

        [SqlProcedure( "sUserExists" )]
        public abstract void CmdExists( ref SqlCommand cmdExists, string userName, out bool existsResult );

        [SqlProcedure( "sUserExists2" )]
        public abstract void CmdExists2( ref SqlCommand cmdExists, int userPart1, int userPart2, out bool existsResult );
    }

    [SqlView( "vUser", Package = typeof( Package ) )]
    public abstract class UserView : SqlView
    {
        void Construct( UserHome userHome )
        {
        }

        //[Query( "where UserName = @UserName" )]
        //public abstract SqlCommand FindUserByName( string userName );

        //[Query( "where Email = @Email" )]
        //public abstract SqlCommand FindUserByEmail( string email );

        //[Query( "where UserName = @UserName" )]
        //public abstract SqlCommandReader<T> FindUserByName<T>( string userName );

        //[Query( "where Email = @Email" )]
        //public abstract SqlCommandReader<T> FindUserByEmail<T>( string email );

        //[Query]
        //public abstract SqlCommandReader<T> AllUsers<T>();
    }

    public class SqlCommandReader<T>
    {
    }


    public class QueryAttribute : Attribute
    {
        public QueryAttribute()
        {
        }
        public QueryAttribute( string query )
        {

        }
    }
    //public class QueryAttribute : SqlMethodForObjectItemAttributeBase
    //{
    //    public QueryAttribute()
    //        : this( "Query.Auto" )
    //    {
    //    }

    //    public QueryAttribute( string query )
    //        : base( query, "SqlActorPackage.Basic.SqlQueryAttributeImpl, SqlActorPackage.Basic" )
    //    {
    //    }
    //}
}
