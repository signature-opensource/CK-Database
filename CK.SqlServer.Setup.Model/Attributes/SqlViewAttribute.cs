#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\Attributes\SqlViewAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlViewAttribute : SqlPackageAttributeBase
    {
        public SqlViewAttribute( string viewName )
            : base( "CK.SqlServer.Setup.SqlViewAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            ViewName = viewName;
        }

        public string ViewName { get; set; }
    }

}
