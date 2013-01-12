using System;
using System.Reflection;
using System.Reflection.Emit;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlProcedureAttributeImpl : IStObjSetupDynamicInitializer, IAutoImplementorMethod
    {
        readonly SqlProcedureAttribute _attr;       
        SqlProcedureItem _item;
        MethodInfo _method;

        public SqlProcedureAttributeImpl( SqlProcedureAttribute a )
        {
            _attr = a;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IActivityLogger logger, IMutableSetupItem item, IStObjRuntime stObj )
        {
            SqlObjectProtoItem proto = SqlObjectItemAttributeImpl.LoadProtoItemFromResource( logger, (SqlPackageBaseItem)item, _attr.ProcedureName, SqlObjectProtoItem.TypeProcedure );
            if( proto == null ) return;
            _item = proto.CreateProcedureItem( logger, _method );
        }

        bool IAutoImplementorMethod.Implement( IActivityLogger logger, MethodInfo m, TypeBuilder b, bool isVirtual )
        {
            // Not ready to implement anything: returns false to implement a stub.
            if( _item == null )
            {
                _method = m;
                return false;
            }
            CK.Reflection.EmitHelper.ImplementEmptyStubMethod( b, m, isVirtual );
            return true;
        }

    }
}
