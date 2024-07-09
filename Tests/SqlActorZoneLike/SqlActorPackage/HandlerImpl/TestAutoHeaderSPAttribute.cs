using System;

namespace SqlActorPackage
{
    /// <summary>
    /// This attribute, when set on a class, injects a header in every stored procedure 
    /// that of a 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
    public class TestAutoHeaderSPAttribute : CK.Setup.SetupItemSelectorBaseAttribute
    {
        public TestAutoHeaderSPAttribute( string headerComment, string commaSeparatedNames )
            : base( "SqlActorPackage.Runtime.TestAutoHeaderSPAttributeImpl, SqlActorPackage.Runtime", commaSeparatedNames, CK.Core.SetupItemSelectorScope.All )
        {
            HeaderComment = headerComment;
        }

        public string HeaderComment { get; set;}

    }
}
