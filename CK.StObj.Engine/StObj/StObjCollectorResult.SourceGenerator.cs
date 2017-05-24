using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Collections;
using System.IO;
using CK.CodeGen;

namespace CK.Setup
{
    static class XXXXX
    {
        public static ClassBuilder DefineReadOnlyProperty(this ClassBuilder @this, string frontModifiers, string returnType, string propertyName )
        {
            var m = @this.DefineMethod( frontModifiers, propertyName );
            m.ReturnType = returnType;
            m.Body.Append( ' ' );
            return @this;
        }
    }

    public partial class StObjCollectorResult
    {
        public bool GenerateSourceCode( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder )
        {
            try
            {
                var rootSource = new NamespaceBuilder( "CK.StObj" );
                rootSource.Usings.Build()
                    .Add( "System" )
                    .Add( "CK.Core" )
                    .Add( "System.Collections.Generic" );
                ClassBuilder cB = rootSource.DefineClass( StObjContextRoot.RootContextTypeName );
                cB.Interfaces.Add( nameof( IStObjMap ) );
                cB.DefineReadOnlyProperty( "public", "IContextualStObjMap", "Default" );
                cB.DefineReadOnlyProperty( "public", "IReadOnlyCollection<IContextualStObjMap>", "Contexts" );
                cB.DefineMethod( "public", "IContextualStObjMap", "FindContext" );

                return true;
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex, "While generating final assembly '{0}' from source code.", _finalAssembly.SaveFileName );
                return false;
            }
        }
    }
}
