using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup object item is initialized from a <see cref="ISetupObjectProtoItem"/>.
    /// This is the implementation for items for which version matters.
    /// </summary>
    public abstract class SetupObjectItemV : SetupObjectItem, IVersionedItem
    {
        Version _version;
        IEnumerable<VersionedName> _previousNames;

        protected SetupObjectItemV( ISetupObjectProtoItem p )
            : base( p )
        {
            _version = p.Version;
            _previousNames = p.PreviousNames;
        }

        /// <summary>
        /// Gets or sets the object that replaces this object.
        /// </summary>
        public new SetupObjectItemV ReplacedBy
        {
            get { return (SetupObjectItemV)base.ReplacedBy; }
            internal protected set { base.ReplacedBy = value; }
        }

        /// <summary>
        /// Gets the object that is replaced by this one.
        /// </summary>
        public new SetupObjectItemV Replaces
        {
            get { return (SetupObjectItemV)base.Replaces; }
        }

        /// <summary>
        /// Gets or sets the version number. Can be null.
        /// </summary>
        /// <remarks>
        /// When code builds the object, it may be safer to let the version be null and to rewrite the object.
        /// </remarks>
        public Version Version
        {
            get { return _version; }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _previousNames.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, ContextLocName.Context, ContextLocName.Location ) ); }
        }

    }


}
