using System;
using CK.Setup;

namespace SqlActorPackage
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
    public class TestAutoHeaderSPAttribute : SetupItemSelectorBaseAttribute
    {
        public TestAutoHeaderSPAttribute( string headerComment, string commaSeparatedNames )
            : base( "SqlActorPackage.Runtime.TestAutoHeaderSPAttributeImpl, SqlActorPackage.Runtime", commaSeparatedNames, SetupItemSelectorScope.All )
        {
            HeaderComment = headerComment;
        }

        public string HeaderComment { get; set;}

    }
}
