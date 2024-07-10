using CK.Setup;
using CK.SqlServer.Setup;
using CK.SqlServer.Parser;
using CK.Core;

namespace SqlActorPackage.Engine
{
    class TestAutoHeaderSPHandler : SetupHandler
    {
        readonly string _header;

        public TestAutoHeaderSPHandler( SetupItemDriver d, string header )
            : base( d )
        {
            CheckItemType<SqlProcedureItem>();
            _header = header;
        }

        protected override bool Init( IActivityMonitor m )
        {
            SqlProcedureItem item = (SqlProcedureItem)Driver.Item;
            if( item.TransformTarget != null ) item = (SqlProcedureItem)item.TransformTarget;

            SqlStoredProcedure p = (SqlStoredProcedure)item.SqlObject;
            if( p != null )
            {
                item.SqlObject = p.AddLeadingTrivia( new SqlTrivia( SqlTokenType.LineComment, _header ) );
            }
            return true;
        }

    }
}
