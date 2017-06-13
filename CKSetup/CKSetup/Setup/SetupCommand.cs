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
    static partial class SetupCommand
    {
        public static void Define( CommandLineApplication c )
        {
            c.FullName = c.Parent.FullName;
            c.Description = "Sets up the given CK.Database assemblies in a SQL Server instance, using the given SQL Server connection string to connect, and generates a structure (StObjMap) assembly.";
            c.StandardConfiguration( true );
            ConnectionStringArgument connectionArg = c.AddConnectionStringArgument();
            BinPathsOption binPaths = c.AddBinPathsOption( "Path to the directory containing the assembly files, and in which the generated assembly will be saved. Defaults to the current working directory." );
            ZipRuntimeFileOption zipFile = c.AddZipRuntimeFileOption();

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
                if( !zipFile.Initialize( monitor, binPaths.BinPaths[0] ) ) return Program.RetCodeError;

                if( binPaths.BinPaths.Count > 1 )
                {
                    throw new NotImplementedException( "Multi Bin path Setup is not yet implemented." );
                }
                else
                {
                    string binPath = binPaths.BinPaths[0];
                    var binFiles = BinFileInfo.ReadBinFolder( monitor, binPath );
                    var setupDependencies = binFiles.SelectMany( b => b.SetupDependencies );
                    using( ZipRuntimeArchive zip = ZipRuntimeArchive.OpenOrCreate( monitor, zipFile.ZipRuntimeFile ) )
                    {
                        if( zip == null ) return Program.RetCodeError;
                        if( !zip.ExtractRuntimeDependencies( setupDependencies, binPath ) ) return Program.RetCodeError;
                        var toSetup = binFiles.Where( b => b.IsRuntimeDependencyDependent ).Select( b => b.Name.Name );
                    }
                }

                return Program.RetCodeSuccess;
            } );
        }

    }
}
