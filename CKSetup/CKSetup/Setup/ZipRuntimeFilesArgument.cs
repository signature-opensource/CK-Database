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
    class ZipRuntimeFilesArgument
    {
        public ZipRuntimeFilesArgument( CommandArgument arg )
        {
            CommandArgument = arg;
        }

        public CommandArgument CommandArgument { get; }

        public struct RuntimeFile
        {
            public RuntimeFile( IReadOnlyList<BinFileInfo> all, BinFileInfo f )
            {
                AllBinFiles = all;
                File = f;
            }

            public readonly IReadOnlyList<BinFileInfo> AllBinFiles;

            public readonly BinFileInfo File;
        }

        public IReadOnlyList<RuntimeFile> Files { get; private set; }
        
        public bool Initialize( ConsoleMonitor m )
        {
            var result = new List<RuntimeFile>();
            using( m.OpenDebug().Send( "Discovering files." ) )
            {
                try
                {
                    var directories = new Dictionary<string, IReadOnlyList<BinFileInfo>>();
                    foreach( var f in CommandArgument.Values )
                    {
                        if( !f.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) )
                        {
                            return m.SendError( $"File '{f}' must be have .dll suffix." ) == Program.RetCodeSuccess;
                        }
                        string fullPath = Path.GetFullPath( f );
                        if( !File.Exists( fullPath ) )
                        {
                            return m.SendError( $"'{fullPath}' not found." ) == Program.RetCodeSuccess;
                        }
                        string binPath = Path.GetDirectoryName( fullPath );
                        IReadOnlyList<BinFileInfo> files;
                        if( !directories.TryGetValue( binPath, out files ) )
                        {
                            files = BinFileInfo.ReadBinFolder( m, binPath );
                            directories.Add( binPath, files );
                        }
                        var theOne = files.FirstOrDefault( x => x.FullPath == fullPath );
                        if( theOne?.CKVersion?.Version?.IsValid != true )
                        {
                            return m.SendError( $"'{f}' must have a standard informational version." ) == Program.RetCodeSuccess;
                        }
                        if( string.IsNullOrEmpty( theOne?.RawTargetFramework ) )
                        {
                            return m.SendError( $"'{f}' must have a TargetFramework attribute." ) == Program.RetCodeSuccess;
                        }
                        result.Add( new RuntimeFile( files, theOne ) );
                    }
                }
                catch( Exception ex )
                {
                    m.Fatal().Send( ex );
                    return false;
                }
            }
            if( result.Count == 0 )
            {
                return m.SendError( "No valid runtime files found." ) == Program.RetCodeSuccess;
            }
            Files = result;
            return true;
        }



    }
}
