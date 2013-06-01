using System.Data.SqlClient;
using System.Reflection;
using System.Reflection.Emit;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl : IStObjSetupDynamicInitializer, IAutoImplementorMethod
    {
        readonly SqlProcedureAttribute _attr;       
        SqlProcedureItem _item;
        MethodInfo _method;

        public SqlProcedureAttributeImpl( SqlProcedureAttribute a )
        {
            _attr = a;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjRuntime stObj )
        {
            SqlPackageBaseItem packageItem = (SqlPackageBaseItem)item;
            // 2 - Attempts to load the resource.
            SqlObjectProtoItem proto = SqlObjectItemAttributeImpl.LoadProtoItemFromResource( state.Logger, packageItem, _attr.ProcedureName, SqlObjectProtoItem.TypeProcedure );
            if( proto == null ) return;
            // On success, creates the SqlProcedureItem bound to the MethodInfo that must call it.
            _item = proto.CreateProcedureItem( state.Logger, _method );
            if( _item != null ) packageItem.Children.Add( _item );
        }

        bool IAutoImplementorMethod.Implement( IActivityLogger logger, MethodInfo m, TypeBuilder tB, bool isVirtual )
        {
            // 1 - Not ready to implement anything (no body yet): 
            //     - memorizes the MethodInfo.
            //     - returns false to implement a stub.
            if( _item == null )
            {
                _method = m;
                return false;
            }
            // 3 - Ready to implement the method.
            if( _item.OriginalStatement == null )
            {
                logger.Warn( "'{0}' has not been parsed. Generating an empty body for the method.", _item.FullName );
                CK.Reflection.EmitHelper.ImplementEmptyStubMethod( tB, m, isVirtual );
                return true;
            }
            if( m.ReturnType != typeof( SqlCommand ) || m.GetParameters().Length > 0 )
            {
                logger.Error( "Method '{0}.{1}' must return a SqlCommand and have no parameters.", m.DeclaringType.FullName, m.Name );
                CK.Reflection.EmitHelper.ImplementEmptyStubMethod( tB, m, isVirtual );
                return true;
            }
            GenerateCreateSqlCommand( m, tB, isVirtual, _item.OriginalStatement.Parameters );
            return true;
        }

    }
}
