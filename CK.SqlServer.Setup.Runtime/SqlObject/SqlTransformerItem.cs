using CK.Setup;
using CK.SqlServer.Parser;
using System;
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

        /// <summary>
        /// Initializes a new Sql transformer. 
        /// It must be added to its target (<see cref="SetupObjectItem.AddTransformer(Core.IActivityMonitor, ISetupObjectTransformerItem)"/>).
        /// </summary>
        /// <param name="name">The name of this transformer. Of course, <see cref="ContextLocName.TransformArg"/> must not be null.</param>
        /// <param name="t">The transformer itself.</param>
        public SqlTransformerItem( SqlContextLocName name, ISqlServerTransformer t )
            : base( name, "Transformer", t )
        {
            if( name.TransformArg == null ) throw new ArgumentNullException( nameof(name.TransformArg) );
            if( t == null ) throw new ArgumentNullException( nameof( t ) );
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

        /// <summary>
        /// Gets the source item.
        /// </summary>
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

        /// <summary>
        /// Creates a <see cref="SetupConfigReader"/>.
        /// </summary>
        /// <returns>The configuration reader to use.</returns>
        public override SetupConfigReader CreateConfigReader() => _target.CreateConfigReader().CreateTransformerConfigReader( this );

    }
}
