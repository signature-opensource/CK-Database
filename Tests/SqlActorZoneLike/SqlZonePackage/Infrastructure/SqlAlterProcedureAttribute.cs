using CK.Setup;
using System;

namespace CK.SqlServer.Setup
{
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
    public sealed class SqlAlterProcedureAttribute : SetupObjectItemRefMemberAttributeBase
    {
        public SqlAlterProcedureAttribute()
            : base( "CK.SqlServer.Setup.SqlAlterProcedureAttributeImpl, SqlZonePackage.Engine" )
        {
        }

    }
}
