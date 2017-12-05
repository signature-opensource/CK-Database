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
using CK.Setup;
using System.Xml.Linq;
using System.Diagnostics;

namespace CKSetup
{
    /// <summary>
    /// Run command implementation.
    /// </summary>
    static public class CommandRun
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Resolves Model/Setup dependencies accross one ore more directories and run a command in a consolidated folder.";
            c.StandardConfiguration( true );
            ConfigurationPathArgument pathArg = c.ConfigurationPathArgument();
            StorePathOptions storePath = c.AddStorePathOption();
            RemoteUriOptions remoteOpt = c.AddRemoteUriOptions();

            c.OnExecute( monitor =>
            {
                if( !pathArg.Initialize( monitor ) ) return Program.RetCodeError;
                if( !storePath.Initialize( monitor ) ) return Program.RetCodeError;
                if( !remoteOpt.Initialize( monitor ) ) return Program.RetCodeError;

                ClientRemoteStore remote = remoteOpt.Url != null
                                            ? new ClientRemoteStore( remoteOpt.Url, remoteOpt.ApiKey )
                                            : null;

                using( RuntimeArchive store = RuntimeArchive.OpenOrCreate( monitor, storePath.StorePath ) )
                {
                    if( store == null ) return Program.RetCodeError;

                    return Facade.DoRun( monitor,
                                         store,
                                         pathArg.Configuration,
                                         remote )
                            ? Program.RetCodeSuccess
                            : Program.RetCodeError;
                }
            } );
        }
    }
}
