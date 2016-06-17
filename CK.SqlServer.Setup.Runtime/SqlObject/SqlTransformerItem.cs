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
    public class SqlTransformerItem : SetupObjectItem
    {
        readonly ISqlServerTransformer _original;
        ISqlServerTransformer _final;

        internal SqlTransformerItem( SqlObjectProtoItem p, ISqlServerTransformer t )
            : base( p )
        {
            Debug.Assert( p.ItemType == SqlObjectProtoItem.TypeTransformer );
            _final = _original = t;
        }

        /// <summary>
        /// Gets whether the definition of this item is valid (its body is available).
        /// </summary>
        public bool IsValid => _original != null;

        /// <summary>
        /// Gets the original parsed object. 
        /// Can be null if an error occurred during parsing.
        /// </summary>
        public ISqlServerTransformer OriginalStatement => _original;

        /// <summary>
        /// Gets or sets a replacement of the <see cref="OriginalStatement"/>.
        /// This is initialized with <see cref="OriginalStatement"/> but can be changed.
        /// </summary>
        public ISqlServerTransformer FinalStatement
        {
            get { return _final; }
            set { _final = value; }
        }

        protected override object StartDependencySort() => typeof(SqlTransformerItemDriver);
    }
}
