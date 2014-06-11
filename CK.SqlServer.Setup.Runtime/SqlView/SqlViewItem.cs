using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlViewItem : StObjDynamicPackageItem
    {
        SqlObjectProtoItem _protoItem;

        public SqlViewItem( Func<SqlView> view )
            : base( "ObjView", typeof( SqlViewSetupDriver ), view )
        {
        }

        public SqlViewItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Name = data.FullNameWithoutContext;
            Context = data.StObj.Context.Context;
            SqlView v = GetObject();
            if( v.Database != null ) Location = v.Database.Name;
            ResourceLocation = (ResourceLocator)data.StObj.GetStObjProperty( "ResourceLocation" );
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlView"/>.
        /// </summary>
        public new SqlView GetObject()
        {
            return (SqlView)base.GetObject(); 
        }

        /// <summary>
        /// Gets or sets a <see cref="ResourceLocation"/> that locates the resources associated 
        /// to this view.
        /// </summary>
        public ResourceLocator ResourceLocation { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="SqlObjectProtoItem"/>
        /// </summary>
        public SqlObjectProtoItem ProtoItem 
        {
            get { return _protoItem; }
            set
            {
                _protoItem = value;
                if( _protoItem != null )
                {
                    this.Requires.Add( _protoItem.Requires );
                    this.RequiredBy.Add( _protoItem.RequiredBy );
                }
            }
        }

        /// <summary>
        /// Writes the drop instruction.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteDrop( TextWriter b )
        {
            b.Write( "if OBJECT_ID('" );
            b.Write( GetObject().SchemaName );
            b.Write( "') is not null drop " );
            b.Write( "View" );
            b.Write( ' ' );
            b.Write( GetObject().SchemaName );
            b.WriteLine( ';' );
        }

        /// <summary>
        /// Writes the whole object.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteCreate( TextWriter b )
        {
            if( ProtoItem != null ) b.WriteLine( ProtoItem.Header );
            b.Write( "create " );
            b.Write( "View" );
            b.Write( ' ' );
            b.Write( GetObject().SchemaName );
            if( ProtoItem != null ) b.WriteLine( ProtoItem.TextAfterName );
        }

    }
}
