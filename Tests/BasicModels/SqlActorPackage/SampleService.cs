using CK.Core;
using CK.SqlServer;

namespace SqlActorPackage
{
    public class SampleService : ISampleService
    {
        readonly Basic.GroupHome _group;

        public SampleService( ISqlCallContext ctx, Basic.GroupHome group )
        {
            _group = group;
            SqlCallContext = ctx;
        }

        public ISqlCallContext SqlCallContext { get; }

        public int CreateGroup( string groupName )
        {
            _group.CmdCreate( SqlCallContext, groupName, out int id );
            return id;
        }
    }
}
