using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using Microsoft.Extensions.CommandLineUtils;

namespace CkDbSetup
{
    static partial class Program
    {
        const int EXIT_SUCCESS = 0;
        const int EXIT_ERROR = 1;
        const int EXIT_HELP = 2;

        static AnsiConsole Output;
        static AnsiConsole Error;

        static readonly string DefaultGeneratedAssemblyName = @"CK.StObj.AutoAsssembly";

        static int Main( string[] args )
        {
            Output = AnsiConsole.GetOutput( true );
            Error = AnsiConsole.GetError( true );

            var app = new CommandLineApplication
            {
                Name = "CkDbSetup",
                Description = $"Database setup utilities for CK.Database assemblies",
                FullName = "CK.Database setup console utility",
                LongVersionGetter = GetLongVersion,
                ShortVersionGetter = GetShortVersion
            };

            PrepareHelpOption( app );
            PrepareVersionOption( app );

            app.Command( "setup", SetupCommand );
            app.Command( "backup", BackupCommand );
            app.Command( "restore", RestoreCommand );

            app.OnExecute( () =>
            {
                app.ShowHelp();
                return EXIT_HELP;
            } );

            try
            {
                return app.Execute( args );
            }
            finally
            {
                Console.ResetColor();
                DisposeLogFileWriter();
            }
        }

        static CommandOption PrepareHelpOption( CommandLineApplication c ) => c.HelpOption( "-?|-h|--help" );

        static CommandOption PrepareVersionOption( CommandLineApplication c ) => c.VersionOption( "-v|--version", GetShortVersion(), GetLongVersion() );

        static string GetShortVersion( bool showRevisionNumber )
        {
            var a = Assembly.GetExecutingAssembly();
            var n = a.GetName();
            Version v = n.Version;

            return showRevisionNumber ? $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}" : $"{v.Major}.{v.Minor}.{v.Build}";
        }
        static string GetShortVersion() => GetShortVersion( false );

        static string GetLongVersion()
        {
            var a = Assembly.GetExecutingAssembly();
            AssemblyInformationalVersionAttribute aiv = Attribute
                .GetCustomAttribute( a, typeof(AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute;

            string version;

            if( aiv == null )
            {
                version = GetShortVersion( true );
            }
            else
            {
                version = aiv.InformationalVersion;
            }

            return $"{version} (CK.Database {GetCkDatabaseVersion()})".Trim();
        }

        static string GetCkDatabaseVersion()
        {
            var a = Assembly.GetAssembly( typeof( CK.Setup.SetupEngineConfiguration ) );

            AssemblyInformationalVersionAttribute aiv = Attribute
                .GetCustomAttribute( a, typeof(AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute;

            if( aiv == null ) { return GetShortVersion( true ); }

            return aiv.InformationalVersion;
        }
    }
}
