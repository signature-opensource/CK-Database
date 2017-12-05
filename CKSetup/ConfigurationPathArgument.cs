using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{
    class ConfigurationPathArgument
    {
        public ConfigurationPathArgument( CommandArgument arg )
        {
            CommandArgument = arg;
        }

        public CommandArgument CommandArgument { get; }

        public SetupConfiguration Configuration { get; private set; }

        public bool Initialize( ConsoleMonitor m )
        {
            string path = CommandArgument.Value;
            if( string.IsNullOrEmpty( path ) )
            {
                return m.SendErrorAndDisplayHelp( "A path to a configuration file is required." ) == Program.RetCodeSuccess;
            }
            using( m.OpenTrace( $"Reading {path}" ) )
            {
                try
                {
                    path = Path.GetFullPath( path );
                    if( !File.Exists( path ) )
                    {
                        return m.SendErrorAndDisplayHelp( $"Configuration file not found: '{path}'." ) == Program.RetCodeSuccess;
                    }
                    Configuration = new SetupConfiguration( XDocument.Load( path ) );
                }
                catch( Exception ex )
                {
                    m.Error( ex );
                    return m.SendErrorAndDisplayHelp( "Unable to load configuration file." ) == Program.RetCodeSuccess;
                }
            }
            return true;
        }
    }
}
