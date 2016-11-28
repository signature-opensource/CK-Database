using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using CK.Core;

/*
 Supporting implicit conversion requires a not so easy refactoring.
 Currently a direct mapping is tested between SlDbType and .Net type: the association validity is a bool.
 Tu support conversion, the whole type checking should handle FromSql and ToSql (static) converter methods
 and emit calls shoumd be made to them...

Since this is not easy, this is yet to be done.

namespace SqlCallDemo
{
    public struct StringWrapper
    {
         public StringWrapper( string s )
        {
            Value = s;
        }

        public string Value { get; set; }

        public override string ToString() => Value;

        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;

        public static implicit operator StringWrapper( string s ) => new StringWrapper( s );

        static public implicit operator string( StringWrapper scopes ) => scopes.Value;

    }

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class ImplicitConversionPackage : SqlPackage
    {
        //[SqlProcedure( "sWithImplicitConversionIO" )]
        //public abstract string CallSyncScopes( ISqlCallContext ctx, byte power, SimpleScopes scopes );


    }
}

    */
