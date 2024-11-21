using CK.Core;

namespace SqlActorPackage.Basic;

[SqlTable( "bad name table", Package = typeof( Package ), Schema = "bad schema name" )]
[Versions( "1.0.0" )]
public abstract class BadNameTable : SqlTable
{
    // This is also used to show the "out of transaction" script.
    // See the Install script.
}
