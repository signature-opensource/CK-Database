using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace CkDbSetup
{
    class Program
    {
        static int Main( string[] args )
        {
            var app = new CommandLineApplication
            {
                Name = "CkDbSetup",
                Description = "Database setup utilities for CK.Database assemblies",
                FullName = "CK.Database setup console utility"
            };

            app.OnExecute( () =>
            {
                app.ShowHelp();
                return 2;
            } );

            return app.Execute( args );
        }
    }
}
