using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer.Setup;
using CK.SqlServer.Parser;

namespace SqlActorPackage.Runtime
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

        protected override bool Init()
        {
            SqlProcedureItem item = (SqlProcedureItem)Driver.Item;
            SqlStoredProcedure p = (SqlStoredProcedure)item.SqlObject;
            if( p != null )
            {
                item.SqlObject = p.AddLeadingTrivia( new SqlTrivia( SqlTokenType.LineComment, _header ) );
            }
            return true;
        }

    }
}
