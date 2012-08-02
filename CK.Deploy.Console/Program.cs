using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using Microsoft.Practices.Unity;
using CK.Setup.SqlServer;
using System.IO;

namespace CK.Deploy.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = MainArgs.Parse(System.Console.Out, args);

            if (a != null) 
            {
                var console = new ActivityLoggerConsoleSync();
                var logger = DefaultActivityLogger.Create().Register(console);


                using (logger.OpenGroup(LogLevel.Info, "Begin dbSetup with:"))
                {
                    logger.Info("Path: " + a.Path);
                    logger.Info("ConnectionString: " + a.ConnectionString);
                }

                using (var context = new SqlSetupContext(a.ConnectionString, logger))
                {
                    if (!context.DefaultDatabase.IsOpen()) context.DefaultDatabase.OpenOrCreate(".", "Test");
                    using (context.Logger.OpenGroup(LogLevel.Trace, "First setup"))
                    {
                        SqlSetupCenter c = new SqlSetupCenter(context);
                        c.DiscoverFilePackages(a.Path);
                        c.DiscoverSqlFiles(a.Path);
                        c.Run();
                    }
                }
            }
            else MainArgs.PrintUsage(System.Console.Out);
        }

        public class MainArgs
        {
            public string Path { get; set; }

            public string ConnectionString { get; set; }


            public static MainArgs Parse(TextWriter output, string[] args)
            {
                if (args.Length != 2 || args.Length > 0 && args[0] == "/?") return null;

                var ma = new MainArgs();
                ma.Path = args[0].Trim('"');
                ma.ConnectionString = args[1].Trim('"');

                bool isValid = true;
                if (string.IsNullOrEmpty(ma.Path)) { output.WriteLine("Invalide argument <path>"); isValid &= false; }
                if (string.IsNullOrEmpty(ma.ConnectionString)) { output.WriteLine("Invalide argument <ConnectionString>"); isValid &= false; }

                if (isValid) return ma;
                else return null;
            }

            public static void PrintUsage(TextWriter output)
            {
                output.WriteLine("CK.Deploy.Console.exe <path> <connectionString>");
                output.WriteLine();
                output.WriteLine("<path>\t root directory to process");
                output.WriteLine("<connectionString>\t Connection String");
            }

        }
    }
}
