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

            app.Command( "db-backup", CommandDBBackup.Define );
            app.Command( "db-restore", CommandDBRestore.Define );
            app.Command( "setup", CommandSetup.Define );
            app.Command( "store", CommandStore.Define );

            return app.Execute( args );
        }
    }
}
