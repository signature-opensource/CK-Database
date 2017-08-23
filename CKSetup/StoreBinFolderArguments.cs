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
    class StoreBinFolderArguments
    {
        public StoreBinFolderArguments( CommandArgument arg )
        {
            CommandArgument = arg;
        }

        public CommandArgument CommandArgument { get; }

        public IReadOnlyList<BinFolder> Folders { get; private set; }
        
        public bool Initialize( ConsoleMonitor m )
        {
            var result = new List<BinFolder>();
            using( m.OpenDebug( "Discovering files." ) )
            {
                try
                {
                    foreach( var d in CommandArgument.Values )
                    {
                        var f = BinFolder.ReadBinFolder( m, d );
                        if( f == null ) return false;
                        result.Add( f );
                    }
                }
                catch( Exception ex )
                {
                    m.Fatal( ex );
                    return false;
                }
            }
            if( result.Count == 0 )
            {
                return m.SendError( "No valid runtime files found." ) == Program.RetCodeSuccess;
            }
            Folders = result;
            return true;
        }



    }
}
