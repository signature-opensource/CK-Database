using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    public class StObjContextRoot
    {

        public static IContextRoot Load( string assemblyName )
        {
            return Load( Assembly.Load( assemblyName ) );
        }

        public static IContextRoot Load( Assembly a )
        {
            if( a == null ) throw new ArgumentNullException( "a" );
            return (IContextRoot)Activator.CreateInstance( a.GetType( "CK.StObj.GeneratedRootContext", true ) );
        }
    }
}
