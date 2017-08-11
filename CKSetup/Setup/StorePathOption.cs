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
    class StorePathOption
    {
        /// <summary>
        /// The default store is the directory Environment.SpecialFolder.LocalApplicationData/CKSetupStore.
        /// </summary>
        public static readonly string DefaultStorePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "CKSetupStore" );

        public StorePathOption( CommandOption arg )
        {
            CommandOption = arg;
        }

        public CommandOption CommandOption { get; }

        public string StorePath { get; private set; }
        
        /// <summary>
        /// Initializes this option.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <param name="binPath">Optional binary path.</param>
        /// <returns>True on success, false on error.</returns>
        public bool Initialize( ConsoleMonitor m, string binPath )
        {
            StorePath = CommandOption.Value();
            if( string.IsNullOrEmpty( StorePath ) )
            {
                if( binPath != null )
                {
                    StorePath = FindStorePathFrom( binPath );
                    if( string.IsNullOrEmpty( StorePath ) )
                    {
                        StorePath = FindStorePathFrom( AppContext.BaseDirectory );
                    }
                }
                if( string.IsNullOrEmpty( StorePath ) )
                {
                    StorePath = DefaultStorePath;
                }
            }
            else 
            {
                if( !File.Exists( StorePath ) || !Directory.Exists( StorePath ) )
                {
                    m.Warn().Send( $"The provided store '{StorePath}' does not exist. It will be created." );
                }
            }
            m.Info().Send( $"Using store: {StorePath}" );
            return true;
        }

        string FindStorePathFrom( string dir )
        {
            while( !string.IsNullOrEmpty( dir ) )
            {
                string test = Path.Combine( dir, "CKSetupStore.zip" );
                if( File.Exists( test ) ) return test;
                test = Path.Combine( dir, "CKSetupStore" );
                if( Directory.Exists( test ) ) return test;
                dir = Path.GetDirectoryName( dir );
            }
            return null;
        }

    }
}
