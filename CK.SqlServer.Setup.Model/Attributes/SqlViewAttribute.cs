using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlViewAttribute : SqlSetupableAttributeBase
    {
        public SqlViewAttribute( string viewName )
            : base( "CK.SqlServer.Setup.SqlViewAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            ViewName = viewName;
        }

        public string ViewName { get; set; }
    }

}
