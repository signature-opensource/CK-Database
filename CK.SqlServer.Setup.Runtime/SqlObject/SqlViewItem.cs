using CK.Setup;
using CK.SqlServer.Parser;
using System.Diagnostics;
using System;
using CK.Core;

namespace CK.SqlServer.Setup
{
    public class SqlViewItem : SqlObjectItem
    {
        internal SqlViewItem( SqlContextLocName name, ISqlServerView view )
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

        public new SqlViewItem TransformTarget => (SqlViewItem)base.TransformTarget;

    }
}
