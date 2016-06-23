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
        }

        public new ISqlServerTransformer SqlObject
        {
            get { return (ISqlServerTransformer)base.SqlObject; }
            set { base.SqlObject = value; }
        }

        public new SqlTransformerItem TransformTarget => (SqlTransformerItem)base.TransformTarget;

        public SqlBaseItem Source { get; private set; }

        public SqlBaseItem Target { get; private set; }

        protected override object StartDependencySort() => typeof(SqlTransformerItemDriver);

        internal void SetTransformSource( IActivityMonitor monitor, SqlBaseItem transformArgument )
        {
            Requires.Add( transformArgument );
            Source = transformArgument;
            Target = transformArgument.AddTransformer( monitor, this );
        }
    }
}
