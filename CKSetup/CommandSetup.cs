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
    /// Setup command implementation.
    /// </summary>
    static public class CommandSetup
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Sets up the given CK.Database assemblies in a SQL Server instance, using the given SQL Server connection string to connect, and generates a structure (StObjMap) assembly.";
            c.StandardConfiguration( true );
            ConnectionStringArgument connectionArg = c.AddConnectionStringArgument();
            BinPathsOption binPaths = c.AddBinPathsOption( "Path to the directories containing the assembly files, and in which the generated assembly will be saved. Defaults to the current working directory." );
            StorePathOptions storePath = c.AddStorePathOption();

            var generatedAssemblyNameOpt = c.Option( "-n|--generatedAssemblyName",
                                                     $"Assembly name, and file name (without the .dll suffix) of the generated assembly. Defaults to 'CK.StObj.AutoAssembly'.",
                                                     CommandOptionType.SingleValue );
            var sourceGenerationOpt = c.Option(
                "-sg|--sourceGeneration",
                $"Use the new code source generation (instead of IL emit).",
                CommandOptionType.NoValue
                );

            c.OnExecute( monitor =>
            {
                if( !connectionArg.Initialize( monitor ) ) return Program.RetCodeError;
                if( !binPaths.Initialize( monitor ) ) return Program.RetCodeError;
                if( !storePath.Initialize( monitor, binPaths.BinPaths[0] ) ) return Program.RetCodeError;

                if( binPaths.BinPaths.Count > 1 )
                {
                    throw new NotImplementedException( "Multi Bin path Setup is not yet implemented." );
                }
                using( RuntimeArchive store = RuntimeArchive.OpenOrCreate( monitor, storePath.StorePath ) )
                {
                    if( store == null ) return Program.RetCodeError;
                    return Facade.DoSetup(
                            monitor,
                            binPaths.BinPaths[0],
                            store,
                            connectionArg.TargetConnectionString,
                            generatedAssemblyNameOpt.Value(),
                            sourceGenerationOpt.HasValue() )
                            ? Program.RetCodeSuccess
                            : Program.RetCodeError;
                }
            } );
        }
    }
}
