using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Runtime
{
    class TestAutoHeaderSPHandler : SetupHandler
    {
        readonly string _header;

        public TestAutoHeaderSPHandler( GenericItemSetupDriver d, string header )
            : base( d )
        {
            CheckItemType<SqlProcedureItem>();
            _header = header;
        }

        protected override bool Init()
        {
            SqlProcedureItem item = (SqlProcedureItem)Driver.Item;
            item.Header += Environment.NewLine;
            item.Header += _header;
            item.Header += Environment.NewLine;
            return true;
        }

    }
}
