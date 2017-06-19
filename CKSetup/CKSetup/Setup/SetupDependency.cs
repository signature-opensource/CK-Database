using CSemVer;
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
        public SetupDependency( BinFileInfo engine )
        {
            Source = engine;
            IsEngine = true;
        }

        public SetupDependency( bool isModel, IList<CustomAttributeArgument> ctorArgs, BinFileInfo modelOrRuntime )
        {
            IsModel = isModel;
            if( ctorArgs.Count > 0 )
            {
                UseName = ctorArgs[0].Value as string;
                if( UseName != null && UseName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase ))
                {
                    UseName = UseName.Substring( 0, UseName.Length - 4 );
                }
            }
            if( ctorArgs.Count > 1 )
            {
                string v = (string)ctorArgs[1].Value;
                UseVersion = CSVersion.TryParse( v );
                if( UseVersion == null )
                {
                    throw new ArgumentException( $"{modelOrRuntime.Name.Name} has an invalid version '{v}' in its {(IsModel ? "IsModelThatUsesRuntime": "IsRuntimeThatUsesEngine")} attribute." );
                }
            }
            Source = modelOrRuntime;
        }

        public bool IsEngine { get; }

        public bool IsRuntime => !IsModel;

        public bool IsModel { get; }

        public BinFileInfo Source { get; }

        public string UseName { get; }

        public CSVersion UseVersion { get; }
    }
}
