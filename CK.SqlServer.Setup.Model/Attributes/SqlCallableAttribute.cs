using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection.Emit;
using System.Reflection;

namespace CK.SqlServer.Setup
{

    public enum ExecutionType
    {
        Unknown,
        ExecuteNonQuery
    } 
    
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
    public abstract class SqlCallableAttributeBase : SqlObjectItemMemberAttributeBase
    {
        protected SqlCallableAttributeBase( string callableName, string objectType )
            : base( callableName, "CK.SqlServer.Setup.SqlCallableAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            ObjectType = objectType;
        }

        public ExecutionType ExecuteCall { get; set; }

        public string ObjectType { get; set; }
    }

}
