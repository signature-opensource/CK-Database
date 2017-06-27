using CK.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CKSetup
{
    class SqlServerHelper
    {
        private static readonly Regex SqlServerIdentifierRegex = new Regex( @"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$" );

        public static bool IsValidIdentifier( string name ) => SqlServerIdentifierRegex.IsMatch( name );

        static public string EncodeStringContent( string s )
        {
            return s == null ? string.Empty : s.Replace( "'", "''" );
        }

        public static string GetDefaultServerBackupPath( IActivityMonitor m, SqlConnection c )
        {
            return ExecXpInstanceRegread( m, c, "HKEY_LOCAL_MACHINE", @"Software\Microsoft\MSSQLServer\MSSQLServer", "BackupDirectory" );
        }

        public static string GetDefaultServerDataPath( IActivityMonitor m, SqlConnection c )
        {
            using( SqlCommand cmd = new SqlCommand( "select serverproperty('InstanceDefaultDataPath')", c ) )
            {
                return (string)cmd.ExecuteScalar();
            }
        }

        public static string GetDefaultServerLogPath( IActivityMonitor m, SqlConnection c )
        {
            using( SqlCommand cmd = new SqlCommand( "select serverproperty('InstanceDefaultLogPath')", c ) )
            {
                return (string)cmd.ExecuteScalar();
            }
        }

        private static string ExecXpInstanceRegread( IActivityMonitor m, SqlConnection c, string rootKey, string key, string valueName )
        {
            using( SqlCommand cmd = new SqlCommand( "master.dbo.xp_instance_regread", c ) )
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var p1 = cmd.Parameters.Add( "@rootkey", SqlDbType.NVarChar );
                p1.Size = 256;
                p1.Direction = ParameterDirection.Input;
                p1.Value = rootKey;

                var p2 = cmd.Parameters.Add( "@key", SqlDbType.NVarChar );
                p2.Size = 256;
                p2.Direction = ParameterDirection.Input;
                p2.Value = key;

                var p3 = cmd.Parameters.Add( "@value_name", SqlDbType.NVarChar );
                p3.Size = 256;
                p3.Direction = ParameterDirection.Input;
                p3.Value = valueName;

                var valParam = cmd.Parameters.Add( "@value", SqlDbType.NVarChar );
                valParam.Size = 256;
                valParam.Direction = ParameterDirection.Output;

                m.Trace().Send( "Executing command: {0}", cmd.CommandText );
                int r = cmd.ExecuteNonQuery();
                m.Trace().Send( "Non-query returned {0}", r );

                string value = valParam.Value is DBNull ? null : (string)valParam.Value;
                return value;
            }
        }

    }
}
