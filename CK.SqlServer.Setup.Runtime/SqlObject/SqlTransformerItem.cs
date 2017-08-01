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

        /// <summary>
        /// Gets whether this is the last transformer of the <see cref="Source"/>.
        /// </summary>
        public bool IsLastTransformer => Source.Transformers[Source.Transformers.Count - 1] == this; 

        /// <summary>
        /// Gets the target item (the final #transform item).
        /// </summary>
        public SqlBaseItem Target => _target;

        IMutableSetupBaseItem ISetupObjectTransformerItem.Target
        {
            get { return _target; }
            set { _target = (SqlBaseItem)value; }
        }

        public override SetupConfigReader CreateConfigReader() => _target.CreateConfigReader().CreateTransformerConfigReader( this );

    }
}
