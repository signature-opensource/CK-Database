using System;
using CK.Setup;

namespace SqlActorPackage;

[AttributeUsage( AttributeTargets.Method, AllowMultiple = true, Inherited = false )]
public class TestAutoHeaderSPMemberAttribute : SetupObjectItemRefMemberAttributeBase
{
    public TestAutoHeaderSPMemberAttribute( string headerComment )
        : base( "SqlActorPackage.Engine.TestAutoHeaderSPMemberAttributeImpl, SqlActorPackage.Engine" )
    {
        HeaderComment = headerComment;
    }

    public string HeaderComment { get; set; }

}
