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
    static partial class StoreCommand
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName + ".Store";
            c.Description = "Manages the runtime store file.";
            c.StandardConfiguration( withMonitor: false );
            c.OnExecute( () => { c.ShowHelp(); return Program.RetCodeHelp; } );
            c.Command( "add", DefineAdd );
            c.Command( "clear", DefineClear );
        }

        public static void DefineAdd( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName + ".Add";
            c.Description = "Adds a Component (Engine, Runtime or Model) to the store.";
            c.StandardConfiguration( true );

            StoreDirArguments toAdd = c.AddStoreDirArguments( "Components to add to the store." );
            StoreFileOption zipFile = c.AddZipRuntimeFileOption();

            c.OnExecute( monitor =>
            {
                if( !toAdd.Initialize( monitor ) ) return Program.RetCodeError;
                if( !zipFile.Initialize( monitor, null ) ) return Program.RetCodeError;
                using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( monitor, zipFile.StorePath ) )
                {
                    if( zip == null ) return Program.RetCodeError;
                    foreach( var f in toAdd.Folders )
                    {
                        if( !zip.AddComponent( f ) ) return Program.RetCodeError;
                    }
                }
                return Program.RetCodeSuccess;
            } );
        }

        static void DefineClear( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Clears the store.";
            c.StandardConfiguration( true );
            StoreFileOption zipFile = c.AddZipRuntimeFileOption();

            c.OnExecute( monitor =>
            {
                if( !zipFile.Initialize( monitor, null ) ) return Program.RetCodeError;
                using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( monitor, zipFile.StorePath ) )
                {
                    if( zip == null || !zip.Clear() ) return Program.RetCodeError;
                }
                return Program.RetCodeSuccess;
            } );
        }

    }
}
