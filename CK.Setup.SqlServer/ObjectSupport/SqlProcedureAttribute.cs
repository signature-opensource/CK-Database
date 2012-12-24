using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection.Emit;
using System.Reflection;

namespace CK.Setup.SqlServer
{
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
    public class SqlProcedureAttribute : Attribute, IAttributeAutoImplemented, IAttributeAmbientContextBound, IStObjSetupDynamicInitializer
    {
        MethodInfo _method;

        public SqlProcedureAttribute( string procedureName )
        {
            ProcedureName = procedureName;
        }

        public string ProcedureName { get; private set; }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IActivityLogger logger, IMutableSetupItem item, IStObj stObj )
        {
            SqlPackageBaseItem p;

            SqlObjectProtoItem proto = SqlObjectItemAttributeImpl.LoadProtoItemFromResource( logger, (SqlPackageBaseItem)item, ProcedureName );
            if( proto == null ) return;
  

        }
    }
}
