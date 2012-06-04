using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.SqlServer
{
    public class Database : PackageBase
    {
        public static readonly string DefaultDatabaseName = "db";

        public Database()
            : base( "Database" )
        {
            Name = DefaultDatabaseName;
        }

        public string Name { get; set; }

        protected override string GetSetupDriverTypeName()
        {
            return typeof( DatabaseSetupDriver ).AssemblyQualifiedName;
        }

        protected override string GetFullName()
        {
            return Name;
        }
    }
}
