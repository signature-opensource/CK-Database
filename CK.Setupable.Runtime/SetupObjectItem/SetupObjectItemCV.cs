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
    /// This is the implementation for items that can be containers or groups and for which version must be supported.
    /// </summary>
    public abstract class SetupObjectItemCV : SetupObjectItemC, IVersionedItem
    {
        Version _version;
        IEnumerable<VersionedName> _previousNames;

        protected SetupObjectItemCV( ISetupObjectProtoItem p )
            : base( p )
        {
            _version = p.Version;
            _previousNames = p.PreviousNames;
        }

        /// <summary>
        /// Gets or sets the version number. Can be null.
        /// </summary>
        /// <remarks>
        /// When code builds the object, it may be safer to let the version be null and to rewrite the object.
        /// </remarks>
        public Version Version => _version; 

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _previousNames.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, ContextLocName.Context, ContextLocName.Location ) ); }
        }

    }



}
