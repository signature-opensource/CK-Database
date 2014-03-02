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
        public SqlViewItem( SqlView view )
            : base( "ObjView", typeof( SqlViewSetupDriver ), view )
        {
        }

        public SqlViewItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Name = data.FullNameWithoutContext;
            Context = data.StObj.Context.Context;
            if( Object.Database != null ) Location = Object.Database.Name;
            ResourceLocation = (ResourceLocator)data.StObj.GetStObjProperty( "ResourceLocation" );
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlView"/>.
        /// </summary>
        public new SqlView Object
        {
            get { return (SqlView)base.Object; } 
        }

        /// <summary>
        /// Gets or sets a <see cref="ResourceLocation"/> that locates the resources associated 
        /// to this package.
        /// </summary>
        public ResourceLocator ResourceLocation { get; set; }

        public SqlObjectProtoItem ProtoItem { get; set; }

        /// <summary>
        /// Writes the drop instruction.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteDrop( TextWriter b )
        {
            b.Write( "if OBJECT_ID('" );
            b.Write( this.Object.SchemaName );
            b.Write( "') is not null drop " );
            b.Write( "View" );
            b.Write( ' ' );
            b.Write( this.Object.SchemaName );
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
            b.Write( this.Object.SchemaName );
            if( ProtoItem != null ) b.WriteLine( ProtoItem.TextAfterName );
        }

    }
}
