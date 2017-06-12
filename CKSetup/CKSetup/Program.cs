using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CKSetup
{
    class Program
    {
        public const int RetCodeSuccess = 0;
        public const int RetCodeError = 1;
        public const int RetCodeHelp = 2;

        static readonly AnsiConsole Output = AnsiConsole.GetOutput( true );
        static readonly AnsiConsole Error = AnsiConsole.GetError( true );

        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "CKSetup",
                Description = $"Database setup utilities for CK.Database assemblies",
                FullName = "CK.Database setup console utility",
            };
            app.StandardConfiguration( withMonitor: false );
            app.OnExecute( () => { app.ShowHelp(); return RetCodeHelp; } );

            app.Command( "backup", BackupCommand.Define );
            app.Command( "restore", RestoreCommand.Define );

            return app.Execute( args );
        }
    }
}