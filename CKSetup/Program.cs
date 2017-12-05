using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace CKSetup
{
    class Program
    {
        public const int RetCodeSuccess = 0;
        public const int RetCodeError = 1;
        public const int RetCodeHelp = 2;

        static int Main( string[] args )
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo( "en-US" );
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

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
            app.Command( "run", CommandRun.Define );
            app.Command( "setup", CommandSetup.Define );
            app.Command( "store", CommandStore.Define );

            return app.Execute( args );
        }
    }
}
