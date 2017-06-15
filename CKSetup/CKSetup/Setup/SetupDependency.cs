using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{

    public class SetupDependency
    {
        public SetupDependency( IList<CustomAttributeArgument> ctorArgs, BinFileInfo referencer )
        {
            if( ctorArgs.Count > 0 )
            {
                Name = ctorArgs[0].Value as string;
                if( Name != null && Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase ))
                {
                    Name = Name.Substring( 0, Name.Length - 4 );
                }
            }
            if( ctorArgs.Count > 1 )
            {
                Version = ctorArgs[1].Value as string;
            }
            else Version = referencer.VersionName;
            Referencer = referencer;
        }

        public BinFileInfo Referencer { get; }

        public string Name { get; }

        public string Version { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace( Name );
    }
}
