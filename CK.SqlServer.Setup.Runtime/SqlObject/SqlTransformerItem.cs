using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;
using System.Text;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlTransformerItem : SqlBaseItem
    {
        internal SqlTransformerItem( SqlContextLocName name, ISqlServerTransformer t )
            : base( name, "Transformer", t )
        {
            Debug.Assert( name.TransformArg != null );
            Requires.Add( new NamedDependentItemRef( name.TransformArg ) );
        }

        public new ISqlServerTransformer SqlObject
        {
            get { return (ISqlServerTransformer)base.SqlObject; }
            set { base.SqlObject = value; }
        }

        protected override object StartDependencySort() => typeof(SqlTransformerItemDriver);
    }
}
