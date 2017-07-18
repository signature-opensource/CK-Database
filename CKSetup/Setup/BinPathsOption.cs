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
    class BinPathsOption
    {
        public BinPathsOption( CommandOption arg )
        {
            CommandOption = arg;
        }

        public CommandOption CommandOption { get; }

        public IReadOnlyList<string> BinPaths { get; private set; }
        
        public bool Initialize( ConsoleMonitor m )
        {
            if( !CommandOption.HasValue() )
            {
                m.Info().Send( "No path to the bin folder is specified. Using current directory." );
                BinPaths = new[] { Environment.CurrentDirectory };
            }
            else
            {
                var result = new List<string>();
                foreach( var p in CommandOption.Values )
                {
                    var full = Path.GetFullPath( p );
                    if( !Directory.Exists( full ) )
                    {
                        return m.SendErrorAndDisplayHelp( $"Path '{full}' does not exist." ) == Program.RetCodeSuccess;
                    }
                    result.Add( full );
                }
                BinPaths = result;
            }
            return true;
        }



    }
}
