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
using System.Linq;
using CK.Text;

namespace CK.SqlServer.Setup
{

    public class SqlTransformerItem : SqlBaseItem, ISetupObjectTransformerItem
    {
        SqlBaseItem _source;
        SqlBaseItem _target;

        internal SqlTransformerItem( SqlContextLocName name, ISqlServerTransformer t )
            : base( name, "Transformer", t )
        {
            Debug.Assert( name.TransformArg != null );
            SetDriverType( typeof( SqlTransformerItemDriver ) );
        }

        public new ISqlServerTransformer SqlObject
        {
            get { return (ISqlServerTransformer)base.SqlObject; }
            set { base.SqlObject = value; }
        }

        public new SqlTransformerItem TransformTarget => (SqlTransformerItem)base.TransformTarget;

        public SqlBaseItem Source => _source;

        IMutableSetupBaseItem ISetupObjectTransformerItem.Source
        {
            get { return _source; }
            set { _source = (SqlBaseItem)value; }
        }

        public SqlBaseItem Target => _target;

        IMutableSetupBaseItem ISetupObjectTransformerItem.Target
        {
            get { return _target; }
            set { _target = (SqlBaseItem)value; }
        }

        internal override bool Initialize( IActivityMonitor monitor, string fileName, IDependentItemContainer packageItem )
        {
            bool foundConfig;
            string h = SqlObject.HeaderComments.Select( c => c.Text ).Concatenate( Environment.NewLine );
            var configReader = CreateConfigReader();
            if( !configReader.Apply( monitor, h, this, _target, out foundConfig ) ) return false;
            if( !foundConfig )
            {
                monitor.Warn().Send( $"Resource '{fileName}' of '{packageItem?.FullName}' should define SetupConfig:{{}} (even an empty one)." );
            }
            return true;
        }

        protected internal override SetupConfigReader CreateConfigReader() => _target.CreateConfigReader();

    }
}
