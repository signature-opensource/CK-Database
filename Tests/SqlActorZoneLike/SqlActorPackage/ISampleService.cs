using CK.Core;
using CK.SqlServer;

namespace SqlActorPackage;

public interface ISampleService : IAutoService
{
    int CreateGroup( string groupName );
}
