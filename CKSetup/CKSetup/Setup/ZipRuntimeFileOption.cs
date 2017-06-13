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
    class ZipRuntimeFileOption
    {
        public ZipRuntimeFileOption( CommandOption arg )
        {
            CommandOption = arg;
        }

        public CommandOption CommandOption { get; }

        public string ZipRuntimeFile { get; private set; }
        
        /// <summary>
        /// Initializes this option.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="binPath">Optional binary path.</param>
        /// <returns>True on success, false on error.</returns>
        public bool Initialize( ConsoleMonitor m, string binPath )
        {
            ZipRuntimeFile = CommandOption.Value();
            if( string.IsNullOrEmpty( ZipRuntimeFile ) )
            {
                if( binPath != null )
                {
                    ZipRuntimeFile = FindZipRuntimeFrom( binPath );
                    if( string.IsNullOrEmpty( ZipRuntimeFile ) )
                    {
                        ZipRuntimeFile = FindZipRuntimeFrom( AppContext.BaseDirectory );
                    }
                }
                if( string.IsNullOrEmpty( ZipRuntimeFile ) )
                {
                    return binPath != null
                            ? m.SendErrorAndDisplayHelp( $"Unable to locate Zip runtime file above '{binPath}' or {AppContext.BaseDirectory}." ) == Program.RetCodeSuccess
                            : m.SendErrorAndDisplayHelp( $"Unable to locate Zip runtime file above {AppContext.BaseDirectory}." ) == Program.RetCodeSuccess;
                }
            }
            else 
            {
                if( !File.Exists( ZipRuntimeFile ) ) return m.SendErrorAndDisplayHelp( $"The provided Zip runtime file '{ZipRuntimeFile}' does not exist." ) == Program.RetCodeSuccess;
            }
            m.Info().Send( $"Using Zip runtime file: {ZipRuntimeFile}" );
            return true;
        }

        string FindZipRuntimeFrom( string dir )
        {
            while( !string.IsNullOrEmpty( dir ) )
            {
                string test = Path.Combine( dir, "CKSetup.Runtime.zip" );
                if( File.Exists( test ) ) return test;
                dir = Path.GetDirectoryName( dir );
            }
            return null;
        }

    }
}
