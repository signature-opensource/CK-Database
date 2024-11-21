using CK.Core;

namespace SqlZonePackage;

/// <summary>
/// This tests the CK.SqlServer.Dapper interface support (Dapper.SqlMapper.AddAbstractTypeMap).
/// </summary>
public interface ISimpleInfo : IPoco
{
    string Name { get; set; }

    int Power { get; set; }
}
