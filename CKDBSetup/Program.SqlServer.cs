using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using CK.Core;

namespace CKDBSetup
{
    static partial class Program
    {

        private static string GetDefaultServerBackupPath( IActivityMonitor m, SqlConnection c )
        {
            return ExecXpInstanceRegread( m, c, "HKEY_LOCAL_MACHINE", @"Software\Microsoft\MSSQLServer\MSSQLServer", "BackupDirectory" );
        }
        private static string GetDefaultServerDataPath( IActivityMonitor m, SqlConnection c )
        {
            using( SqlCommand cmd = new SqlCommand( "select serverproperty('InstanceDefaultDataPath')", c ) )
            {
                return (string)cmd.ExecuteScalar();
            }
        }
        private static string GetDefaultServerLogPath( IActivityMonitor m, SqlConnection c )
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

        private static readonly Regex SqlServerIdentifierRegex = new Regex(@"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$");

        private static bool ValidateIdentifier( string name ) => SqlServerIdentifierRegex.IsMatch( name );
    }
}
