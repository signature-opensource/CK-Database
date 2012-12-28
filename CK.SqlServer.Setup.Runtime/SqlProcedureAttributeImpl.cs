using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlProcedureAttributeImpl : IStObjSetupDynamicInitializer
    {
        readonly SqlProcedureAttribute Attribute;

        public SqlProcedureAttributeImpl( SqlProcedureAttribute a )
        {
            Attribute = a;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IActivityLogger logger, IMutableSetupItem item, IStObj stObj )
        {
            SqlObjectProtoItem proto = SqlObjectItemAttributeImpl.LoadProtoItemFromResource( logger, (SqlPackageBaseItem)item, Attribute.ProcedureName );
            if( proto == null ) return;
  
        }
    }
}
