using CSemVer;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    static class CecilExtensions
    {
        static public string GetAssemblyInformationalVersion( this AssemblyDefinition @this )
        {
            var attr = @this.CustomAttributes.FirstOrDefault( a => a.AttributeType.FullName == "System.Reflection.AssemblyInformationalVersionAttribute" );
            if( attr != null && attr.HasConstructorArguments )
            {
                return attr.ConstructorArguments[0].Value as string;
            }
            return null;
        }

        static public InformationalVersion GetInformationalVersion( this AssemblyDefinition @this ) => new InformationalVersion( @this.GetAssemblyInformationalVersion() );

    }
}
