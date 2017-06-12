using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    class BackupPathArgument
    {
        public BackupPathArgument( CommandArgument arg )
        {
            CommandArgument = arg;
        }

        public CommandArgument CommandArgument { get; }

        public string BackupPath { get; private set; }
        
        public bool Initialize( ConsoleMonitor m, SqlConnection c )
        {
            string backupPath = CommandArgument.Value;
            if( string.IsNullOrEmpty( backupPath ) )
            {
                return m.SendErrorAndDisplayHelp( "A path to the backup file is required." ) == Program.RetCodeSuccess;
            }
            m.Trace().Send( $"Path to backup: {backupPath}" );
            if( !Path.IsPathRooted( backupPath ) )
            {
                m.Info().Send( $"Path '{backupPath}' is not absolute: Using default server backup directory.", backupPath );
                string defaultBackupPath = SqlServerHelper.GetDefaultServerBackupPath( m, c );
                m.Trace().Send( $"Default server backup path: '{defaultBackupPath}'." );
                backupPath = Path.GetFullPath( Path.Combine( defaultBackupPath, backupPath ) );
            }
            m.Info().Send( $"Effective backup path: {BackupPath = backupPath}" );
            return true;
        }



    }
}
