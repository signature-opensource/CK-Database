using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection.Emit;
using System.Reflection;
using CK.Setup;

namespace CK.SqlServer.Setup
{

    public enum ExecutionType
    {
        Unknown,
        ExecuteNonQuery
    } 
    
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = false )]
    public abstract class SqlCallableAttributeBase : SetupObjectItemMemberAttributeBase
    {
        protected SqlCallableAttributeBase( string callableName, string objectType, bool generateCall = true )
            : base( callableName, "CK.SqlServer.Setup.SqlCallableAttributeImpl, CK.SqlServer.Setup.Runtime" )
        {
            ObjectType = objectType;
        }

        public ExecutionType ExecuteCall { get; set; }

        public string ObjectType { get; set; }
    }

}
