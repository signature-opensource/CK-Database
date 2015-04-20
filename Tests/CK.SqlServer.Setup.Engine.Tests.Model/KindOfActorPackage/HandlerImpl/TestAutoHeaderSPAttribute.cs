using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;

namespace SqlActorPackage
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
    public class TestAutoHeaderSPAttribute : SetupItemSelectorBaseAttribute
    {
        public TestAutoHeaderSPAttribute( string sqlHeader, string commaSeparatedNames )
            : base( "SqlActorPackage.Runtime.TestAutoHeaderSPAttributeImpl, SqlActorPackage.Runtime", commaSeparatedNames, SetupItemSelectorScope.All )
        {
            SqlHeader = sqlHeader;
        }

        public string SqlHeader { get; set;}

    }
}
