using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using CK.Text;

namespace CKSetup
{
    static partial class BackupCommand
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Creates a full independent backup of a SQL Server database to a file on the SQL Server instance.";
            c.StandardConfiguration( true );
            ConnectionStringArgument connectionArg = c.AddConnectionStringArgument();
            BackupPathArgument backupPathArg = c.AddBackupPathArgument( $"Path to the new backup file on the machine hosting the SQL Server instance.{Environment.NewLine}It must be writable by the SQL Server service. Non-absolute paths will be resolved relative to the default server backup directory." );

            c.OnExecute( monitor =>
            {
                if( !connectionArg.Initialize( monitor ) ) return Program.RetCodeError;
                using( var sqlConn = connectionArg.CreateOpenedConnection( monitor, useMasterConnectionString: true ) )
                {
                    if( !backupPathArg.Initialize( monitor, sqlConn ) ) return Program.RetCodeError;
                    string q = BuildBackupQuery( connectionArg.TargetDatabaseName, backupPathArg.BackupPath );

                    return SqlServerHelper.ExecuteNonQuery( monitor, sqlConn, q );
                }
            } );
        }

        private static string BuildBackupQuery( string targetDatabaseName, string backupPath )
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( "BACKUP DATABASE " )
              .Append( targetDatabaseName )
              .Append( " TO DISK = '" )
              .Append( SqlServerHelper.EncodeStringContent( backupPath ) )
              .Append( "'" )
              .Append( " WITH COPY_ONLY, FORMAT, STATS, NAME='CKDBSetup backup', DESCRIPTION='CKDBSetup backup'" );
            return sb.ToString();
        }
    }
}
