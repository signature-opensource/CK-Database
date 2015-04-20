using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;

namespace SqlActorPackage
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited=false)]
    public class TestAutoHeaderSPMemberAttribute : SetupObjectItemRefMemberAttributeBase
    {
        public TestAutoHeaderSPMemberAttribute( string sqlHeader )
            : base( "SqlActorPackage.Runtime.TestAutoHeaderSPMemberAttributeImpl, SqlActorPackage.Runtime" )
        {
            SqlHeader = sqlHeader;
        }

        public string SqlHeader { get; set;}

    }
}
