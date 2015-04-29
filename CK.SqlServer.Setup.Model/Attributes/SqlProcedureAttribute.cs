#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\Attributes\SqlProcedureAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection.Emit;
using System.Reflection;

namespace CK.SqlServer.Setup
{
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
    public class SqlProcedureAttribute : SqlObjectItemMemberAttributeBase
    {
        public SqlProcedureAttribute( string procedureName )
            : base( procedureName, "CK.SqlServer.Setup.SqlProcedureAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
        }


        public ExecutionType ExecuteAs { get; set; }
    }

    public enum ExecutionType
    {
        Unknown,
        ExecuteNonQuery,
        ExecuteScalar,
        ExecuteIndependentReader,
        ExecuteXmlReader
    }
}
