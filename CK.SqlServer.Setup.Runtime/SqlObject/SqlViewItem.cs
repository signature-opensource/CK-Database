using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// A sql view item is a specialized <see cref="SqlObjectItem"/> whose <see cref="SqlObject"/> 
    /// is a <see cref="ISqlServerView"/>.
    /// </summary>
    public class SqlViewItem : SqlObjectItem
    {
        /// <summary>
        /// Initializes a view item with its name and code.
        /// </summary>
        /// <param name="name">Name of the view.</param>
        /// <param name="view">Code of the view.</param>
        public SqlViewItem( SqlContextLocName name, ISqlServerView view )
            : base( name, "View", view )
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="ISqlServerView"/> (specialized <see cref="ISqlServerObject"/>). 
        /// </summary>
        public new ISqlServerView SqlObject
        {
            get { return (ISqlServerView)base.SqlObject; }
            set { base.SqlObject = value; }
        }

        /// <summary>
        /// Gets the transformed view for this original view if there are transformers registered.
        /// </summary>
        public new SqlViewItem TransformTarget => (SqlViewItem)base.TransformTarget;

    }
}
