using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Code.Cake;
using CodeCake;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCakeBuilder
{
    public class RunCKDBSetup : CodeCakeHost
    {
        public RunCKDBSetup()
        {
            string configuration = "Debug";

            var exe = Cake.File( $@"CKDBSetup\bin\{configuration}\CKDBSetup.exe" ).Path.MakeAbsolute( Cake.Environment );
            var callDemoPath = Cake.Directory( $@"Tests\SqlCallDemo\SqlCallDemo\bin\{configuration}\net461\win" );

            string c = Environment.GetEnvironmentVariable( "CK_DB_TEST_MASTER_CONNECTION_STRING" );
            if( c == null ) c = System.Configuration.ConfigurationManager.AppSettings["CK_DB_TEST_MASTER_CONNECTION_STRING"];
            if( c == null ) c = "Server=.;Database=master;Integrated Security=SSPI";
            var csB = new SqlConnectionStringBuilder( c );
            csB.InitialCatalog = "CKDB_TEST_SqlCallDemo";
            var dbCon = csB.ToString();

            var cmdLineIL = $@"{exe.FullPath} setup ""{dbCon}"" -ra ""SqlCallDemo"" -n ""GenByCKDBSetup"" -p ""{callDemoPath}""";
            int result = Cake.RunCmd( cmdLineIL );
            if( result != 0 ) throw new Exception( "CKDSetup.exe failed for IL generation." );

            result = Cake.RunCmd( cmdLineIL + " -sg" );
            if( result != 0 ) throw new Exception( "CKDSetup.exe failed for Source Code generation." );
        }
    }
}
