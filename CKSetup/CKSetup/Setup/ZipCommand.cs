using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using CK.Text;
using Mono.Cecil;
using System.Linq;
using Mono.Collections.Generic;

namespace CKSetup
{
    static partial class ZipCommand
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName+".Zip";
            c.Description = "Manages the runtime zip file.";
            c.StandardConfiguration( withMonitor: false );
            c.OnExecute( () => { c.ShowHelp(); return Program.RetCodeHelp; } );
            c.Command( "add", DefineAdd );
            c.Command( "clear", DefineClear );
        }

        static void DefineAdd( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Adds a runtime file and its local dependencies to the zip runtime file.";
            c.StandardConfiguration( true );
            ZipRuntimeFilesArgument toAdd = c.AddZipRuntimeFilesArgument( "Runtime files to add to the runtime zip." );
            ZipRuntimeFileOption zipFile = c.AddZipRuntimeFileOption();

            c.OnExecute( monitor =>
            {
                if( !toAdd.Initialize( monitor ) ) return Program.RetCodeError;
                if( !zipFile.Initialize( monitor, null ) ) return Program.RetCodeError;
                using( ZipRuntimeArchive zip = ZipRuntimeArchive.OpenOrCreate( monitor, zipFile.ZipRuntimeFile ) )
                {
                    if( zip == null ) return Program.RetCodeError;
                    foreach( var f in toAdd.Files )
                    {
                        if( !zip.AddOrUpdateRuntime( f.File ) ) return Program.RetCodeError;
                    }
                }
                return Program.RetCodeSuccess;
            } );
        }

        static void DefineClear( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Clears the zip runtime file.";
            c.StandardConfiguration( true );
            ZipRuntimeFileOption zipFile = c.AddZipRuntimeFileOption();

            c.OnExecute( monitor =>
            {
                if( !zipFile.Initialize( monitor, null ) ) return Program.RetCodeError;
                using( ZipRuntimeArchive zip = ZipRuntimeArchive.OpenOrCreate( monitor, zipFile.ZipRuntimeFile ) )
                {
                    if( zip == null || !zip.Clear() ) return Program.RetCodeError;
                }
                return Program.RetCodeSuccess;
            } );
        }

    }
}
