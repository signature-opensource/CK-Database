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
            c.Description = "Manages the runtime store.";
            c.StandardConfiguration( withMonitor: false );
            c.OnExecute( () => { c.ShowHelp(); return Program.RetCodeHelp; } );
            c.Command( "add", DefineAdd );
            c.Command( "clear", DefineClear );
            c.Command( "push", DefinePush );
        }

        public static void DefineAdd( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName + ".Add";
            c.Description = "Adds components (Engine, Runtime or Model) to the store.";
            c.StandardConfiguration( true );

            StoreBinFolderArguments toAdd = c.AddStoreDirArguments( "Components to add to the store." );
            StorePathOptions storePath = c.AddStorePathOption();

            c.OnExecute( monitor =>
            {
                if( !toAdd.Initialize( monitor ) ) return Program.RetCodeError;
                if( !storePath.Initialize( monitor, null ) ) return Program.RetCodeError;
                using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( monitor, storePath.StorePath ) )
                {
                    if( zip == null ) return Program.RetCodeError;
                    if( !zip.CreateLocalImporter().AddComponent( toAdd.Folders ).Import() )
                    {
                        return Program.RetCodeError;
                    }
                }
                return Program.RetCodeSuccess;
            } );
        }

        static void DefineClear( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName + ".Clear";
            c.Description = "Clears the store.";
            c.StandardConfiguration( true );
            StorePathOptions zipFile = c.AddStorePathOption();

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

        static void DefinePush( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName + ".Push";
            c.Description = "Push local components to a remote store.";
            c.StandardConfiguration( true );
            StorePathOptions storePath = c.AddStorePathOption();
            StorePushOptions pushOptions = c.AddStorePushOptions();

            c.OnExecute( monitor =>
            {
                if( !storePath.Initialize( monitor, null ) ) return Program.RetCodeError;
                if( !pushOptions.Initialize( monitor ) ) return Program.RetCodeError;
                using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( monitor, storePath.StorePath ) )
                {
                    if( zip == null ) return Program.RetCodeError;
                    if( !zip.PushComponents( comp => true, pushOptions.Url, pushOptions.ApiKey ) ) return Program.RetCodeError;
                }
                return Program.RetCodeSuccess;
            } );
        }

    }
}
