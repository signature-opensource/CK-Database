using CK.Setup;
using CK.SqlServer.Parser;
using System.Diagnostics;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Sql transformer item is a <see cref="SqlBaseItem"/>.
    /// </summary>
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

        /// <summary>
        /// Masked to be formally associated to a <see cref="ISqlServerTransformer"/> sql object.
        /// </summary>
        public new ISqlServerTransformer SqlObject
        {
            get { return (ISqlServerTransformer)base.SqlObject; }
            set { base.SqlObject = value; }
        }

        /// <summary>
        /// Masked to be formally associated to a <see cref="SqlTransformerItem"/> transformer.
        /// </summary>
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
